using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "GlobalValues", menuName = "Microtopia/GlobalValues", order = 1)]
public class GlobalValues : ScriptableObject
{
	public static GlobalValues standard;

	[Header("Values")]
	public float flightHeight = 50f;

	[Header("Values")]
	public float flightWindUpDownDistance = 5f;

	[Header("Values")]
	public float flightRandomRadius = 10f;

	public float baseMineDuration = 1f;

	public float gravity = -9.807f;

	public float trailHeight;

	public float corpseRotTime = 1200f;

	public float baseDeathJumpDuration = 0.3f;

	public float radDeathTime = 180f;

	public int nAutosaveSlots = 3;

	public float electrolyseDecay = 1f;

	public float antExpensiveUpdatePerSecond = 6f;

	[Tooltip("Avoid rare spikes (or idiotically low framerate) to cause havoc by capping deltatime")]
	public float maxDeltaTime = 0.2f;

	public int maxBuildMenuItemCount = 10;

	[Header("Nuptial Flight")]
	public List<NuptialFlightLevel> nuptialFlightLevels = new List<NuptialFlightLevel>();

	public Vector2 nuptialFlightHeightRange = new Vector2(55f, 70f);

	public Vector2Int nuptialFlightCountRange = new Vector2Int(200, 1000);

	public float nuptialFlightWarmUp = 2f;

	public float nuptialFlightDuration = 60f;

	public float nuptialFlightFlyOff = 180f;

	public float nuptialFlightSeasonLength = 3600f;

	[Header("Curves")]
	public AnimationCurve curveParabola;

	public AnimationCurve curveEaseIn;

	public AnimationCurve curveEaseOut;

	public AnimationCurve curveEaseInHeavy;

	public AnimationCurve curveEaseOutHeavy;

	public AnimationCurve curveEaseOutFinal;

	public AnimationCurve curveSIn;

	public AnimationCurve curveSOut;

	[Header("Hunger")]
	public float hungerEnergyStart;

	public List<HungerTier> hungerTiers;

	[Tooltip("Each +<per> energy above the normal tiers adds an overcharge tier")]
	public float overchargeTierPer;

	[Tooltip("Larvae spawn rate when overcharged")]
	public int overchargeLarvaeSpawnRate;

	[Tooltip("Max population increase per overcharge tier")]
	public int overchargeMaxPopulationGain;

	[Tooltip("Energy drain per second per overcharge tier")]
	public float overchargeDrainGain;

	[Header("Lighting")]
	public BiomeLighting spaceLighting;

	public BiomeLighting mapLighting;

	[Header("Biomes")]
	public List<DistributionTemplate> distributionTemplates = new List<DistributionTemplate>();

	[Header("Other")]
	public string steamPageLink;

	public string discordLink;

	public string googleFormLink;

	public static IEnumerator CInit()
	{
		AsyncOperationHandle<GlobalValues> loading = Addressables.LoadAssetAsync<GlobalValues>("ScriptableObjects/GlobalValues");
		yield return loading;
		standard = loading.Result;
	}

	public static GlobalValues GetStandardEditor()
	{
		if (standard == null)
		{
			standard = Addressables.LoadAssetAsync<GlobalValues>("ScriptableObjects/GlobalValues").WaitForCompletion();
		}
		return standard;
	}
}
