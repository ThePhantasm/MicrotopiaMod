using System;
using System.Collections.Generic;
using UnityEngine;

public class BiomeObject : PickupContainer
{
	public struct Circum
	{
		public Vector2 pos;

		public Vector2 dir;

		public Circum(Vector2 _pos, Vector2 _dir)
		{
			pos = _pos;
			dir = _dir;
		}

		public override bool Equals(object obj)
		{
			return Equals((Circum)obj);
		}

		public override int GetHashCode()
		{
			return pos.GetHashCode();
		}

		public static bool operator ==(Circum a, Circum b)
		{
			return a.pos == b.pos;
		}

		public static bool operator !=(Circum a, Circum b)
		{
			return a.pos != b.pos;
		}
	}

	[Header("BiomeObject")]
	public List<SourceMesh> meshes;

	public List<Animator> randomizables = new List<Animator>();

	public List<GameObject> sideObjects = new List<GameObject>();

	[NonSerialized]
	protected SourceMesh currentMesh;

	protected Dictionary<PickupType, int> startPickups = new Dictionary<PickupType, int>();

	protected Dictionary<PickupType, int> subtractedPickups = new Dictionary<PickupType, int>();

	private HashSet<PickupType> extractablePickups = new HashSet<PickupType>();

	private bool multiplePickups;

	protected int substractedValue_old = -1;

	[NonSerialized]
	public int meshIndex = -1;

	[NonSerialized]
	public string code;

	[NonSerialized]
	public float spawnSize;

	[SerializeField]
	protected EffectArea effectArea;

	public BiomeObjectData data;

	[NonSerialized]
	public Ground ground;

	private List<Vector2> circumPoses;

	private List<Vector2> circumDirs;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(meshIndex);
		save.Write(GameManager.instance.GetGroundInstanceId(ground));
		save.Write(subtractedPickups.Count);
		foreach (KeyValuePair<PickupType, int> subtractedPickup in subtractedPickups)
		{
			save.Write((int)subtractedPickup.Key);
			save.Write(subtractedPickup.Value);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		meshIndex = save.ReadInt();
		if (save.version < 36)
		{
			substractedValue_old = save.ReadInt();
		}
		if (save.version >= 28)
		{
			int id = save.ReadInt();
			ground = GameManager.instance.GetGroundInstance(id);
		}
		if (save.version >= 36)
		{
			int num = save.ReadInt();
			subtractedPickups = new Dictionary<PickupType, int>();
			for (int i = 0; i < num; i++)
			{
				PickupType key = (PickupType)save.ReadInt();
				int value = save.ReadInt();
				subtractedPickups.Add(key, value);
			}
		}
	}

	public void Fill(BiomeObjectData bob_data)
	{
		data = bob_data;
	}

