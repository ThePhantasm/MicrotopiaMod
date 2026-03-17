using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Assets/Resources_moved/Biomes/Biome", menuName = "Microtopia/Biome", order = 0)]
public class Biome : ScriptableObject
{
	public enum Element
	{
		NONE = 0,
		NIRNROOT = 1,
		ROCK_SLIDE_MULTI = 2,
		ROCK_MOUNTAIN = 3,
		IRON_DEPOSIT = 4,
		IRON_DEPOSIT_SCRAPA = 5,
		IRON_DEPOSIT_TOXIC = 6,
		COPPER_DEPOSIT = 7,
		RUBBLE_DEPOSIT_BLUE = 8,
		RUBBLE_DEPOSIT_SCRAPA = 9,
		GLASS_DEPOSIT = 10,
		IRON_PILE = 11,
		COPPER_PILE = 12,
		GLASS_PILE = 13,
		WIRE_MOUNTAIN = 14,
		WIRE_MOUNTAIN2 = 15,
		JERRYCAN_MOUNTAIN = 16,
		HARDDISK_MOUNTAIN1 = 17,
		TRASHBAG_MOUNTAIN = 18,
		GROUND_CABLE = 19,
		CONCRETE_SCRAP_POLES = 20,
		TOXIC_WASTE = 21,
		TOXIC_BOTTOM_GROWER = 22,
		JUNGLE_DEPOSIT = 23,
		JUNGLEGRASS = 24,
		DESERTGRASS = 25,
		GLASS_DEPOSIT_TOXIC = 26,
		JUNGLE_GIANT_TREE_STATIC = 50,
		JUNGLE_CORRAL_TREE_STATIC = 51,
		JUNGLE_STRANGLE_TREE_STATIC = 52,
		IRON_RAW = 500,
		COPPER_RAW = 501
	}

	public BiomeType biomeType;

	[Tooltip("Note: index is used in savegame so please don't just change the list (adding is fine)")]
	public List<Ground> groundPrefabs;

	[Tooltip("If older ground prefabs get cut but need to be there for older saves")]
	public List<int> cutGrounds = new List<int>();

	public BiomeLighting lighting;

	public List<BiomeArea> areas;

	[Tooltip("How many plants should grow - atm this needs to be set higher for bigger ground surfaces to get the same density")]
	public float fertility = 200f;

	[Tooltip("The plants native to this biome")]
	public List<PlantType> plantTypes;

	public static Biome generatingBiome;

	public static Ground generatingGround;

	public const float squareSize = 8f;

	[NonSerialized]
	public List<BiomeObject> preplacedSpawnedBobs;

	public bool spawnUnlocker = true;

	public int unlockerTier = 1;

	private void Reset()
	{
		areas = new List<BiomeArea>();
		BiomeArea biomeArea = new BiomeArea();
		biomeArea.elements.Add(new BiomeElement(biomeArea));
		areas.Add(biomeArea);
	}

	public int PickGroundIndex()
	{
		List<Ground> list = new List<Ground>(groundPrefabs);
		foreach (int cutGround in cutGrounds)
		{
			list.Remove(groundPrefabs[cutGround]);
		}
		if (GameManager.instance != null)
		{
			List<Ground> list2 = new List<Ground>(list);
			for (int num = list2.Count - 1; num >= 0; num--)
			{
				string text = list2[num].name + "(Clone)";
				foreach (Ground item2 in GameManager.instance.EGrounds())
				{
					if (item2.name == text)
					{
						list2.RemoveAt(num);
						break;
					}
				}
			}
			if (list2.Count > 0)
			{
				list = list2;
			}
		}
		Ground item = list[UnityEngine.Random.Range(0, list.Count)];
		return groundPrefabs.IndexOf(item);
	}

	public IEnumerable<(Distribution, BiomeArea, BiomeElement)> EDistributions()
	{
		foreach (BiomeArea area in areas)
		{
			yield return (area.distribution, area, null);
			foreach (BiomeElement element in area.elements)
			{
				if (element.area == null)
				{
					element.area = area;
				}
				yield return (element.distribution, area, element);
			}
		}
	}

	public static BiomeType ParseBiomeType(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return BiomeType.NONE;
		}
		if (Enum.TryParse<BiomeType>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("Prefabs: BiomeType parse error; '" + str + "' invalid");
		return BiomeType.NONE;
	}

	public static BiomeElementType GetElementType(Element el)
	{
		if (el < Element.IRON_RAW)
		{
			return BiomeElementType.BIOMEOBJECT;
		}
		return BiomeElementType.PICKUP;
	}
}
