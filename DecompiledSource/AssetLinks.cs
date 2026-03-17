using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "AssetLinks", menuName = "Microtopia/AssetLinks", order = 0)]
public class AssetLinks : ScriptableObject
{
	[Serializable]
	public class ExplosionAsset
	{
		public string name;

		public ExplosionType type;

		public GameObject prefab;
	}

	[Serializable]
	public class TrailTypeMaterial
	{
		public string name;

		public TrailType type;

		public TrailStatus status;

		public Material mat;

		public Material mat_low;

		public Material mat_cursor;

		public Sprite icon;

		public Color iconCol = Color.white;
	}

	[Serializable]
	public struct CursorInfo
	{
		public Texture2D image;

		public Vector2 hotspot;
	}

	[Serializable]
	public class AntStatusEffectMaterial
	{
		public StatusEffect statusEffect;

		public Material matBody;

		public Material matEyes;
	}

	[Serializable]
	public class PickupMaterial
	{
		public PickupType pickup;

		public Material mat;
	}

	public static AssetLinks standard;

	[Header("UI prefabs")]
	[SerializeField]
	private GameObject prefabUiGame;

	[SerializeField]
	private GameObject prefabUiDialogueBase;

	[SerializeField]
	private GameObject prefabUiDialogueNewBuilding;

	[SerializeField]
	private GameObject prefabUiDialogueInputField;

	[SerializeField]
	private GameObject prefabUiHoverBuilding;

	[SerializeField]
	private GameObject prefabUiItemMenu;

	[SerializeField]
	private GameObject prefabUiIconItem;

	[SerializeField]
	private GameObject prefabUiTechTree;

	[SerializeField]
	private GameObject prefabUiHover;

	[SerializeField]
	private GameObject prefabUiTutorial;

	[SerializeField]
	private GameObject prefabUiRecipeMenu;

	[SerializeField]
	private GameObject prefabUiEscMenu;

	[SerializeField]
	private GameObject prefabUiLoadSave;

	[SerializeField]
	private GameObject prefabUiSettings;

	[SerializeField]
	private GameObject prefabUiLoading;

	[SerializeField]
	private GameObject prefabUiReport;

	[SerializeField]
	private GameObject prefabUiFeedback;

	[SerializeField]
	private GameObject prefabUiBlueprints;

	[SerializeField]
	private GameObject prefabUiFloorSelection;

	[Header("Gameplay prefabs")]
	[SerializeField]
	private GameObject prefabTrail;

	[SerializeField]
	private GameObject prefabTrailSplit;

	[SerializeField]
	private GameObject prefabTrailGateSensor;

	[SerializeField]
	private GameObject prefabTrailGateCounter;

	[SerializeField]
	private GameObject prefabTrailGateLife;

	[SerializeField]
	private GameObject prefabTrailGateCarry;

	[SerializeField]
	private GameObject prefabTrailGateCaste;

	[SerializeField]
	private GameObject prefabTrailGateOld;

	[SerializeField]
	private GameObject prefabTrailGateNone;

	[SerializeField]
	private GameObject prefabTrailGateSpeed;

	[SerializeField]
	private GameObject prefabTrailGateTimer;

	[SerializeField]
	private GameObject prefabTrailGateStockpile;

	[SerializeField]
	private GameObject prefabTrailGateLink;

	[SerializeField]
	private GameObject prefabAntLaunchCollider;

	[Header("Status Effects")]
	[SerializeField]
	private GameObject statusEffectCharged;

	[SerializeField]
	private GameObject statusEffectHyper;

	[SerializeField]
	private GameObject statusEffectDiseased;

	[SerializeField]
	private GameObject statusEffectRadiated_Light;

	[SerializeField]
	private GameObject statusEffectRadiated_Medium;

	[SerializeField]
	private GameObject statusEffectRadiated_Heavy;

	[SerializeField]
	private GameObject statusEffectRadiation;

	[Header("Other prefabs")]
	[SerializeField]
	private GameObject prefabArrowPointer;

	[SerializeField]
	private GameObject prefabMouseCursor;

	[SerializeField]
	private GameObject prefabAssignLine;

	[SerializeField]
	private GameObject prefabBillboard;

	public GameObject prefabBlueprintCamera;

