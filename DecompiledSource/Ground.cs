using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Ground : KoroutineBehaviour
{
	public GroundGroup group;

	[Tooltip("Transform with one or more sphere colliders that combined describe the global ground shape to spawn in")]
	[SerializeField]
	private Transform shapeTransform;

	[NonSerialized]
	public Circle[] shapeCircles;

	[NonSerialized]
	public Circle globalShapeCircle;

	[Space(10f)]
	[FormerlySerializedAs("randomizables")]
	public List<GameObject> preplacedGroups = new List<GameObject>();

	public List<UnlockerSpawner> unlockerSpawners = new List<UnlockerSpawner>();

	[Space(10f)]
	public List<StringTransform> arrowLocations = new List<StringTransform>();

	[Header("Plant grooves")]
	public Transform tfGrooves;

	public float groovePointOffset = 0.5f;

	[Tooltip("Get these filled with the button below")]
	public List<Vector2> groovePoints;

	public Distribution groovePlantDistribution;

	private List<Building> allBuildings = new List<Building>();

	private List<Stockpile> allStockpiles = new List<Stockpile>();

	private List<BatteryBuilding> allBatteryBuildings = new List<BatteryBuilding>();

	private Dictionary<PickupType, int> dicInventory;

	private List<Pickup> allLarvae = new List<Pickup>();

	private List<Pickup> allPickupsOnGround = new List<Pickup>();

	private HashSet<Ant> allAnts = new HashSet<Ant>();

	private List<Building> allAntBuildings = new List<Building>();

	private int antCount;

	private Collider[] colliders;

	private List<ArrowPointer3D> spawnedArrows = new List<ArrowPointer3D>();

	[NonSerialized]
	public Biome biome;

	[NonSerialized]
	public string biomeAddress;

	[NonSerialized]
	public int groundIndex;

	[NonSerialized]
	public Vector3 rectCorner;

	[NonSerialized]
	public Vector3 rectDir1;

	[NonSerialized]
	public Vector3 rectDir2;

	private int navGridW;

	private int navGridH;

	[NonSerialized]
	public Vector3 navSquareX;

	[NonSerialized]
	public Vector3 navSquareY;

	[NonSerialized]
	public Vector2Int gridSizeBiome;

	private Dictionary<int, NavPoint> navPoints;

	public Ecology ecology;

	private float pollution;

	[NonSerialized]
	public float surfaceFactor;

	public const int NO_GROUND_ID = -10;

	private List<Material> matsMap;

	private List<Material> matsMapOrig;

	public int generationSeed { get; private set; }

	public int generationSeedIndex { get; private set; }

	public void Write(Save save)
	{
		save.Write(biomeAddress);
		save.Write(generationSeed);
		save.Write(generationSeedIndex);
		save.Write(base.transform.position);
		save.WriteYRot(base.transform.rotation);
		save.Write(groundIndex);
		save.Write(surfaceFactor);
		ecology.Write(save);
	}

	public IEnumerator KRead(Save save, Action<float> action_progress)
	{
		KoroutineId kid = SetFinalizer();
		try
		{
			if (save.version < 31)
			{
				save.ReadFloat();
			}
			if (save.version >= 33)
			{
				surfaceFactor = save.ReadFloat();
			}
			else
			{
				surfaceFactor = 1f;
			}
			ecology = new Ecology();
			ecology.Init(this, during_load: true);
			action_progress(0.2f);
			yield return null;
			int n = save.ReadInt();
			for (int i = 0; i < n; i++)
			{
				ecology.ReadPart(save);
				action_progress(0.2f + 0.8f * (float)(i + 1) / (float)n);
				yield return null;
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public void SetLinkIds(ref int id)
	{
		ecology.SetLinkIds(ref id);
	}

	public void LoadLinkPickups()
	{
		ecology.LoadLinkPickups();
	}

	public static Ground Create(Biome biome, Vector3 ground_pos, Quaternion ground_rot, int ground_index)
	{
		if (ground_index == -1)
		{
			ground_index = biome.PickGroundIndex();
		}
		else if (ground_index < 0 || ground_index >= biome.groundPrefabs.Count)
		{
			Debug.LogWarning($"Ground create {biome.name}.Generate: ground index {ground_index} doesn't exist");
			ground_index = 0;
		}
		Ground component = UnityEngine.Object.Instantiate(biome.groundPrefabs[ground_index], ground_pos, ground_rot).GetComponent<Ground>();
		component.biome = biome;
		component.groundIndex = ground_index;
		component.InitDissolve();
		return component;
	}

	private void InitDissolve()
	{
		matsMap = new List<Material>();
		matsMapOrig = new List<Material>();
		AddDissolveMaterials(base.gameObject);
	}

	private void AddDissolveMaterials(GameObject ob)
	{
		if (ob.layer == 22 && ob.TryGetComponent<Renderer>(out var component))
		{
			Material[] sharedMaterials = component.sharedMaterials;
			for (int i = 0; i < sharedMaterials.Length; i++)
			{
				Material material = sharedMaterials[i];
				if (!(material == null))
				{
					int num = matsMapOrig.IndexOf(material);
					if (num == -1)
					{
						matsMapOrig.Add(material);
						material = new Material(material);
						matsMap.Add(material);
						sharedMaterials[i] = material;
					}
					else
					{
						sharedMaterials[i] = matsMap[num];
					}
				}
			}
			component.sharedMaterials = sharedMaterials;
		}
		for (int j = 0; j < ob.transform.childCount; j++)
		{
			AddDissolveMaterials(ob.transform.GetChild(j).gameObject);
		}
	}

	public void SetDissolve(float f)
	{
		foreach (Material item in matsMap)
		{
			item.SetFloat("_Dissolve", f);
		}
	}

	public IEnumerator KFill(Transform spawn_parent, int generation_seed, int generation_seed_index, bool spawn_unlocker, Save from_save = null)
	{
		KoroutineId kid = SetFinalizer();
		try
		{
			Toolkit.SetRandomSeed(generation_seed, generation_seed_index);
			Biome.generatingBiome = biome;
			Biome.generatingGround = this;
			List<BiomeObjectSpawnInfo> list = new List<BiomeObjectSpawnInfo>();
			List<BuildingSpawnInfo> list2 = new List<BuildingSpawnInfo>();
			bool flag = from_save != null;
			if (!InitShape(base.transform.rotation))
			{
				Debug.LogError("Ow, Ground " + base.name + " is niet goed ingesteld met shapeTransform colliders; aborting");
				yield break;
			}
			generationSeed = generation_seed;
			generationSeedIndex = generation_seed_index;
			dicInventory = new Dictionary<PickupType, int>();
			foreach (PickupType item3 in PickupData.EAllPickupTypes())
			{
				dicInventory.Add(item3, 0);
			}
			if (TestBed.instance != null)
			{
				if (preplacedGroups.Count > 0)
				{
					int num = UnityEngine.Random.Range(0, preplacedGroups.Count);
					for (int i = 0; i < preplacedGroups.Count; i++)
					{
						preplacedGroups[i].SetObActive(i == num);
					}
				}
				if (spawn_unlocker && unlockerSpawners.Count > 0 && !WorldSettings.sandbox)
				{
					List<UnlockerSpawner> list3 = new List<UnlockerSpawner>();
					foreach (UnlockerSpawner unlockerSpawner in unlockerSpawners)
					{
						if (unlockerSpawner.gameObject.activeInHierarchy)
						{
							list3.Add(unlockerSpawner);
						}
					}
					foreach (UnlockerSpawner item4 in list3)
					{
						item4.SpawnUnlocker();
					}
				}
			}
			else if (!flag)
			{
				if (preplacedGroups.Count > 0)
				{
					int num2 = UnityEngine.Random.Range(0, preplacedGroups.Count);
					for (int j = 0; j < preplacedGroups.Count; j++)
					{
						preplacedGroups[j].SetObActive(j == num2);
					}
				}
				if (spawn_unlocker && unlockerSpawners.Count > 0 && !WorldSettings.sandbox)
				{
					List<UnlockerSpawner> list4 = new List<UnlockerSpawner>();
					foreach (UnlockerSpawner unlockerSpawner2 in unlockerSpawners)
					{
						if (unlockerSpawner2.gameObject.activeInHierarchy)
						{
							list4.Add(unlockerSpawner2);
						}
					}
					foreach (UnlockerSpawner item5 in list4)
					{
						item5.SpawnUnlocker();
					}
				}
				BiomeObject[] componentsInChildren = GetComponentsInChildren<BiomeObject>(includeInactive: false);
				foreach (BiomeObject biomeObject in componentsInChildren)
				{
					if (biomeObject is Plant)
					{
						Debug.LogError("No support for plants preplaced in ground " + base.name);
						continue;
					}
					string codeFromBiomeObject = BiomeObjectData.GetCodeFromBiomeObject(biomeObject);
					if (codeFromBiomeObject == null)
					{
						continue;
					}
					List<int> list5 = new List<int>();
					for (int l = 0; l < biomeObject.meshes.Count; l++)
					{
						if (biomeObject.meshes[l].gameObject.activeInHierarchy)
						{
							list5.Add(l);
						}
					}
					int meshIndex = ((list5.Count != 0) ? list5[UnityEngine.Random.Range(0, list5.Count)] : UnityEngine.Random.Range(0, biomeObject.meshes.Count));
					BiomeObjectSpawnInfo item = new BiomeObjectSpawnInfo
					{
						code = codeFromBiomeObject,
						pos = biomeObject.transform.position,
						rot = biomeObject.transform.rotation,
						size = biomeObject.transform.localScale.x,
						meshIndex = meshIndex
					};
					list.Add(item);
				}
				Building[] componentsInChildren2 = GetComponentsInChildren<Building>(includeInactive: false);
				foreach (Building building in componentsInChildren2)
				{
					string codeFromBuilding = BuildingData.GetCodeFromBuilding(building);
					if (codeFromBuilding != null)
					{
						BuildingSpawnInfo item2 = new BuildingSpawnInfo
						{
							code = codeFromBuilding,
							pos = building.transform.position,
							rot = building.transform.rotation
						};
						list2.Add(item2);
					}
				}
				for (int n = 0; n < componentsInChildren.Length; n++)
				{
					UnityEngine.Object.Destroy(componentsInChildren[n].gameObject);
				}
				for (int num3 = 0; num3 < componentsInChildren2.Length; num3++)
				{
					UnityEngine.Object.Destroy(componentsInChildren2[num3].gameObject);
				}
			}
			else
			{
				foreach (GameObject preplacedGroup in preplacedGroups)
				{
					preplacedGroup.SetObActive(active: false);
				}
				BiomeObject[] componentsInChildren3 = GetComponentsInChildren<BiomeObject>(includeInactive: false);
				for (int num4 = 0; num4 < componentsInChildren3.Length; num4++)
				{
					componentsInChildren3[num4].SetObActive(active: false);
				}
			}
			if (from_save != null)
			{
				yield break;
			}
			SetConvex(target: false);
			int grid_w = gridSizeBiome.x;
			int grid_h = gridSizeBiome.y;
			Vector3 ground_corner = rectCorner;
			if (spawn_parent == null)
			{
				spawn_parent = base.transform;
			}
			biome.preplacedSpawnedBobs = new List<BiomeObject>();
			foreach (BiomeObjectSpawnInfo item6 in list)
			{
				BiomeObject biomeObject2 = GameManager.instance.SpawnBiomeObject(item6.code, this, item6.pos, item6.rot, spawn_parent, item6.size, item6.meshIndex);
				biome.preplacedSpawnedBobs.Add(biomeObject2);
				AddDissolveMaterials(biomeObject2.gameObject);
			}
			List<Building> spawned_buildings = new List<Building>();
			foreach (BuildingSpawnInfo item7 in list2)
			{
				Building building2 = BuildingEditing.SpawnBuilding(item7.code);
				building2.transform.SetPositionAndRotation(item7.pos, item7.rot);
				building2.PlacePreplacedBuilding();
				if (building2 is Unlocker { unlockerType: UnlockerType.Unlocker } unlocker)
				{
					unlocker.PickUnlock(biome.unlockerTier);
				}
				AddDissolveMaterials(building2.gameObject);
				spawned_buildings.Add(building2);
			}
			yield return new WaitForSeconds(0.1f);
			float[,] areaGrid = new float[grid_w, grid_h];
			float[,] elGrid = new float[grid_w, grid_h];
			Vector3 vector = rectDir1 * 8f;
			Vector3 vector2 = rectDir2 * 8f;
			GridPos[,] fillGrid = new GridPos[grid_w, grid_h];
			List<GridPos> toFill = new List<GridPos>();
			for (int num5 = 0; num5 < grid_w; num5++)
			{
				for (int num6 = 0; num6 < grid_h; num6++)
				{
					Vector3 pos = ground_corner + num5 * vector + num6 * vector2;
					GridPos gridPos = null;
					if (IsWithinShape(pos))
					{
						int num7 = Toolkit.CheckFreeGroundPos(ref pos);
						if (num7 == 0 || num7 == 2)
						{
							gridPos = new GridPos(num5, num6, pos);
							if (num7 == 2)
							{
								gridPos.obstacle = true;
								gridPos.clearance = 3;
							}
							toFill.Add(gridPos);
						}
					}
					fillGrid[num5, num6] = gridPos;
				}
			}
			List<GridPos> list6 = new List<GridPos>(toFill);
			int num8 = grid_w * grid_h * 10;
			while (list6.Count > 0 && num8 > 0)
			{
				num8--;
				GridPos gridPos2 = list6[0];
				list6.RemoveAt(0);
				if (gridPos2.obstacle)
				{
					continue;
				}
				int num4 = gridPos2.x;
				int y = gridPos2.y;
				int num9 = num4;
				int num10 = y;
				GridPos gridPos3 = ((num9 == 0) ? null : fillGrid[num9 - 1, num10]);
				GridPos gridPos4 = ((num10 == 0) ? null : fillGrid[num9, num10 - 1]);
				GridPos gridPos5 = ((num9 == grid_w - 1) ? null : fillGrid[num9 + 1, num10]);
				GridPos gridPos6 = ((num10 == grid_h - 1) ? null : fillGrid[num9, num10 + 1]);
				int num11 = -1;
				if (gridPos3 == null || gridPos4 == null || gridPos5 == null || gridPos6 == null)
				{
					num11 = 1;
				}
				else
				{
					int num12 = int.MaxValue;
					if (gridPos3.clearance > -1)
					{
						num12 = gridPos3.clearance;
					}
					if (gridPos4.clearance > -1)
					{
						num12 = Mathf.Min(gridPos4.clearance, num12);
					}
					if (gridPos5.clearance > -1)
					{
						num12 = Mathf.Min(gridPos5.clearance, num12);
					}
					if (gridPos6.clearance > -1)
					{
						num12 = Mathf.Min(gridPos6.clearance, num12);
					}
					if (num12 < int.MaxValue)
					{
						num11 = num12 + 1;
					}
				}
				if (num11 == -1)
				{
					list6.Add(gridPos2);
					continue;
				}
				gridPos2.clearance = num11;
				if (gridPos3 != null && gridPos3.clearance > num11 + 1 && !gridPos3.obstacle)
				{
					gridPos3.clearance = -1;
					list6.Add(gridPos3);
				}
				if (gridPos4 != null && gridPos4.clearance > num11 + 1 && !gridPos4.obstacle)
				{
					gridPos4.clearance = -1;
					list6.Add(gridPos4);
				}
				if (gridPos5 != null && gridPos5.clearance > num11 + 1 && !gridPos5.obstacle)
				{
					gridPos5.clearance = -1;
					list6.Add(gridPos5);
				}
				if (gridPos6 != null && gridPos6.clearance > num11 + 1 && !gridPos6.obstacle)
				{
					gridPos6.clearance = -1;
					list6.Add(gridPos6);
				}
			}
			if (num8 < 0)
			{
				Debug.LogError("Endless loop");
			}
			for (int num13 = toFill.Count - 1; num13 >= 0; num13--)
			{
				if (toFill[num13].obstacle)
				{
					toFill.RemoveAt(num13);
				}
			}
			float surface_factor = (surfaceFactor = (float)toFill.Count / 10000f);
			toFill.Shuffle();
			Distribution.Init();
			int spread = 0;
			foreach (BiomeArea area2 in biome.areas)
			{
				foreach (BiomeElement element in area2.elements)
				{
					element.spawned = new List<Vector2Int>();
				}
			}
			foreach (BiomeArea area in biome.areas)
			{
				if (area.disabled)
				{
					continue;
				}
				area.distribution.Fill(areaGrid, surface_factor);
				foreach (BiomeElement el in area.elements)
				{
					if (el.disabled)
					{
						continue;
					}
					el.distribution.Fill(elGrid, surface_factor);
					int min_amount = Mathf.RoundToInt((float)el.minAmount * surface_factor);
					int max_amount = Mathf.RoundToInt((float)el.maxAmount * surface_factor);
					if (min_amount == 0 && el.minAmount > 0)
					{
						min_amount = 1;
					}
					string code = el.element.ToString();
					BiomeElementType typ = Biome.GetElementType(el.element);
					GameObject prefab = null;
					PickupType pickup_type = PickupType.NONE;
					switch (typ)
					{
					case BiomeElementType.BIOMEOBJECT:
						prefab = BiomeObjectData.Get(code).prefab;
						break;
					case BiomeElementType.PICKUP:
						if (Enum.TryParse<PickupType>(code, out pickup_type))
						{
							PickupData pickupData = PickupData.Get(pickup_type);
							if (pickupData != null)
							{
								prefab = pickupData?.prefab;
							}
						}
						break;
					default:
						Debug.LogError("Biome " + base.name + ": don't know object with code '" + code + "'");
						break;
					}
					if (prefab == null)
					{
						Debug.LogError("Can't spawn object with code '" + code + "'");
						continue;
					}
					List<GridPos> toFillCurrent = new List<GridPos>(toFill);
					if (prefab.TryGetComponent<BiomeObject>(out var component))
					{
						float num14 = ((component.sidePoint == null) ? component.GetRadiusWhenNoMeshSelectedYet() : component.GetRadius());
						float num15 = (el.minSize + el.maxSize) * 0.5f;
						int num16 = Mathf.RoundToInt(num14 * num15 / 8f);
						if (num16 > 1)
						{
							for (int num17 = toFillCurrent.Count - 1; num17 >= 0; num17--)
							{
								if (toFillCurrent[num17].clearance < num16)
								{
									toFillCurrent.RemoveAt(num17);
								}
							}
						}
					}
					GridPos[,] fillGridCurrent = new GridPos[grid_w, grid_h];
					foreach (GridPos item8 in toFillCurrent)
					{
						fillGridCurrent[item8.x, item8.y] = item8;
					}
					int attempt = 0;
					int maxAttempts = 10;
					do
					{
						attempt++;
						for (int i2 = toFillCurrent.Count - 1; i2 >= 0; i2--)
						{
							GridPos gridPos7 = toFillCurrent[i2];
							int x = gridPos7.x;
							int y2 = gridPos7.y;
							float num18 = areaGrid[x, y2] * elGrid[x, y2];
							if (num18 > 0f && UnityEngine.Random.value < num18 * (float)attempt)
							{
								Vector3 pos2 = gridPos7.pos + new Vector3(0.5f + UnityEngine.Random.Range(-0.4f, 0.4f), 0f, 0.5f + UnityEngine.Random.Range(-0.4f, 0.4f)) * 8f;
								Quaternion rot = Toolkit.RandomYRotation();
								float num19 = ((typ == BiomeElementType.PICKUP) ? 1f : (el.minSize + (el.maxSize - el.minSize) * Mathf.Lerp(num18, UnityEngine.Random.value, el.sizeRandomness)));
								switch (typ)
								{
								case BiomeElementType.BIOMEOBJECT:
									if (TestBed.instance != null)
									{
										TestBed.instance.SpawnBiomeObject(prefab, code, pos2, rot, spawn_parent, num19);
									}
									else
									{
										GameManager.instance.SpawnBiomeObject(prefab, code, this, pos2, rot, spawn_parent, num19);
									}
									break;
								case BiomeElementType.PICKUP:
									if (TestBed.instance != null)
									{
										TestBed.instance.SpawnPickup(prefab, pickup_type, pos2, rot, spawn_parent);
									}
									else
									{
										GameManager.instance.SpawnPickup(prefab, pickup_type, pos2, rot).SetStatus(PickupStatus.ON_GROUND, spawn_parent);
									}
									break;
								}
								if (el.radiusBlockOwn > 0f)
								{
									int num20 = BlockGrid(x, y2, Mathf.RoundToInt(el.radiusBlockOwn * num19 * 0.8f / 8f), toFillCurrent, fillGridCurrent, grid_w, grid_h);
									if (num20 == 0)
									{
										Debug.LogError("uuuhhhhhh");
										yield break;
									}
									i2 -= num20 - 1;
								}
								if (el.radiusBlockOthers > 0f)
								{
									BlockGrid(x, y2, Mathf.RoundToInt(el.radiusBlockOthers * num19 * 0.8f / 8f), toFill, fillGrid, grid_w, grid_h);
								}
								el.spawned.Add(new Vector2Int(x, y2));
								spread++;
								if (spread % 100 == 0)
								{
									yield return null;
								}
							}
							if ((max_amount > 0 && el.spawned.Count >= max_amount) || (attempt > 1 && el.spawned.Count >= min_amount))
							{
								break;
							}
						}
					}
					while (el.spawned.Count < min_amount && attempt < maxAttempts);
					if (el.spawned.Count < min_amount)
					{
						Debug.LogWarning($"Couldn't get {min_amount} elements of type {el.element}, stopped at {el.spawned.Count}");
					}
				}
				if (!(area.blockThreshold < 1f))
				{
					continue;
				}
				for (int num21 = 0; num21 < grid_w; num21++)
				{
					for (int num22 = 0; num22 < grid_h; num22++)
					{
						if (areaGrid[num21, num22] > area.blockThreshold)
						{
							GridPos gridPos8 = fillGrid[num21, num22];
							if (gridPos8 != null)
							{
								toFill.Remove(gridPos8);
								fillGrid[num21, num22] = null;
							}
						}
					}
				}
			}
			foreach (Building item9 in spawned_buildings)
			{
				if (item9 != null)
				{
					item9.ClearEntrances();
				}
			}
			SetConvex(target: true);
			yield return new WaitForSeconds(0.1f);
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public static int BlockGrid(int x, int y, int radius, List<GridPos> to_fill, GridPos[,] fill_grid, int grid_w, int grid_h)
	{
		int num = 0;
		int num2 = Mathf.RoundToInt((float)radius * 1.5f);
		for (int i = -radius; i <= radius; i++)
		{
			int num3 = x + i;
			if (num3 < 0 || num3 >= grid_w)
			{
				continue;
			}
			for (int j = -radius; j <= radius; j++)
			{
				int num4 = y + j;
				if (num4 >= 0 && num4 < grid_h && Mathf.Abs(i) + Mathf.Abs(j) <= num2)
				{
					GridPos gridPos = fill_grid[num3, num4];
					if (gridPos != null)
					{
						num++;
						to_fill.Remove(gridPos);
						fill_grid[num3, num4] = null;
					}
				}
			}
		}
		return num;
	}

	public void GroundUpdate(float dt)
	{
		ecology.Update(pollution, dt);
	}

	public void Delete()
	{
		allBuildings.Clear();
		allStockpiles.Clear();
		allBatteryBuildings.Clear();
		ecology.Delete();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public bool InitShape(Quaternion ground_rot, bool force = false)
	{
		if (!force && globalShapeCircle.radius > 0f)
		{
			return true;
		}
		if (shapeTransform == null)
		{
			return false;
		}
		SphereCollider[] components = shapeTransform.GetComponents<SphereCollider>();
		if (components.Length == 0)
		{
			return false;
		}
		shapeCircles = new Circle[components.Length];
		for (int i = 0; i < components.Length; i++)
		{
			SphereCollider sphereCollider = components[i];
			Circle circle = new Circle(ground_rot * sphereCollider.center, sphereCollider.radius);
			shapeCircles[i] = circle;
			sphereCollider.enabled = false;
			globalShapeCircle = ((i == 0) ? shapeCircles[0] : new Circle(globalShapeCircle, shapeCircles[i]));
		}
		Vector3[] array = new Vector3[shapeCircles.Length * 4];
		float num = float.MaxValue;
		float num2 = 0f;
		float num3 = 0f;
		for (float num4 = 0f; num4 < 89f; num4 += 22.5f)
		{
			Quaternion quaternion = Quaternion.Euler(0f, num4, 0f);
			Quaternion quaternion2 = Quaternion.Inverse(quaternion);
			Vector3 vector = quaternion * new Vector3(0f, 0f, 1f);
			Vector3 vector2 = quaternion * new Vector3(1f, 0f, 0f);
			for (int j = 0; j < shapeCircles.Length; j++)
			{
				Circle circle2 = shapeCircles[j];
				array[j * 4] = circle2.pos + vector * circle2.radius;
				array[j * 4 + 1] = circle2.pos - vector * circle2.radius;
				array[j * 4 + 2] = circle2.pos + vector2 * circle2.radius;
				array[j * 4 + 3] = circle2.pos - vector2 * circle2.radius;
			}
			float num5 = float.MaxValue;
			float num6 = float.MinValue;
			float num7 = float.MaxValue;
			float num8 = float.MinValue;
			for (int k = 0; k < array.Length; k++)
			{
				Vector3 vector3 = quaternion2 * array[k];
				if (vector3.x < num5)
				{
					num5 = vector3.x;
				}
				if (vector3.x > num6)
				{
					num6 = vector3.x;
				}
				if (vector3.z < num7)
				{
					num7 = vector3.z;
				}
				if (vector3.z > num8)
				{
					num8 = vector3.z;
				}
			}
			float num9 = num6 - num5;
			float num10 = num8 - num7;
			if (num9 * num10 < num)
			{
				num = num9 * num10;
				rectCorner = quaternion * new Vector3(num5, 0f, num7);
				rectDir1 = vector2;
				rectDir2 = vector;
				num2 = num9;
				num3 = num10;
			}
		}
		rectCorner += base.transform.position;
		gridSizeBiome = new Vector2Int(Mathf.CeilToInt(num2 / 8f), Mathf.CeilToInt(num3 / 8f));
		CreateNavPoints(num2, num3);
		return true;
	}

	private void CreateNavPoints(float rect_size_x, float rect_size_y)
	{
		navPoints = new Dictionary<int, NavPoint>();
		float num = 12f;
		navGridW = Mathf.CeilToInt(rect_size_x / num);
		navGridH = Mathf.CeilToInt(rect_size_y / num);
		navSquareX = rectDir1 * num;
		navSquareY = rectDir2 * num;
		for (int i = 0; i < navGridW; i++)
		{
			for (int j = 0; j < navGridH; j++)
			{
				Vector3 vector = rectCorner + i * navSquareX + j * navSquareY;
				if (IsWithinShape(vector) && Toolkit.IsOnGround(vector))
				{
					navPoints.Add((i << 16) + j, new NavPoint(vector.XZ()));
				}
			}
		}
		List<NavPoint> list = new List<NavPoint>();
		for (int k = 0; k < navGridW; k++)
		{
			for (int l = 0; l < navGridH; l++)
			{
				NavPoint navPoint = GetNavPoint(k, l);
				if (navPoint == null)
				{
					continue;
				}
				list.Clear();
				for (int m = -1; m <= 1; m++)
				{
					for (int n = -1; n <= 1; n++)
					{
						if (m != 0 || n != 0)
						{
							NavPoint navPoint2 = GetNavPoint(k + m, l + n);
							if (navPoint2 != null)
							{
								list.Add(navPoint2);
							}
						}
					}
				}
				navPoint.SetNeighbours(list);
			}
		}
	}

	public void NavReset()
	{
		foreach (NavPoint value in navPoints.Values)
		{
			value.ResetScores();
		}
	}

	private NavPoint GetNavPoint(int x, int y)
	{
		if (navPoints.TryGetValue((x << 16) + y, out var value))
		{
			return value;
		}
		return null;
	}

	private Vector2Int GetNavGridPosNear(Vector2 pos)
	{
		Vector2 target = pos - rectCorner.XZ();
		Vector2 sx = navSquareX.XZ();
		Vector2 sy = navSquareY.XZ();
		int x = 0;
		int y = 0;
		float num = dist(x, y);
		for (x = 1; x <= navGridW; x++)
		{
			float num2 = dist(x, y);
			if (num2 > num)
			{
				break;
			}
			num = num2;
		}
		x--;
		for (y = 1; y <= navGridH; y++)
		{
			float num3 = dist(x, y);
			if (num3 > num)
			{
				break;
			}
			num = num3;
		}
		y--;
		return new Vector2Int(x, y);
		float dist(int num4, int num5)
		{
			return (num4 * sx + num5 * sy - target).sqrMagnitude;
		}
	}

	public NavPoint GetNearestNavPoint(Vector2 pos)
	{
		Vector2Int navGridPosNear = GetNavGridPosNear(pos);
		return GetNavPoint(navGridPosNear.x, navGridPosNear.y);
	}

	public void UpdateNavMesh(Vector3 pos, float radius)
	{
		int num = Mathf.CeilToInt(radius / 12f + 0.5f);
		Vector2Int navGridPosNear = GetNavGridPosNear(pos.XZ());
		for (int i = navGridPosNear.x - num; i <= navGridPosNear.x + num; i++)
		{
			for (int j = navGridPosNear.y - num; j <= navGridPosNear.y + num; j++)
			{
				GetNavPoint(i, j)?.ResetLinks();
			}
		}
	}

	public Vector3 GetRandomPosInShape()
	{
		for (int i = 0; i < 50; i++)
		{
			Vector3 randomPos = globalShapeCircle.GetRandomPos();
			if (IsWithinShapeLocal(randomPos))
			{
				return randomPos;
			}
		}
		Debug.LogError("Ground.GetRandomPosInShape: couldn't pick random pos");
		return globalShapeCircle.pos;
	}

	public bool IsWithinShape(Vector3 global_pos)
	{
		return IsWithinShapeLocal(global_pos - base.transform.position);
	}

	private bool IsWithinShapeLocal(Vector3 local_pos)
	{
		for (int i = 0; i < shapeCircles.Length; i++)
		{
			Circle circle = shapeCircles[i];
			float num = local_pos.x - circle.pos.x;
			float num2 = local_pos.z - circle.pos.z;
			if (num * num + num2 * num2 < circle.radiusSq)
			{
				return true;
			}
		}
		return false;
	}

	public float DistanceToShape(Vector3 global_pos)
	{
		Vector3 vector = global_pos - base.transform.position;
		float num = float.MaxValue;
		for (int i = 0; i < shapeCircles.Length; i++)
		{
			Circle circle = shapeCircles[i];
			float num2 = vector.x - circle.pos.x;
			float num3 = vector.z - circle.pos.z;
			float num4 = num2 * num2 + num3 * num3;
			if (num4 < circle.radiusSq)
			{
				return 0f;
			}
			float num5 = Mathf.Sqrt(num4) - circle.radius;
			if (num5 < num)
			{
				num = num5;
			}
		}
		return num;
	}

	public IEnumerator KGenerateEcology()
	{
		KoroutineId kid = SetFinalizer();
		try
		{
			ecology = new Ecology();
			ecology.Init(this);
			bool done = false;
			int n_loops = 30;
			int stuck = 0;
			while (!done)
			{
				ecology.Generate(pollution, ref stuck, n_loops, delegate
				{
					done = true;
				});
				yield return null;
			}
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public void AddPollution(float p)
	{
		pollution += p / surfaceFactor;
		if (DebugSettings.standard.logPollution)
		{
			Debug.Log($"Pollution on ground {base.name}: {pollution:0.0}");
		}
	}

	public float GetPollution()
	{
		return pollution;
	}

	public void SetConvex(bool target)
	{
		if (colliders == null)
		{
			colliders = GetComponentsInChildren<Collider>();
		}
		Collider[] array = colliders;
		foreach (Collider collider in array)
		{
			if (collider != null && collider is MeshCollider meshCollider)
			{
				meshCollider.convex = target;
			}
		}
	}

	public void SetArrowPointer(string s, float? size = null)
	{
		foreach (ArrowPointer3D spawnedArrow in spawnedArrows)
		{
			if (spawnedArrow != null)
			{
				UnityEngine.Object.Destroy(spawnedArrow.gameObject);
			}
		}
		spawnedArrows.Clear();
		if (!size.HasValue)
		{
			return;
		}
		if (s.ToLower() == "ant")
		{
			foreach (Ant item in GameManager.instance.EAnts())
			{
				SpawnArrow(size.Value, item.transform);
			}
			return;
		}
		if (Enum.TryParse<PickupType>(s, out var result))
		{
			foreach (Pickup item2 in GameManager.instance.EAllPickupsOfType(result))
			{
				SpawnArrow(size.Value, item2.transform);
			}
			return;
		}
		bool flag = false;
		foreach (StringTransform arrowLocation in arrowLocations)
		{
			if (arrowLocation.text.ToUpper() == s.ToUpper())
			{
				SpawnArrow(size.Value, arrowLocation.trans);
				flag = true;
			}
		}
		if (!flag)
		{
			Debug.LogWarning("SetArrowPointer: Couldn't find target for string " + s);
		}
	}

	private void SpawnArrow(float size, Transform parent)
	{
		ArrowPointer3D component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(ArrowPointer3D))).GetComponent<ArrowPointer3D>();
		component.SetTarget(parent);
		component.SetSize(size);
		spawnedArrows.Add(component);
	}

	public void AddBuilding(Building build)
	{
		if (!allBuildings.Contains(build))
		{
			allBuildings.Add(build);
		}
		if (build is Stockpile item && !allStockpiles.Contains(item))
		{
			allStockpiles.Add(item);
			allStockpiles.Sort((Stockpile s1, Stockpile s2) => s1.inventoryPriority.CompareTo(s2.inventoryPriority));
		}
		if (build is BatteryBuilding item2 && !allBatteryBuildings.Contains(item2))
		{
			allBatteryBuildings.Add(item2);
		}
		if (build.countAsAnt && !allAntBuildings.Contains(build))
		{
			allAntBuildings.Add(build);
		}
	}

	public void RemoveBuilding(Building build)
	{
		if (allBuildings.Contains(build))
		{
			allBuildings.Remove(build);
		}
		if (build is Stockpile item && allStockpiles.Contains(item))
		{
			allStockpiles.Remove(item);
		}
		if (build is BatteryBuilding item2 && allBatteryBuildings.Contains(item2))
		{
			allBatteryBuildings.Remove(item2);
		}
		if (build.countAsAnt && allAntBuildings.Contains(build))
		{
			allAntBuildings.Remove(build);
		}
	}

	public IEnumerable<Building> EBuildings()
	{
		foreach (Building allBuilding in allBuildings)
		{
			yield return allBuilding;
		}
	}

	public IEnumerable<Stockpile> EStockpiles()
	{
		foreach (Stockpile allStockpile in allStockpiles)
		{
			yield return allStockpile;
		}
	}

	public int GetBuildingCount(string code, bool only_completed = false)
	{
		int num = 0;
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding.IsPlaced() && allBuilding.data.code == code && ((allBuilding.currentStatus == BuildingStatus.BUILDING && !only_completed) || allBuilding.currentStatus == BuildingStatus.COMPLETED))
			{
				num++;
			}
		}
		return num;
	}

	public bool EnergyAvailable(out bool found_battery)
	{
		found_battery = false;
		foreach (BatteryBuilding allBatteryBuilding in allBatteryBuildings)
		{
			found_battery = true;
			if (allBatteryBuilding.storedEnergy > 0f)
			{
				return true;
			}
		}
		return false;
	}

	public float GetEnergy(float amount)
	{
		if (allBatteryBuildings.Count == 0)
		{
			return 0f;
		}
		int num = 1;
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			List<BatteryBuilding> list = new List<BatteryBuilding>();
			foreach (BatteryBuilding allBatteryBuilding in allBatteryBuildings)
			{
				if (allBatteryBuilding.storedEnergy > 0f)
				{
					list.Add(allBatteryBuilding);
				}
			}
			if ((float)list.Count == 0f)
			{
				return num2;
			}
			float num3 = amount - num2;
			foreach (BatteryBuilding item in list)
			{
				num2 += item.GetEnergy(num3 / (float)list.Count);
			}
			if (num2 < amount && num < 100)
			{
				num++;
			}
		}
		return num2;
	}

	public void AddPickupOnGround(Pickup p)
	{
		if (!allPickupsOnGround.Contains(p))
		{
			allPickupsOnGround.Add(p);
		}
	}

	public void RemovePickupOnGround(Pickup p)
	{
		if (allPickupsOnGround.Contains(p))
		{
			allPickupsOnGround.Remove(p);
		}
	}

	public IEnumerable<Pickup> EPickupsOnGround(PickupType _type)
	{
		foreach (Pickup item in allPickupsOnGround)
		{
			if (item.type == _type)
			{
				yield return item;
			}
		}
	}

	public IEnumerable<Pickup> EPickupsOnGround()
	{
		foreach (Pickup item in allPickupsOnGround)
		{
			yield return item;
		}
	}

	public void AddAnt(Ant ant)
	{
		allAnts.Add(ant);
		UpdateAntCount();
	}

	public void RemoveAnt(Ant ant)
	{
		allAnts.Remove(ant);
		UpdateAntCount();
	}

	public IEnumerable<Ant> EAnts()
	{
		foreach (Ant allAnt in allAnts)
		{
			if (!(allAnt == null))
			{
				yield return allAnt;
			}
		}
	}

	public void UpdateAntCount()
	{
		antCount = 0;
		foreach (Ant allAnt in allAnts)
		{
			if (!allAnt.IsDead())
			{
				antCount++;
			}
		}
		foreach (Building allBuilding in allBuildings)
		{
			if (allBuilding.countAsAnt)
			{
				antCount++;
			}
		}
	}

	public int Population()
	{
		return antCount;
	}

	public Dictionary<PickupType, int> UpdateInventory(ref Dictionary<PickupType, int> dic_total_inventory)
	{
		dicInventory.Clear();
		foreach (PickupType item in PickupData.EAllPickupTypes())
		{
			dicInventory[item] = 0;
		}
		foreach (Stockpile allStockpile in allStockpiles)
		{
			if (allStockpile.currentStatus != BuildingStatus.COMPLETED)
			{
				continue;
			}
			foreach (PickupType extractablePickup in allStockpile.GetExtractablePickups(ExchangeType.BUILDING_OUT))
			{
				int collectedAmount = allStockpile.GetCollectedAmount(extractablePickup, BuildingStatus.COMPLETED, include_incoming: false);
				dicInventory[extractablePickup] += collectedAmount;
				dic_total_inventory[extractablePickup] += collectedAmount;
			}
		}
		return dicInventory;
	}

	public int GetInventoryAmount(PickupType pickup_type)
	{
		return dicInventory[pickup_type];
	}

	public IEnumerable<KeyValuePair<PickupType, int>> EInventory()
	{
		foreach (KeyValuePair<PickupType, int> item in dicInventory)
		{
			yield return item;
		}
	}

	public IEnumerable<Stockpile> EStockpilesForExtract(PickupType pickup_type, bool only_open_to_smart)
	{
		foreach (Stockpile allStockpile in allStockpiles)
		{
			if (allStockpile.HasExtractablePickup(ExchangeType.BUILDING_OUT, pickup_type) && (!only_open_to_smart || allStockpile.OpenToSmartDispensers()))
			{
				yield return allStockpile;
			}
		}
	}

	public Stockpile GetStockpileForInsert(PickupType pickup_type, Vector3 near_pos, Storage exclude_stockpile, bool exclude_empty_stockpiles = false)
	{
		Stockpile result = null;
		float num = float.MinValue;
		foreach (Stockpile allStockpile in allStockpiles)
		{
			bool let_ant_wait = false;
			if (allStockpile != exclude_stockpile && allStockpile.CanInsert(pickup_type, ExchangeType.BUILDING_IN, null, ref let_ant_wait) && (!exclude_empty_stockpiles || !allStockpile.IsEmpty()))
			{
				float num2 = ((allStockpile.GetCollectedAmount(pickup_type, BuildingStatus.COMPLETED, include_incoming: false) > 0) ? 10000f : 0f);
				num2 -= Mathf.Clamp((allStockpile.transform.position - near_pos).sqrMagnitude / 10000f, 0f, 10000f);
				if (num2 > num)
				{
					num = num2;
					result = allStockpile;
				}
			}
		}
		return result;
	}

	public int GetStockpileSpace(PickupType pickup_type)
	{
		int num = 0;
		bool let_ant_wait = false;
		foreach (Stockpile allStockpile in allStockpiles)
		{
			if (allStockpile.CanInsert(pickup_type, ExchangeType.BUILDING_IN, null, ref let_ant_wait) && allStockpile.HasSpaceLeft(pickup_type, PileType.NONE, null, out var n))
			{
				num += n;
			}
		}
		return num;
	}

	public bool IsBusy()
	{
		return antCount >= 20;
	}

	public int GetBusyness()
	{
		return Mathf.FloorToInt(antCount / 20);
	}

	protected override void OnDestroy()
	{
		if (matsMap != null)
		{
			foreach (Material item in matsMap)
			{
				if (item != null)
				{
					UnityEngine.Object.Destroy(item);
				}
			}
		}
		base.OnDestroy();
	}
}