	private void Awake()
	{
		if (effectArea.statusEffect == StatusEffect.NONE)
		{
			effectArea = null;
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		Fill(BiomeObjectData.Get(code));
		SetMesh();
		if (data.HasPickups())
		{
			startPickups = new Dictionary<PickupType, int>();
			if (!during_load)
			{
				subtractedPickups = new Dictionary<PickupType, int>();
			}
			int num = 0;
			foreach (PickupCost pickup in data.pickups)
			{
				startPickups.Add(pickup.type, Mathf.Clamp(Mathf.RoundToInt((float)pickup.intValue * spawnSize * currentMesh.multiplier), 1, int.MaxValue));
				if (!during_load)
				{
					subtractedPickups.Add(pickup.type, 0);
				}
				else if (!subtractedPickups.ContainsKey(pickup.type))
				{
					subtractedPickups.Add(pickup.type, 0);
				}
				num++;
			}
			multiplePickups = num > 1;
			if (during_load && substractedValue_old != -1)
			{
				subtractedPickups = new Dictionary<PickupType, int>();
				foreach (PickupCost pickup2 in data.pickups)
				{
					subtractedPickups.Add(pickup2.type, substractedValue_old);
					substractedValue_old = 0;
				}
			}
		}
		SetSize();
		if (effectArea != null)
		{
			effectArea.SetActive(_active: true, base.transform.position, force: true);
		}
		foreach (Animator randomizable in randomizables)
		{
			if (randomizable.isActiveAndEnabled)
			{
				float value = UnityEngine.Random.Range(0.5f, 2f);
				randomizable.SetFloat(ClickableObject.paramSpeed, value);
				randomizable.keepAnimatorControllerStateOnDisable = true;
				randomizable.cullingMode = AnimatorCullingMode.CullCompletely;
			}
		}
		foreach (GameObject sideObject in sideObjects)
		{
			sideObject.transform.parent = null;
		}
		if (data.pollution > 0f && ground != null)
		{
			ground.AddPollution(data.pollution);
		}
	}

	public float GetRadiusWhenNoMeshSelectedYet()
	{
		float num = float.MaxValue;
		float num2 = float.MinValue;
		foreach (SourceMesh mesh in meshes)
		{
			if (!(mesh.sidePoint == null))
			{
				radius = (base.transform.position.XZ() - mesh.sidePoint.position.XZ()).magnitude;
				if (radius < num)
				{
					num = radius;
				}
				if (radius > num2)
				{
					num2 = radius;
				}
			}
		}
		if (num == float.MaxValue)
		{
			return 1f;
		}
		return Mathf.Lerp(num, num2, 0.8f);
	}

	protected void SetMesh()
	{
		if (!(currentMesh == null) || meshes.Count <= 0)
		{
			return;
		}
		if (meshIndex < 0 || meshIndex >= meshes.Count)
		{
			float num = 0f;
			foreach (SourceMesh mesh in meshes)
			{
				num += mesh.chance;
			}
			float num2 = UnityEngine.Random.Range(0f, num);
			float num3 = 0f;
			for (int i = 0; i < meshes.Count; i++)
			{
				if (num2 <= meshes[i].chance + num3)
				{
					meshIndex = i;
					break;
				}
				num3 += meshes[i].chance;
			}
		}
		currentMesh = meshes[meshIndex];
		for (int j = 0; j < meshes.Count; j++)
		{
			if (j != meshIndex)
			{
				UnityEngine.Object.Destroy(meshes[j].gameObject);
			}
		}
		meshes.Clear();
		currentMesh.SetObActive(active: true);
		if (currentMesh.topPoint != null)
		{
			topPoint = currentMesh.topPoint;
		}
		if (currentMesh.sidePoint != null)
		{
			sidePoint = currentMesh.sidePoint;
			radius = -1f;
		}
	}

	public override bool IsClickable()
	{
		return !data.unclickable;
	}

	public override void OnClickDelete()
	{
		if (DebugSettings.standard.DeletableEverything())
		{
			Delete();
		}
	}

	protected override void DoDelete()
	{
		if (data.pollution > 0f && ground != null)
		{
			ground.AddPollution(0f - data.pollution);
		}
		if (effectArea != null)
		{
			effectArea.SetActive(_active: false);
		}
		GameManager.instance.RemoveBiomeObject(this);
		foreach (GameObject sideObject in sideObjects)
		{
			sideObject.transform.parent = base.transform;
		}
		if (ground != null)
		{
			ground.UpdateNavMesh(base.transform.position, GetRadius());
		}
		base.DoDelete();
	}

	protected virtual void SetSize()
	{
		Vector3 localScale = Vector3.one * spawnSize;
		base.transform.localScale = localScale;
		if (data.HasPickups() && !HasPickups())
		{
			Delete();
		}
	}

	public override IEnumerable<ExchangeType> EPossibleExchangeTypes()
	{
		foreach (ExchangeType exchangeType in data.exchangeTypes)
		{
			if (exchangeType != ExchangeType.FORAGE || currentMesh.growPoints.Count > 0)
			{
				yield return exchangeType;
			}
		}
	}

	public override bool HasExchangeType(ExchangeType exchange_type)
	{
		foreach (ExchangeType exchangeType in data.exchangeTypes)
		{
			if (exchangeType == exchange_type)
			{
				return exchangeType != ExchangeType.FORAGE || currentMesh.growPoints.Count > 0;
			}
		}
		return false;
	}

	public override ExchangeType TrailInteraction(Trail _trail)
	{
		ExchangeType exchangeType = ExchangeType.NONE;
		if (HasPickups())
		{
			foreach (ExchangeType item in EPossibleExchangeTypes())
			{
				if (_trail.CanDoExchangeType(item))
				{
					if (exchangeType == ExchangeType.NONE)
					{
						exchangeType = item;
					}
					else if (item == ExchangeType.FORAGE)
					{
						exchangeType = item;
					}
				}
			}
		}
		return exchangeType;
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (HasExchangeType(exchange))
		{
			return HasPickups();
		}
		return false;
	}

	public override List<PickupType> GetExtractablePickupsInternal()
	{
		List<PickupType> list = new List<PickupType>();
		extractablePickups.Clear();
		int num = 0;
		foreach (KeyValuePair<PickupType, int> startPickup in startPickups)
		{
			int num2 = startPickup.Value;
			if (!data.infinite)
			{
				num2 -= subtractedPickups[startPickup.Key];
			}
			num += num2;
		}
		int num3 = num / 20;
		if (num3 == 0)
		{
			num3 = 1;
		}
		foreach (KeyValuePair<PickupType, int> startPickup2 in startPickups)
		{
			int num4 = startPickup2.Value;
			PickupType key = startPickup2.Key;
			if (!data.infinite)
			{
				num4 -= subtractedPickups[key];
			}
			if (num4 <= 0)
			{
				continue;
			}
			extractablePickups.Add(key);
			if (multiplePickups)
			{
				int num5 = num4 / num3;
				if (num5 == 0)
				{
					num5 = 1;
				}
				for (int i = 0; i < num5; i++)
				{
					list.Add(startPickup2.Key);
				}
			}
			else
			{
				list.Add(startPickup2.Key);
			}
		}
		return list;
	}

	public override bool HasExtractablePickup(ExchangeType exchange, PickupType pickup)
	{
		if (extractablePickupsChanged)
		{
			GetExtractablePickupsInternal();
		}
		return extractablePickups.Contains(pickup);
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		if (startPickups[_type] - subtractedPickups[_type] > 0 || data.infinite)
		{
			Pickup result = GameManager.instance.SpawnPickup(_type, GetExtractPos(), Toolkit.RandomYRotation());
			subtractedPickups[_type]++;
			extractablePickupsChanged = true;
			SetSize();
			return result;
		}
		return base.ExtractPickup(_type);
	}

	protected virtual bool HasPickups()
	{
		if (data.infinite)
		{
			return true;
		}
		foreach (KeyValuePair<PickupType, int> startPickup in startPickups)
		{
			if (subtractedPickups.TryGetValue(startPickup.Key, out var value) && startPickup.Value - value > 0)
			{
				return true;
			}
		}
		return false;
	}

	public float GetMineDuration(float mine_speed)
	{
		return GlobalValues.standard.baseMineDuration * data.hardness / mine_speed;
	}

	public override void SetHoverUI(UIHoverClickOb ui_hover)
	{
		ui_hover.SetTitle(data.GetTitle());
		if (data.HasPickups())
		{
			ui_hover.SetInventory();
		}
		if (DebugSettings.standard.DeletableEverything())
		{
			ui_hover.SetBottomButtons(OnClickDelete, null, null);
		}
	}

	public override void UpdateHoverUI(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI(ui_hover);
		if (!data.HasPickups())
		{
			return;
		}
		Dictionary<PickupType, string> dictionary = new Dictionary<PickupType, string>();
		foreach (KeyValuePair<PickupType, int> startPickup in startPickups)
		{
			if (data.infinite)
			{
				dictionary.Add(startPickup.Key, "∞");
			}
			else
			{
				dictionary.Add(startPickup.Key, (startPickup.Value - subtractedPickups[startPickup.Key]).ToString());
			}
		}
		ui_hover.inventoryGrid.Update(Loc.GetUI("BIOME_CONTAINS"), dictionary, Loc.GetUI("GENERIC_NOTHING"));
		ui_hover.SetCapabilities(Loc.GetUI("BIOME_COLLECTWITH"));
		foreach (TrailType item in TrailData.ExchangeTypesToTrailTypes(EPossibleExchangeTypes()))
		{
			ui_hover.AddCapability(item);
		}
	}

	public override UIClickType GetUiClickType()
	{
		if (data.pickups.Count > 4)
		{
			return UIClickType.BIOMEOBJECT_LARGE;
		}
		return UIClickType.BIOMEOBJECT;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		UIClickLayout_BiomeObject uIClickLayout_BiomeObject = (UIClickLayout_BiomeObject)ui_click;
		uIClickLayout_BiomeObject.SetTitle(data.GetTitle());
		uIClickLayout_BiomeObject.SetInventory(data.HasPickups());
		if (DebugSettings.standard.DeletableEverything())
		{
			uIClickLayout_BiomeObject.SetButton(UIClickButtonType.Delete, OnClickDelete, InputAction.Delete);
		}
		uIClickLayout_BiomeObject.UpdateButton(UIClickButtonType.Delete, DebugSettings.standard.DeletableEverything());
		uIClickLayout_BiomeObject.SetSlots(target: false);
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		base.UpdateClickUi(ui_click);
		UIClickLayout_BiomeObject uIClickLayout_BiomeObject = (UIClickLayout_BiomeObject)ui_click;
		if (!data.HasPickups())
		{
			return;
		}
		Dictionary<PickupType, string> dictionary = new Dictionary<PickupType, string>();
		foreach (KeyValuePair<PickupType, int> startPickup in startPickups)
		{
			if (data.infinite)
			{
				dictionary.Add(startPickup.Key, "∞");
			}
			else
			{
				dictionary.Add(startPickup.Key, (startPickup.Value - subtractedPickups[startPickup.Key]).ToString());
			}
		}
		uIClickLayout_BiomeObject.inventoryGrid.Update(Loc.GetUI("BIOME_CONTAINS"), dictionary, Loc.GetUI("GENERIC_NOTHING"));
		uIClickLayout_BiomeObject.SetCapabilities(Loc.GetUI("BIOME_COLLECTWITH"));
		foreach (TrailType item in TrailData.ExchangeTypesToTrailTypes(EPossibleExchangeTypes()))
		{
			uIClickLayout_BiomeObject.AddCapability(item);
		}
	}

	public IEnumerable<Circum> ECircums()
	{
		if (circumPoses == null)
		{
			FillCircums();
		}
		for (int i = 0; i < circumPoses.Count; i++)
		{
			yield return new Circum(circumPoses[i], circumDirs[i]);
		}
	}

	private void FillCircums()
	{
		float y = 0.5f;
		float num = 5f;
		float num2 = GetRadius();
		float num3 = MathF.PI * 2f * num2;
		float num4 = MathF.PI * 2f / (num3 / num);
		Vector3 vector = base.transform.position.SetY(y);
		circumPoses = new List<Vector2>();
		circumDirs = new List<Vector2>();
		float num5 = num2 * 2f;
		int layerMask = Toolkit.Mask((Layers)base.gameObject.layer);
		for (int i = 0; i < 2; i++)
		{
			for (float num6 = 0f; num6 < MathF.PI * 2f; num6 += num4)
			{
				Vector3 vector2 = new Vector3(Mathf.Cos(num6), 0f, Mathf.Sin(num6));
				int num7 = ((i != 0) ? Physics.SphereCastNonAlloc(vector, 1f, vector2, Toolkit.raycastHits, num5 * 1.5f, layerMask) : Physics.SphereCastNonAlloc(vector - vector2 * num5, 1f, vector2, Toolkit.raycastHits, num5 * 1.5f, layerMask));
				float num8 = float.MaxValue;
				bool flag = false;
				Vector2 vector3 = Vector2.zero;
				for (int j = 0; j < num7; j++)
				{
					RaycastHit raycastHit = Toolkit.raycastHits[j];
					float distance = raycastHit.distance;
					if (raycastHit.collider.GetComponentInParent<ClickableObject>() == this && distance > 0f && distance < num8)
					{
						vector3 = raycastHit.point.XZ();
						num8 = distance;
						flag = true;
					}
				}
				if (!flag)
				{
					continue;
				}
				bool flag2 = false;
				foreach (Vector2 circumPose in circumPoses)
				{
					if ((circumPose - vector3).sqrMagnitude < num * num)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					vector3 -= vector2.XZ() * 2.5f;
					circumPoses.Add(vector3);
					circumDirs.Add(vector2.XZ());
				}
			}
			if (i != 0)
			{
				continue;
			}
			int num9 = Physics.OverlapSphereNonAlloc(vector, 1f, Toolkit.overlapColliders, layerMask);
			bool flag3 = true;
			for (int k = 0; k < num9; k++)
			{
				if (Toolkit.overlapColliders[k].GetComponentInParent<ClickableObject>() == this)
				{
					flag3 = false;
					break;
				}
			}
			if (!flag3)
			{
				break;
			}
		}
	}
}