	[SerializeField]
	private GameObject prefabHologram;

	[SerializeField]
	private List<ExplosionAsset> explosions = new List<ExplosionAsset>();

	[Header("Images")]
	public CursorInfo cursorArrow;

	public CursorInfo cursorHand;

	public CursorInfo cursorHandHold;

	public CursorInfo cursorCameraMove;

	public CursorInfo cursorCameraRotate;

	public CursorInfo cursorTrailDraw;

	public CursorInfo cursorTrailErase;

	public CursorInfo cursorBuildingHover;

	public CursorInfo cursorBuildingPlace;

	public CursorInfo cursorBuildingRotate;

	public CursorInfo cursorClick;

	public CursorInfo cursorPipette;

	public Sprite spriteButtonBlueprints;

	[Header("Materials")]
	[SerializeField]
	private TrailTypeMaterial[] trailTypeMaterials;

	[SerializeField]
	private AntStatusEffectMaterial[] antMaterials;

	[SerializeField]
	private PickupMaterial[] pickupMaterials;

	[Header("References")]
	[SerializeField]
	private HighlightSetups highlightSetups;

	private Dictionary<AntCaste, Dictionary<StatusEffect, List<Material>>> dicdicAntStatusEffectMats = new Dictionary<AntCaste, Dictionary<StatusEffect, List<Material>>>();

	private List<StatusEffect> listStatusEffectWithMaterials = new List<StatusEffect>();

	public static IEnumerator CInit()
	{
		AsyncOperationHandle<AssetLinks> loading = Addressables.LoadAssetAsync<AssetLinks>("ScriptableObjects/AssetLinks");
		yield return loading;
		standard = loading.Result;
	}

	public GameObject GetPrefab(Type t)
	{
		if (t == typeof(UIGame))
		{
			return prefabUiGame;
		}
		if (t == typeof(UIDialogBase))
		{
			return prefabUiDialogueBase;
		}
		if (t == typeof(UIDialogNewBuilding))
		{
			return prefabUiDialogueNewBuilding;
		}
		if (t == typeof(UIDialogueInputField))
		{
			return prefabUiDialogueInputField;
		}
		if (t == typeof(UIHoverClickOb))
		{
			return prefabUiHoverBuilding;
		}
		if (t == typeof(UIItemMenu))
		{
			return prefabUiItemMenu;
		}
		if (t == typeof(UIIconItem))
		{
			return prefabUiIconItem;
		}
		if (t == typeof(UITechTree))
		{
			return prefabUiTechTree;
		}
		if (t == typeof(UIHover))
		{
			return prefabUiHover;
		}
		if (t == typeof(UITutorial))
		{
			return prefabUiTutorial;
		}
		if (t == typeof(UIRecipeMenu))
		{
			return prefabUiRecipeMenu;
		}
		if (t == typeof(UISettings))
		{
			return prefabUiSettings;
		}
		if (t == typeof(UIReportScreen))
		{
			return prefabUiReport;
		}
		if (t == typeof(UIFeedback))
		{
			return prefabUiFeedback;
		}
		if (t == typeof(UIEscMenu))
		{
			return prefabUiEscMenu;
		}
		if (t == typeof(UILoadSave))
		{
			return prefabUiLoadSave;
		}
		if (t == typeof(UILoading))
		{
			return prefabUiLoading;
		}
		if (t == typeof(UIBlueprints))
		{
			return prefabUiBlueprints;
		}
		if (t == typeof(UIFloorSelection))
		{
			return prefabUiFloorSelection;
		}
		if (t == typeof(Trail))
		{
			return prefabTrail;
		}
		if (t == typeof(Split))
		{
			return prefabTrailSplit;
		}
		if (t == typeof(TrailGate_Sensors))
		{
			return prefabTrailGateSensor;
		}
		if (t == typeof(TrailGate_Counter))
		{
			return prefabTrailGateCounter;
		}
		if (t == typeof(TrailGate_Life))
		{
			return prefabTrailGateLife;
		}
		if (t == typeof(TrailGate_Carry))
		{
			return prefabTrailGateCarry;
		}
		if (t == typeof(TrailGate_Caste))
		{
			return prefabTrailGateCaste;
		}
		if (t == typeof(TrailGate_Old))
		{
			return prefabTrailGateOld;
		}
		if (t == typeof(TrailGate_Speed))
		{
			return prefabTrailGateSpeed;
		}
		if (t == typeof(TrailGate_Timer))
		{
			return prefabTrailGateTimer;
		}
		if (t == typeof(TrailGate_Stockpile))
		{
			return prefabTrailGateStockpile;
		}
		if (t == typeof(TrailGate_Link))
		{
			return prefabTrailGateLink;
		}
		if (t == typeof(TrailGate_CounterEnd))
		{
			return prefabTrailGateNone;
		}
		if (t == typeof(AntLaunchCollider))
		{
			return prefabAntLaunchCollider;
		}
		if (t == typeof(ArrowPointer3D))
		{
			return prefabArrowPointer;
		}
		if (t == typeof(MouseCursor))
		{
			return prefabMouseCursor;
		}
		if (t == typeof(AssignLine))
		{
			return prefabAssignLine;
		}
		if (t == typeof(Billboard))
		{
			return prefabBillboard;
		}
		if (t == typeof(Hologram))
		{
			return prefabHologram;
		}
		Debug.Log("AssetLinks.GetUIPrefab: Don't know " + t);
		return null;
	}

