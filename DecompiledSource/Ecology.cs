using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Ecology
{
	private class Species
	{
		public PlantType type;

		public PlantData data;

		public bool invasive;

		private Ecology ecology;

		private string code;

		private GameObject prefab;

		private Plant prefabPlant;

		private float distMin;

		private float distMax;

		public float pollutionFactor;

		public float coverage;

		public int targetAmount;

		private int change;

		private float remainingTime;

		private int spreadFrameIndex;

		public List<Plant> plants;

		private List<Plant> deadPlants;

		private List<List<Plant>> clusters;

		private int obstructionMaskStrong;

		private int obstructionMaskWeak;

		private List<(Vector3, float)> spawnPoints;

		public Species(PlantType plant_type, Ecology ecology)
		{
			type = plant_type;
			data = PlantData.Get(type);
			if (data == null)
			{
				return;
			}
			this.ecology = ecology;
			code = type.ToString();
			BiomeObjectData biomeObjectData = BiomeObjectData.Get(code);
			prefab = biomeObjectData.prefab;
			prefabPlant = prefab.GetComponent<Plant>();
			plants = new List<Plant>();
			deadPlants = new List<Plant>();
			clusters = new List<List<Plant>>();
			if (biomeObjectData.trailsPassThrough)
			{
				obstructionMaskStrong = Toolkit.Mask(Layers.Default, Layers.Buildings, Layers.Scenery, Layers.Ants, Layers.Pickups, Layers.Sources, Layers.FloorTiles);
			}
			else
			{
				obstructionMaskStrong = Toolkit.Mask(Layers.Default, Layers.Buildings, Layers.Scenery, Layers.Ants, Layers.Trails, Layers.Pickups, Layers.Sources, Layers.FloorTiles);
			}
			obstructionMaskWeak = Toolkit.Mask(Layers.Plants, Layers.BigPlants);
			distMin = data.distMin;
			distMax = data.distMax;
			Ground ground = ecology.ground;
			if (ground.groovePoints == null || ground.groovePoints.Count <= 0 || data.ignoreGrooves)
			{
				return;
			}
			spawnPoints = new List<(Vector3, float)>();
			float num = float.MaxValue;
			float num2 = float.MaxValue;
			float num3 = float.MinValue;
			float num4 = float.MinValue;
			foreach (Vector2 groovePoint in ground.groovePoints)
			{
				if (groovePoint.x < num)
				{
					num = groovePoint.x;
				}
				if (groovePoint.x > num3)
				{
					num3 = groovePoint.x;
				}
				if (groovePoint.y < num2)
				{
					num2 = groovePoint.y;
				}
				if (groovePoint.y > num4)
				{
					num4 = groovePoint.y;
				}
			}
			float num5 = 10f;
			int num6 = Mathf.RoundToInt((num3 - num) / 10f);
			int num7 = Mathf.RoundToInt((num4 - num2) / 10f);
			float[,] array = new float[num6, num7];
			if (ground.groovePlantDistribution.type == DistributionType.NEAR_ELEMENT)
			{
				Debug.LogWarning("groovePlantDistribution not expected NEAR_ELEMENT");
			}
			ground.groovePlantDistribution.Fill(array, 1f);
			foreach (Vector2 groovePoint2 in ground.groovePoints)
			{
				int num8 = Mathf.Clamp(Mathf.RoundToInt((groovePoint2.x - num) / num5), 0, num6 - 1);
				int num9 = Mathf.Clamp(Mathf.RoundToInt((groovePoint2.y - num2) / num5), 0, num7 - 1);
				float num10 = array[num8, num9];
				if (num10 > 0f)
				{
					spawnPoints.Add((ground.transform.TransformPoint(new Vector3(groovePoint2.x, 0f, groovePoint2.y)), num10));
				}
			}
			spawnPoints.Shuffle();
			spawnPoints.Sort(((Vector3, float) p1, (Vector3, float) p2) => p1.Item2.CompareTo(p2.Item2));
		}

		public void Write(Save save)
		{
			foreach (Plant item in new List<Plant>(plants))
			{
				if (item == null)
				{
					Debug.LogError("Removed null from plants");
					plants.Remove(item);
				}
			}
			save.Write(plants.Count + deadPlants.Count);
			foreach (Plant plant in plants)
			{
				plant.Write(save);
			}
			foreach (Plant deadPlant in deadPlants)
			{
				deadPlant.Write(save);
			}
		}

		public void Read(Save save)
		{
			int num = save.ReadInt();
			for (int i = 0; i < num; i++)
			{
				SpawnPlant(save.ReadVector3(), save.ReadYRot(), save.ReadFloat(), save);
			}
		}

		public void SetLinkIds(ref int id)
		{
			foreach (Plant plant in plants)
			{
				plant.linkId = ++id;
			}
			foreach (Plant deadPlant in deadPlants)
			{
				deadPlant.linkId = 0;
			}
		}

		public void LoadLinkPickups()
		{
			foreach (Plant plant in plants)
			{
				plant.LoadLinkPickups();
			}
		}

		public Plant SpawnPlant(Vector3 pos, PlantState start_state)
		{
			Plant plant = SpawnPlant(pos, Toolkit.RandomYRotation(), data.scaleRange.GetRandom());
			plant.SetState(start_state);
			return plant;
		}

		public Plant SpawnPlant(Vector3 pos, Quaternion rot, float size, Save from_save = null)
		{
			Transform parent = ((GameManager.instance == null) ? null : GameManager.instance.spawnParent);
			Plant component = UnityEngine.Object.Instantiate(prefab, pos, rot, parent).GetComponent<Plant>();
			component.Fill(type);
			component.ecology = ecology;
			component.spawnSize = size;
			if (from_save != null)
			{
				component.Read(from_save);
			}
			component.Init(from_save != null);
			GetList(component).Add(component);
			if (GameManager.instance != null)
			{
				foreach (Animator randomizable in component.randomizables)
				{
					GameManager.instance.InitAnimator(randomizable, AnimationCulling.Always);
				}
			}
			return component;
		}

		private List<Plant> GetList(Plant plant)
		{
			if (plant.state == PlantState.Dead || plant.state == PlantState.Remove)
			{
				return deadPlants;
			}
			return plants;
		}

		public void UpdatePlants(float dt)
		{
			int count = plants.Count;
			if (count < targetAmount)
			{
				if (UpdateChange(1, dt))
				{
					SpreadNew(new_island: false);
				}
			}
			else if (count > targetAmount)
			{
				if (UpdateChange(-1, dt))
				{
					WiltOne();
				}
			}
			else
			{
				UpdateChange(0, dt);
			}
			float dt2 = dt * 10f;
			int num = plants.Count - 1;
			for (num -= (10 + num - spreadFrameIndex) % 10; num >= 0; num -= 10)
			{
				Plant plant = plants[num];
				plant.UpdateGrowWilt(dt2);
				if (plant.state == PlantState.Dead)
				{
					plants.RemoveAt(num);
					deadPlants.Add(plant);
				}
			}
			num = deadPlants.Count - 1;
			for (num -= (10 + num - spreadFrameIndex) % 10; num >= 0; num -= 10)
			{
				Plant plant2 = deadPlants[num];
				plant2.UpdateGrowWilt(dt2);
				if (plant2.state == PlantState.Remove)
				{
					DestroyPlant(plant2);
					deadPlants.RemoveAt(num);
				}
			}
			spreadFrameIndex++;
			if (spreadFrameIndex >= 10)
			{
				spreadFrameIndex = 0;
			}
		}

		public bool UpdateChange(int c, float dt)
		{
			if (change != c)
			{
				change = c;
				if (change == -1)
				{
					remainingTime = data.wiltDelay;
				}
				else if (change == 1)
				{
					remainingTime = data.spreadDelay;
				}
			}
			if (change == 0)
			{
				return false;
			}
			remainingTime -= dt;
			if (remainingTime < 0f)
			{
				change = 0;
				return true;
			}
			return false;
		}

		private bool IsObstructed(Vector3 pos, bool check_plants_separately, out bool by_plants)
		{
			by_plants = false;
			if (check_plants_separately)
			{
				if (Physics.CheckSphere(pos, distMin, obstructionMaskStrong))
				{
					return true;
				}
				if (Physics.CheckSphere(pos, distMin, obstructionMaskWeak))
				{
					by_plants = true;
					return true;
				}
				return false;
			}
			return Physics.CheckSphere(pos, distMin, obstructionMaskStrong | obstructionMaskWeak);
		}

		public bool SpreadNew(bool new_island, PlantState plant_state = PlantState.Growing)
		{
			List<Plant> cluster = null;
			bool found;
			Vector3 pos = ((spawnPoints != null) ? GetScoredSpawnPos(out found) : ((!(data.evenClustering && new_island)) ? GetClusteredSpawnPos(out found) : GetEvenlyClusteredSpawnPos(out found, out cluster)));
			if (!found)
			{
				return false;
			}
			Plant item = SpawnPlant(pos, plant_state);
			if (cluster != null)
			{
				cluster.Add(item);
			}
			else
			{
				clusters.Add(new List<Plant> { item });
			}
			return true;
		}

		private Vector3 GetScoredSpawnPos(out bool found)
		{
			int num = 6;
			bool flag = false;
			Vector3 vector = Vector3.zero;
			Vector3 vector2 = Vector3.zero;
			found = false;
			for (int i = 0; i < num; i++)
			{
				int index = Mathf.Min(UnityEngine.Random.Range(0, spawnPoints.Count), UnityEngine.Random.Range(0, spawnPoints.Count));
				vector = spawnPoints[index].Item1;
				if (!IsObstructed(vector, !flag, out var by_plants))
				{
					found = true;
					break;
				}
				if (!flag && by_plants)
				{
					vector2 = vector;
				}
			}
			if (!found && flag)
			{
				found = true;
				vector = vector2;
			}
			return vector;
		}

		private Vector3 GetClusteredSpawnPos(out bool found)
		{
			List<Plant> list = plants;
			Vector3 spawn_pos = Vector3.zero;
			found = false;
			float extraEdgeCheckRadius = prefabPlant.extraEdgeCheckRadius;
			List<Vector3> options = new List<Vector3>();
			if (list.Count > 0 && UnityEngine.Random.value < data.clustering)
			{
				for (int i = 0; i < 3; i++)
				{
					Plant plant = list[UnityEngine.Random.Range(0, list.Count)];
					for (int j = 0; j < 3; j++)
					{
						options.Add(plant.transform.position + Toolkit.GetRandomInDonut(distMin * 1.01f, distMax));
					}
				}
				found = ValidPosIn(ref options, ref spawn_pos, extraEdgeCheckRadius);
			}
			if (!found)
			{
				options.Clear();
				Vector3 position = ecology.ground.transform.position;
				for (int k = 0; k < 9; k++)
				{
					options.Add(position + ecology.ground.GetRandomPosInShape());
				}
				found = ValidPosIn(ref options, ref spawn_pos, extraEdgeCheckRadius);
			}
			return spawn_pos;
		}

		private Vector3 GetEvenlyClusteredSpawnPos(out bool found, out List<Plant> cluster)
		{
			List<List<Plant>> list = clusters;
			Vector3 spawn_pos = Vector3.zero;
			cluster = null;
			found = false;
			float extraEdgeCheckRadius = prefabPlant.extraEdgeCheckRadius;
			List<Vector3> options = new List<Vector3>();
			if (list.Count > 0 && UnityEngine.Random.value < data.clustering)
			{
				cluster = list[UnityEngine.Random.Range(0, list.Count)];
				for (int i = 0; i < 3; i++)
				{
					Plant plant = cluster[UnityEngine.Random.Range(0, cluster.Count)];
					for (int j = 0; j < 3; j++)
					{
						options.Add(plant.transform.position + Toolkit.GetRandomInDonut(distMin * 1.01f, distMax));
					}
				}
				found = ValidPosIn(ref options, ref spawn_pos, extraEdgeCheckRadius);
			}
			if (!found)
			{
				cluster = null;
				options.Clear();
				Vector3 position = ecology.ground.transform.position;
				for (int k = 0; k < 9; k++)
				{
					options.Add(position + ecology.ground.GetRandomPosInShape());
				}
				found = ValidPosIn(ref options, ref spawn_pos, extraEdgeCheckRadius);
			}
			return spawn_pos;
		}

		private bool ValidPosIn(ref List<Vector3> options, ref Vector3 spawn_pos, float edge_radius)
		{
			bool flag = false;
			Vector3 vector = spawn_pos;
			foreach (Vector3 option in options)
			{
				if ((!IsObstructed(option, !flag, out var by_plants) || (!flag && by_plants)) && Toolkit.GetGroundPos(option, out spawn_pos) && (!(edge_radius > 5f) || !Toolkit.IsOverEdge(option, edge_radius, 4)))
				{
					if (!by_plants)
					{
						return true;
					}
					flag = true;
					vector = spawn_pos;
				}
			}
			if (flag)
			{
				spawn_pos = vector;
				return true;
			}
			return false;
		}

		private void WiltOne()
		{
			plants[UnityEngine.Random.Range(0, plants.Count)].SetState(PlantState.Dead);
		}

		public bool HasDiedOut()
		{
			if (plants.Count == 0)
			{
				return targetAmount == 0;
			}
			return false;
		}

		private void DestroyPlant(Plant plant)
		{
			if (Gameplay.instance != null)
			{
				foreach (Animator randomizable in plant.randomizables)
				{
					GameManager.instance.RemovePausableAnimator(randomizable);
				}
				plant.ecology = null;
				plant.Delete();
			}
			else
			{
				UnityEngine.Object.Destroy(plant.gameObject);
			}
		}

		public void GotRemoved(Plant plant)
		{
			GetList(plant).Remove(plant);
		}

		public void Delete()
		{
			foreach (Plant plant in plants)
			{
				DestroyPlant(plant);
			}
			foreach (Plant deadPlant in deadPlants)
			{
				DestroyPlant(deadPlant);
			}
		}

		public string GetAmountInfo()
		{
			return string.Format("{0}: {1}{2}", type, plants.Count, (plants.Count == targetAmount) ? "" : $" -> {targetAmount}");
		}
	}

	private List<Species> specieses;

	private Biome biome;

	private Ground ground;

	public void Init(Ground ground, bool during_load = false)
	{
		this.ground = ground;
		biome = ground.biome;
		specieses = new List<Species>();
		if (during_load)
		{
			return;
		}
		foreach (PlantType plantType in biome.plantTypes)
		{
			Species species = new Species(plantType, this);
			if (species.data != null)
			{
				specieses.Add(species);
			}
		}
	}

	public void ReadPart(Save save)
	{
		Species species = new Species((PlantType)save.ReadInt(), this);
		species.Read(save);
		specieses.Add(species);
	}

	public void Write(Save save)
	{
		save.Write(specieses.Count);
		foreach (Species speciese in specieses)
		{
			save.Write((int)speciese.type);
			speciese.Write(save);
		}
	}

	public void SetLinkIds(ref int id)
	{
		foreach (Species speciese in specieses)
		{
			speciese.SetLinkIds(ref id);
		}
	}

	public void LoadLinkPickups()
	{
		foreach (Species speciese in specieses)
		{
			speciese.LoadLinkPickups();
		}
	}

	public void Generate(float global_pollution, ref int stuck, int max = -1, Action<bool> func_done = null)
	{
		CalcAmounts(global_pollution);
		bool flag = false;
		int num = 0;
		while (!flag && stuck < 100 && (max == -1 || num < max))
		{
			flag = true;
			foreach (Species speciese in specieses)
			{
				if (speciese.plants.Count < speciese.targetAmount)
				{
					flag = false;
					num++;
					if (speciese.SpreadNew(new_island: true, PlantState.Grown))
					{
						stuck = 0;
					}
					else
					{
						stuck++;
					}
				}
			}
		}
		if (flag)
		{
			func_done?.Invoke(obj: true);
		}
		else if (max == -1 || num < max)
		{
			Debug.LogError("Plants getting stuck");
			func_done?.Invoke(obj: false);
		}
	}

	public void Update(float global_pollution, float dt)
	{
		CalcAmounts(global_pollution);
		UpdatePlants(dt);
	}

	private void CalcAmounts(float global_pollution)
	{
		float num = 0f;
		foreach (Species speciese in specieses)
		{
			float num2 = Mathf.Clamp01(1f - Mathf.Max(speciese.data.pollutionRange.min - global_pollution, global_pollution - speciese.data.pollutionRange.max) / Mathf.Max(speciese.data.pollutionTolerance, 0.01f));
			float num3 = num2 * speciese.data.dominance;
			speciese.pollutionFactor = num2;
			speciese.coverage = num3;
			num += num3;
		}
		foreach (Species speciese2 in specieses)
		{
			speciese2.targetAmount = Mathf.RoundToInt((num == 0f) ? 0f : ((float)Mathf.RoundToInt(biome.fertility * ground.surfaceFactor * speciese2.pollutionFactor * (speciese2.coverage / num) / speciese2.data.mass)));
		}
	}

	private void UpdatePlants(float dt)
	{
		foreach (Species speciese in specieses)
		{
			speciese.UpdatePlants(dt);
		}
		for (int num = specieses.Count - 1; num >= 0; num--)
		{
			if (specieses[num].invasive && specieses[num].HasDiedOut())
			{
				specieses.RemoveAt(num);
			}
		}
	}

	public void AddNewSpecies(PlantType plant_type)
	{
		Species species = new Species(plant_type, this);
		if (species.data != null)
		{
			species.invasive = true;
			specieses.Add(species);
		}
	}

	public void AppendSpeciesAmountInfo(StringBuilder sb)
	{
		foreach (Species speciese in specieses)
		{
			sb.AppendLine(" - " + speciese.GetAmountInfo());
		}
	}

	public Plant FindClosestPlant(Vector3 pos)
	{
		float num = float.MaxValue;
		Plant result = null;
		foreach (Species speciese in specieses)
		{
			foreach (Plant plant in speciese.plants)
			{
				float sqrMagnitude = (plant.transform.position - pos).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = plant;
				}
			}
		}
		return result;
	}

	public IEnumerable<Plant> EPlants()
	{
		foreach (Species speciese in specieses)
		{
			foreach (Plant plant in speciese.plants)
			{
				yield return plant;
			}
		}
	}

	public void GotRemoved(Plant plant)
	{
		foreach (Species speciese in specieses)
		{
			if (speciese.type == plant.type)
			{
				speciese.GotRemoved(plant);
			}
		}
	}

	public void Delete()
	{
		foreach (Species speciese in specieses)
		{
			speciese.Delete();
		}
	}
}