	public TrailGate GetTrailGate(TrailType _type)
	{
		return _type switch
		{
			TrailType.GATE_SENSORS => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Sensors))).GetComponent<TrailGate_Sensors>(), 
			TrailType.GATE_COUNTER => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Counter))).GetComponent<TrailGate_Counter>(), 
			TrailType.GATE_LIFE => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Life))).GetComponent<TrailGate_Life>(), 
			TrailType.GATE_CARRY => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Carry))).GetComponent<TrailGate_Carry>(), 
			TrailType.GATE_CASTE => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Caste))).GetComponent<TrailGate_Caste>(), 
			TrailType.GATE_OLD => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Old))).GetComponent<TrailGate_Old>(), 
			TrailType.GATE_COUNTER_END => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_CounterEnd))).GetComponent<TrailGate_CounterEnd>(), 
			TrailType.GATE_SPEED => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Speed))).GetComponent<TrailGate_Speed>(), 
			TrailType.GATE_TIMER => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Timer))).GetComponent<TrailGate_Timer>(), 
			TrailType.GATE_STOCKPILE => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Stockpile))).GetComponent<TrailGate_Stockpile>(), 
			TrailType.GATE_LINK => UnityEngine.Object.Instantiate(standard.GetPrefab(typeof(TrailGate_Link))).GetComponent<TrailGate_Link>(), 
			_ => null, 
		};
	}

	public Material GetTrailMaterial(TrailStatus _status, bool lit_up = true, bool cursor = false)
	{
		return GetTrailMaterial(_status, TrailType.NONE, lit_up, cursor);
	}

	public Material GetTrailMaterial(TrailType _type, bool lit_up = true, bool cursor = false)
	{
		return GetTrailMaterial(TrailStatus.NONE, _type, lit_up, cursor);
	}

	public Material GetTrailMaterial(TrailStatus _status, TrailType _type, bool lit_up = true, bool cursor = false)
	{
		TrailTypeMaterial trailTypeMaterial = null;
		if (_status != TrailStatus.NONE)
		{
			trailTypeMaterial = GetTTM(_status);
		}
		if (_type != TrailType.NONE)
		{
			trailTypeMaterial = GetTTM(_type);
		}
		if (trailTypeMaterial != null)
		{
			if (cursor && trailTypeMaterial.mat_cursor != null)
			{
				return trailTypeMaterial.mat_cursor;
			}
			if (!lit_up && trailTypeMaterial.mat_low != null)
			{
				return trailTypeMaterial.mat_low;
			}
			if (trailTypeMaterial.mat != null)
			{
				return trailTypeMaterial.mat;
			}
		}
		Debug.LogError("No material found for trail status " + _status.ToString() + " and type " + _type);
		return trailTypeMaterials[0].mat;
	}

	public Sprite GetTrailIcon(TrailType _type, out Color col)
	{
		TrailTypeMaterial tTM = GetTTM(_type);
		if (tTM != null && tTM.icon != null)
		{
			col = tTM.iconCol;
			return tTM.icon;
		}
		Debug.LogError("No icon found for trail type " + _type);
		col = Color.white;
		return null;
	}

	private TrailTypeMaterial GetTTM(TrailType _type)
	{
		return GetTTM(TrailStatus.NONE, _type);
	}

	private TrailTypeMaterial GetTTM(TrailStatus _status, TrailType _type = TrailType.NONE)
	{
		if (_status != TrailStatus.NONE)
		{
			TrailTypeMaterial[] array = trailTypeMaterials;
			foreach (TrailTypeMaterial trailTypeMaterial in array)
			{
				if (trailTypeMaterial.status == _status)
				{
					return trailTypeMaterial;
				}
			}
		}
		if (_type != TrailType.NONE)
		{
			TrailTypeMaterial[] array = trailTypeMaterials;
			foreach (TrailTypeMaterial trailTypeMaterial2 in array)
			{
				if (trailTypeMaterial2.type == _type)
				{
					return trailTypeMaterial2;
				}
			}
		}
		if (_type != TrailType.NONE)
		{
			Debug.LogError("Can't find visuals for trail type " + _type);
		}
		else if (_status != TrailStatus.NONE)
		{
			Debug.LogError("Can't find visuals for trail status " + _status);
		}
		return null;
	}

	public Material GetPickupMaterial(PickupType _type)
	{
		PickupMaterial[] array = pickupMaterials;
		foreach (PickupMaterial pickupMaterial in array)
		{
			if (pickupMaterial.pickup == _type)
			{
				return pickupMaterial.mat;
			}
		}
		Debug.LogError("No material found for pickup type " + _type);
		return pickupMaterials[0].mat;
	}

	public Sprite GetBuildingThumbnail(string code)
	{
		BuildingData buildingData = BuildingData.Get(code);
		return Resources.Load<Sprite>("Building Icons/" + buildingData.prefab.name);
	}

	public Sprite GetPickupThumbnail(PickupType type)
	{
		if (type == PickupType.NONE)
		{
			return null;
		}
		PickupData pickupData = PickupData.Get(type);
		return Resources.Load<Sprite>("Pickup Icons/" + pickupData.prefab.name);
	}

	public Sprite GetAntCasteThumbnail(AntCaste caste)
	{
		if (caste == AntCaste.NONE)
		{
			return null;
		}
		AntCasteData antCasteData = AntCasteData.Get(caste);
		return Resources.Load<Sprite>("Ant Caste Icons/" + antCasteData.prefab.name);
	}

	public HighlightEffect GetHighlightEffect(HighlightStatus _status)
	{
		return highlightSetups.GetHighlightEffect(_status);
	}

	public GameObject GetStatusEffectParticleEffect(StatusEffect status_effect)
	{
		switch (status_effect)
		{
		case StatusEffect.NONE:
			return null;
		case StatusEffect.RADIATED_LIGHT:
		case StatusEffect.RADIATED_MEDIUM:
		case StatusEffect.RADIATED_HEAVY:
		case StatusEffect.ELECTROLYSED:
		case StatusEffect.ELDER_SPED:
		case StatusEffect.ELDER_SLOWED:
		case StatusEffect.OLD:
			return null;
		case StatusEffect.CHARGED:
			return statusEffectCharged;
		case StatusEffect.DISEASED:
			return statusEffectDiseased;
		case StatusEffect.HYPER:
			return statusEffectHyper;
		case StatusEffect.RADIATION:
			return statusEffectRadiation;
		default:
			Debug.LogWarning($"GetStatusEffectVisual: unknown '{status_effect}'");
			return null;
		}
	}

	public List<Material> GetAntMaterial(AntCaste _caste, StatusEffect status_effect)
	{
		AntStatusEffectMaterial antStatusEffectMaterial = null;
		AntStatusEffectMaterial[] array = antMaterials;
		foreach (AntStatusEffectMaterial antStatusEffectMaterial2 in array)
		{
			if (antStatusEffectMaterial2.statusEffect == status_effect)
			{
				antStatusEffectMaterial = antStatusEffectMaterial2;
			}
		}
		if (antStatusEffectMaterial == null)
		{
			antStatusEffectMaterial = antMaterials[0];
		}
		if (!dicdicAntStatusEffectMats.ContainsKey(_caste))
		{
			dicdicAntStatusEffectMats.Add(_caste, new Dictionary<StatusEffect, List<Material>>());
		}
		Dictionary<StatusEffect, List<Material>> dictionary = dicdicAntStatusEffectMats[_caste];
		if (!dictionary.ContainsKey(status_effect))
		{
			Material mat_target = new Material(AntCasteData.Get(_caste).prefab.GetComponent<Ant>().rends[0].sharedMaterials[0]);
			mat_target.name = antStatusEffectMaterial.matBody.name + "_" + _caste;
			CopyMaterialValues(ref antStatusEffectMaterial.matBody, ref mat_target);
			Material mat_target2 = new Material(AntCasteData.Get(_caste).prefab.GetComponent<Ant>().rends[0].sharedMaterials[1]);
			mat_target2.name = antStatusEffectMaterial.matEyes?.ToString() + "_" + _caste;
			CopyMaterialValues(ref antStatusEffectMaterial.matEyes, ref mat_target2);
			dictionary.Add(status_effect, new List<Material> { mat_target, mat_target2 });
		}
		return dictionary[status_effect];
	}

	private void CopyMaterialValues(ref Material mat_orig, ref Material mat_target)
	{
		if (mat_orig.HasFloat("_Saturation"))
		{
			mat_target.SetFloat("_Saturation", mat_orig.GetFloat("_Saturation"));
		}
		if (mat_orig.HasFloat("_Contrast"))
		{
			mat_target.SetFloat("_Contrast", mat_orig.GetFloat("_Contrast"));
		}
		if (mat_orig.HasFloat("_Fresnel_Power_Top"))
		{
			mat_target.SetFloat("_Fresnel_Power_Top", mat_orig.GetFloat("_Fresnel_Power_Top"));
		}
		if (mat_orig.HasFloat("_Fresnel_Power_Base"))
		{
			mat_target.SetFloat("_Fresnel_Power_Base", mat_orig.GetFloat("_Fresnel_Power_Base"));
		}
		if (mat_orig.HasColor("_Top_Color"))
		{
			mat_target.SetColor("_Top_Color", mat_orig.GetColor("_Top_Color"));
		}
		if (mat_orig.HasFloat("_Dissolve"))
		{
			mat_target.SetFloat("_Dissolve", mat_orig.GetFloat("_Dissolve"));
		}
		if (mat_orig.HasFloat("_EdgeWidth"))
		{
			mat_target.SetFloat("_EdgeWidth", mat_orig.GetFloat("_EdgeWidth"));
		}
		if (mat_orig.HasFloat("_NoiseScale"))
		{
			mat_target.SetFloat("_NoiseScale", mat_orig.GetFloat("_NoiseScale"));
		}
		if (mat_orig.HasFloat("_Eye_Blink_Speed"))
		{
			mat_target.SetFloat("_Eye_Blink_Speed", mat_orig.GetFloat("_Eye_Blink_Speed"));
		}
		if (mat_orig.HasFloat("_Eyes_Blink_Speed"))
		{
			mat_target.SetFloat("_Eyes_Blink_Speed", mat_orig.GetFloat("_Eyes_Blink_Speed"));
		}
	}

	public bool StatusEffectHasMaterial(StatusEffect _effect)
	{
		if (listStatusEffectWithMaterials.Count == 0)
		{
			listStatusEffectWithMaterials = new List<StatusEffect>();
			AntStatusEffectMaterial[] array = antMaterials;
			foreach (AntStatusEffectMaterial antStatusEffectMaterial in array)
			{
				listStatusEffectWithMaterials.Add(antStatusEffectMaterial.statusEffect);
			}
		}
		return listStatusEffectWithMaterials.Contains(_effect);
	}

	public GameObject GetExplosionPrefab(ExplosionType _type)
	{
		foreach (ExplosionAsset explosion in explosions)
		{
			if (explosion.type == _type)
			{
				return explosion.prefab;
			}
		}
		Debug.LogError("No prefab found for explosion type " + _type);
		return explosions[0].prefab;
	}
}
