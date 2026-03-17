using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PrefabData
{
	public static List<BuildingData> buildings = new List<BuildingData>();

	public static List<TrailData> trails = new List<TrailData>();

	public static List<PickupData> pickups = new List<PickupData>();

	public static List<PickupCategoryData> pickupCategories = new List<PickupCategoryData>();

	public static List<AntCasteData> antCastes = new List<AntCasteData>();

	public static List<FactoryRecipeData> factoryRecipes = new List<FactoryRecipeData>();

	public static List<StatusEffectData> statusEffects = new List<StatusEffectData>();

	public static List<BiomeObjectData> biomeObjects = new List<BiomeObjectData>();

	public static List<PlantData> plants = new List<PlantData>();

	private static List<string> recyclerRecipes = new List<string>();

	private static List<string> deconstructorRecipes = new List<string>();

	private static List<string> cheatRecipes_pickups = new List<string>();

	private static List<string> cheatRecipes_ants = new List<string>();

	public static IEnumerator KInit(KoroutineBehaviour caller, bool for_test_bed = false)
	{
		KoroutineBehaviour.KoroutineId kid = caller.SetFinalizer();
		try
		{
			buildings.Clear();
			trails.Clear();
			pickups.Clear();
			pickupCategories.Clear();
			antCastes.Clear();
			factoryRecipes.Clear();
			biomeObjects.Clear();
			XmlDocument fods = SheetReader.GetXmlDoc(Files.FodsPrefabs());
			if (fods == null)
			{
				yield break;
			}
			yield return caller.StartCoroutine(CPreloadPrefabs(fods, "Pickups", "Type", "Prefab", "Pickups/"));
			foreach (SheetRow row in SheetReader.ERead(fods, "Pickups"))
			{
				string type_code = row.GetString("Type");
				if (SheetRow.Skip(type_code))
				{
					continue;
				}
				PickupData pickup_data = new PickupData
				{
					type = PickupData.ParsePickupType(type_code),
					order = row.GetInt("Order")
				};
				if (pickup_data.order == -1)
				{
					continue;
				}
				string text = row.GetString("Prefab");
				AsyncOperationHandle<GameObject> loading = Addressables.LoadAssetAsync<GameObject>("Pickups/" + text);
				yield return loading;
				pickup_data.prefab = loading.Result;
				if (pickup_data.prefab == null)
				{
					Debug.LogError("Pickups: " + type_code + " has unknown prefab '" + row.GetString("Prefab") + "'");
					continue;
				}
				pickup_data.title = row.GetString("Title");
				pickup_data.description = row.GetString("Description");
				pickup_data.categories = new List<PickupCategory>();
				string str = row.GetString("Categories");
				if (!SheetRow.Skip(str))
				{
					List<PickupCategory> list = new List<PickupCategory>();
					foreach (string item in str.EListItems())
					{
						if (Enum.TryParse<PickupCategory>(item.ToUpper(), out var result))
						{
							list.Add(result);
						}
						else
						{
							Debug.LogError("Don't know pickup category " + item + " on pickup " + type_code);
						}
					}
					if (list.Count == 0)
					{
						list.Add(PickupCategory.NONE);
					}
					pickup_data.categories = list;
				}
				pickup_data.energyAmount = row.GetFloat("energy", 0f);
				pickup_data.weight = row.GetFloat("weight", 0f);
				pickup_data.state = PickupData.ParsePickupState(row.GetString("state"));
				pickup_data.decay = row.GetFloat("decay");
				pickup_data.inDemo = row.GetBool("In demo");
				pickup_data.planned = row.GetBool("Planned");
				pickup_data.components = PickupCost.ParseList(row.GetString("Components"));
				pickup_data.statusEffects = StatusEffectData.ParseList(row.GetString("Status Effects"));
				pickups.Add(pickup_data);
			}
			pickups.Sort((PickupData t1, PickupData t2) => t1.order.CompareTo(t2.order));
			if (!for_test_bed)
			{
				yield return caller.StartCoroutine(CPreloadPrefabs(fods, "Ant Castes", "Caste", "Prefab", "Ant Castes/"));
				foreach (SheetRow row in SheetReader.ERead(fods, "Ant Castes"))
				{
					string type_code = row.GetString("Caste");
					if (SheetRow.Skip(type_code))
					{
						continue;
					}
					AntCasteData ac_data = new AntCasteData
					{
						caste = AntCasteData.ParseAntCaste(type_code)
					};
					string prefab_name = row.GetString("Prefab");
					AsyncOperationHandle<GameObject> loading = Addressables.LoadAssetAsync<GameObject>("Ant Castes/" + prefab_name);
					yield return loading;
					ac_data.prefab = loading.Result;
					if (ac_data.prefab == null)
					{
						Debug.LogError("Ant Castes: " + type_code + " has unknown prefab '" + row.GetString("Prefab") + "'");
						continue;
					}
					ac_data.title = row.GetString("Title");
					ac_data.description = row.GetString("Description");
					ac_data.exchangeTypes = TrailData.ParseListExchangeType(row.GetString("Exchange types"), "Ant caste " + ac_data.caste.ToString() + ": ");
					ac_data.canDo = row.GetString("Can do").EListItems().ToList();
					ac_data.speed = row.GetFloat("Speed");
					ac_data.strength = row.GetFloat("Strength");
					ac_data.energy = row.GetFloat("Energy");
					ac_data.energyExtra = row.GetFloat("Energy extra", 0f);
					ac_data.oldTime = row.GetFloat("Old time", 0f);
					ac_data.mineSpeed = row.GetFloat("Mine speed");
					ac_data.flying = row.GetBool("Flying");
					if (ac_data.flying)
					{
						loading = Addressables.LoadAssetAsync<GameObject>("Nuptial Flight/" + prefab_name);
						yield return loading;
						ac_data.prefab_nuptialFlight = loading.Result;
					}
					ac_data.flightSpeed = row.GetFloat("Flight Speed", 1f);
					ac_data.isGyne = row.GetBool("Gyne");
					ac_data.canBeCarried = row.GetBool("Can be carried");
					ac_data.mineForever = row.GetBool("Mine forever");
					ac_data.inDemo = row.GetBool("In demo");
					ac_data.order = row.GetInt("Order");
					ac_data.components = PickupCost.ParseList(row.GetString("Components"));
					ac_data.deathSpawn = AntCasteData.ParseAntCaste(row.GetString("Death Spawn"));
					string text2 = row.GetString("Corpse");
					if (string.IsNullOrEmpty(text2))
					{
						ac_data.corpse = PickupType.CORPSE_ANT;
					}
					else
					{
						ac_data.corpse = PickupData.ParsePickupType(text2);
					}
					ac_data.vulnerabilityBits = -1;
					string[] array = row.GetString("Immunities").Split(',');
					for (int num = 0; num < array.Length; num++)
					{
						StatusEffect statusEffect = StatusEffectData.ParseStatusEffect(array[num].Trim());
						if (statusEffect != StatusEffect.NONE)
						{
							ac_data.vulnerabilityBits &= ~(1 << (int)statusEffect);
						}
					}
					antCastes.Add(ac_data);
				}
				foreach (SheetRow item2 in SheetReader.ERead(fods, "Factory Recipes"))
				{
					string text3 = item2.GetString("ENUM");
					if (SheetRow.Skip(text3))
					{
						continue;
					}
					FactoryRecipeData factoryRecipeData = new FactoryRecipeData();
					factoryRecipeData.code = text3;
					factoryRecipeData.title = item2.GetString("Title");
					factoryRecipeData.costsPickup = PickupCost.ParseList(item2.GetString("Costs Pickup"));
					factoryRecipeData.costsAnt = AntCasteAmount.ParseList(item2.GetString("Cost Ant"));
					factoryRecipeData.productPickups = PickupCost.ParseList(item2.GetString("Product Pickup"));
					factoryRecipeData.productAnts = AntCasteAmount.ParseList(item2.GetString("Product Ant"));
					factoryRecipeData.energyCost = item2.GetFloat("Energy_Cost");
					factoryRecipeData.processTime = item2.GetFloat("Process_Time");
					factoryRecipeData.alwaysUnlocked = item2.GetBool("Always Unlocked");
					factoryRecipeData.inDemo = item2.GetBool("In demo");
					List<string> list2 = new List<string>();
					foreach (string item3 in item2.GetString("Buildings").EListItems())
					{
						list2.Add(item3);
					}
					factoryRecipeData.buildings = list2;
					factoryRecipes.Add(factoryRecipeData);
				}
				foreach (AntCasteData antCaste in antCastes)
				{
					if (antCaste.components == null || antCaste.components.Count == 0)
					{
						continue;
					}
					FactoryRecipeData factoryRecipeData2 = new FactoryRecipeData();
					factoryRecipeData2.code = "RECYCLE_" + antCaste.caste;
					factoryRecipeData2.title = factoryRecipeData2.code;
					factoryRecipeData2.costsPickup = new List<PickupCost>();
					factoryRecipeData2.costsAnt = new List<AntCasteAmount>();
					factoryRecipeData2.costsAnt.Add(new AntCasteAmount(antCaste.caste, 1));
					factoryRecipeData2.productPickups = new List<PickupCost>();
					int num2 = 0;
					foreach (PickupCost component in antCaste.components)
					{
						factoryRecipeData2.productPickups.Add(new PickupCost(component));
						num2 += component.intValue;
					}
					factoryRecipeData2.productAnts = new List<AntCasteAmount>();
					factoryRecipeData2.energyCost = 0f;
					factoryRecipeData2.processTime = 2f;
					factoryRecipeData2.alwaysUnlocked = true;
					factoryRecipeData2.inDemo = antCaste.inDemo;
					factoryRecipeData2.buildings = new List<string> { "CRUSHER" };
					factoryRecipes.Add(factoryRecipeData2);
					recyclerRecipes.Add(factoryRecipeData2.code);
				}
				foreach (PickupData pickup in pickups)
				{
					if (pickup.components == null || pickup.components.Count == 0)
					{
						continue;
					}
					FactoryRecipeData factoryRecipeData3 = new FactoryRecipeData();
					factoryRecipeData3.code = "DECONSTRUCT_" + pickup.type;
					factoryRecipeData3.title = factoryRecipeData3.code;
					factoryRecipeData3.costsPickup = new List<PickupCost>
					{
						new PickupCost(pickup.type, 1)
					};
					factoryRecipeData3.costsAnt = new List<AntCasteAmount>();
					factoryRecipeData3.productPickups = new List<PickupCost>();
					int num3 = 0;
					foreach (PickupCost component2 in pickup.components)
					{
						factoryRecipeData3.productPickups.Add(new PickupCost(component2));
						num3 += component2.intValue;
					}
					factoryRecipeData3.productAnts = new List<AntCasteAmount>();
					factoryRecipeData3.energyCost = 0f;
					factoryRecipeData3.processTime = 5f * (float)num3;
					factoryRecipeData3.alwaysUnlocked = true;
					factoryRecipeData3.inDemo = pickup.inDemo;
					factoryRecipeData3.buildings = new List<string> { "DECONSTRUCTOR" };
					factoryRecipes.Add(factoryRecipeData3);
					deconstructorRecipes.Add(factoryRecipeData3.code);
				}
				foreach (PickupData pickup2 in pickups)
				{
					if (pickup2.type != PickupType.NONE && !pickup2.planned)
					{
						FactoryRecipeData factoryRecipeData4 = new FactoryRecipeData();
						factoryRecipeData4.code = "CHEAT_PICKUP_" + pickup2.type;
						factoryRecipeData4.title = factoryRecipeData4.code;
						factoryRecipeData4.costsPickup = new List<PickupCost>();
						factoryRecipeData4.costsAnt = new List<AntCasteAmount>();
						factoryRecipeData4.productPickups = new List<PickupCost>
						{
							new PickupCost(pickup2.type.ToString() + " 1")
						};
						factoryRecipeData4.productAnts = new List<AntCasteAmount>();
						factoryRecipeData4.energyCost = 0f;
						factoryRecipeData4.processTime = 0.1f;
						factoryRecipeData4.alwaysUnlocked = true;
						factoryRecipeData4.inDemo = pickup2.inDemo;
						factoryRecipeData4.buildings = new List<string> { "CHEAT_PICKUPS_SPAWNER" };
						factoryRecipes.Add(factoryRecipeData4);
						cheatRecipes_pickups.Add(factoryRecipeData4.code);
					}
				}
				foreach (AntCasteData antCaste2 in antCastes)
				{
					if (antCaste2.caste != AntCaste.NONE && antCaste2.order >= 0)
					{
						FactoryRecipeData factoryRecipeData5 = new FactoryRecipeData();
						factoryRecipeData5.code = "CHEAT_ANT_" + antCaste2.caste;
						factoryRecipeData5.title = factoryRecipeData5.code;
						factoryRecipeData5.costsPickup = new List<PickupCost>();
						factoryRecipeData5.costsAnt = new List<AntCasteAmount>();
						factoryRecipeData5.productPickups = new List<PickupCost>();
						factoryRecipeData5.productAnts = new List<AntCasteAmount>
						{
							new AntCasteAmount(antCaste2.caste.ToString() + " 1")
						};
						factoryRecipeData5.energyCost = 0f;
						factoryRecipeData5.processTime = 3f;
						factoryRecipeData5.alwaysUnlocked = true;
						factoryRecipeData5.inDemo = antCaste2.inDemo;
						factoryRecipeData5.buildings = new List<string> { "CHEAT_ANT_SPAWNER" };
						factoryRecipes.Add(factoryRecipeData5);
						cheatRecipes_ants.Add(factoryRecipeData5.code);
					}
				}
				yield return caller.StartCoroutine(CPreloadPrefabs(fods, "Buildings", "Code", "Prefab", "Buildings/"));
				foreach (SheetRow row in SheetReader.ERead(fods, "Buildings"))
				{
					string prefab_name = row.GetString("Code");
					if (SheetRow.Skip(prefab_name))
					{
						continue;
					}
					BuildingData building_data = new BuildingData
					{
						code = prefab_name
					};
					string text4 = row.GetString("Prefab");
					AsyncOperationHandle<GameObject> loading = Addressables.LoadAssetAsync<GameObject>("Buildings/" + text4);
					yield return loading;
					building_data.prefab = loading.Result;
					if (building_data.prefab == null)
					{
						Debug.LogError("Parts: " + prefab_name + " has unknown prefab '" + row.GetString("Prefab") + "'");
						continue;
					}
					string text5 = row.GetString("Order");
					if (string.IsNullOrEmpty(text5) || text5.Contains("//"))
					{
						building_data.inBuildMenu = false;
						building_data.showOrder = int.MaxValue;
					}
					else
					{
						building_data.inBuildMenu = true;
						building_data.showOrder = row.GetInt("Order");
					}
					building_data.title = row.GetString("Title");
					building_data.titleParent = row.GetString("Title Parent");
					building_data.description = row.GetString("Description");
					building_data.group = BuildingData.ParseBuildingGroup(row.GetString("group"));
					building_data.parentBuilding = row.GetString("Parent Building");
					building_data.baseCosts = PickupCost.ParseList(row.GetString("Cost"));
					building_data.maxBuildCount = row.GetInt("N BUILDS");
					building_data.noDemolish = row.GetBool("NO DEMOLISH");
					building_data.autoRecipe = row.GetBool("Auto Recipe");
					building_data.pollution = row.GetFloat("Pollution", 0f);
					building_data.storageCapacity = row.GetInt("Storage Capacity", 0);
					building_data.tutorial = UITutorial.ParseTutorial(row.GetString("Tutorial"));
					building_data.inDemo = row.GetBool("In demo");
					List<string> list3 = new List<string>();
					foreach (FactoryRecipeData factoryRecipe in factoryRecipes)
					{
						if (factoryRecipe.buildings == null)
						{
							continue;
						}
						foreach (string building in factoryRecipe.buildings)
						{
							if (building == building_data.code)
							{
								list3.Add(factoryRecipe.code);
								break;
							}
						}
					}
					building_data.recipes = list3;
					buildings.Add(building_data);
				}
				foreach (SheetRow item4 in SheetReader.ERead(fods, "Trails"))
				{
					string str2 = item4.GetString("Type");
					if (SheetRow.Skip(str2))
					{
						continue;
					}
					TrailData trailData = new TrailData();
					trailData.type = TrailData.ParseTrailType(str2);
					if (string.IsNullOrEmpty(item4.GetString("Order")))
					{
						trailData.showOrder = int.MaxValue;
						trailData.inBuildMenu = false;
					}
					else
					{
						trailData.showOrder = item4.GetInt("Order");
						trailData.inBuildMenu = true;
					}
					trailData.title = item4.GetString("Title");
					trailData.description = item4.GetString("Description");
					trailData.exchangeTypes = TrailData.ParseListExchangeType(item4.GetString("Exchange types"), "Trail " + trailData.type.ToString() + ": ");
					trailData.parentType = TrailData.ParseTrailType(item4.GetString("Parent Trail"));
					trailData.logic = item4.GetBool("Logic");
					trailData.eraser = item4.GetBool("Eraser");
					trailData.snapToConnectable = !string.IsNullOrWhiteSpace(item4.GetString("Snap_to_connectable"));
					trailData.tutorial = UITutorial.ParseTutorial(item4.GetString("Tutorial"));
					trailData.inGame = item4.GetBool("In game");
					trailData.inDemo = item4.GetBool("In demo");
					trailData.elder = item4.GetBool("elder");
					trailData.shortcutKey = item4.GetString("Shortcut");
					List<int> list4 = new List<int>();
					foreach (string item5 in item4.GetString("Page").EListItems())
					{
						if (int.TryParse(item5, out var result2))
						{
							list4.Add(result2);
						}
					}
					trailData.trailPages = list4;
					trails.Add(trailData);
				}
				foreach (SheetRow item6 in SheetReader.ERead(fods, "Pickup Categories"))
				{
					string str3 = item6.GetString("Enum");
					if (!SheetRow.Skip(str3))
					{
						PickupCategoryData pickupCategoryData = new PickupCategoryData();
						pickupCategoryData.category = PickupCategoryData.ParsePickupCategory(str3);
						pickupCategoryData.order = item6.GetInt("Order");
						pickupCategoryData.title = item6.GetString("Title");
						pickupCategoryData.showInInventory = item6.GetBool("Inventory");
						pickupCategoryData.examplePickup = PickupData.ParsePickupType(item6.GetString("Example"));
						pickupCategories.Add(pickupCategoryData);
					}
				}
				foreach (SheetRow item7 in SheetReader.ERead(fods, "Status Effects"))
				{
					string str4 = item7.GetString("Effect");
					if (SheetRow.Skip(str4))
					{
						continue;
					}
					StatusEffectData statusEffectData = new StatusEffectData();
					statusEffectData.statusEffect = StatusEffectData.ParseStatusEffect(str4);
					float num4 = item7.GetFloat("Duration", 0f);
					if (num4 == 0f)
					{
						num4 = 0.1f;
					}
					if (num4 < 0f)
					{
						num4 = float.MaxValue;
					}
					statusEffectData.duration = num4;
					statusEffectData.effectSpeedFactor = item7.GetFloat("Speed", 1f);
					statusEffectData.effectDrainFactor = item7.GetFloat("Life Drain", 1f);
					statusEffectData.effectRadiation = item7.GetFloat("Radiation", 0f);
					statusEffectData.effectRadDeathFactor = item7.GetFloat("Rad Death", 0f);
					statusEffectData.effectDeath = item7.GetFloat("Death", 0f);
					string str5 = item7.GetString("Death Explosion");
					if (!SheetRow.Skip(str5))
					{
						statusEffectData.effectDeathExplosion = StatusEffectData.ParseExplosionType(str5);
					}
					statusEffectData.effectBlockActionPoints = item7.GetBool("Block AP");
					statusEffectData.effectIsTrigger = item7.GetBool("Trigger");
					int num5 = 0;
					foreach (StatusEffect item8 in StatusEffectData.EParseListStatusEffect(item7.GetString("Immunities")))
					{
						if (item8 != StatusEffect.NONE)
						{
							num5 |= 1 << (int)item8;
						}
					}
					statusEffectData.effectImmunitiesBits = num5;
					string str6 = item7.GetString("Effect Area");
					if (!SheetRow.Skip(str6))
					{
						statusEffectData.effectEffectArea = StatusEffectData.ParseStatusEffect(str6);
					}
					else
					{
						statusEffectData.effectEffectArea = StatusEffect.NONE;
					}
					statusEffectData.effectAreaRadius = item7.GetFloat("Area Radius", 0f);
					statusEffects.Add(statusEffectData);
				}
			}
			fods = SheetReader.GetXmlDoc(Files.FodsBiome());
			if (fods == null)
			{
				yield break;
			}
			yield return caller.StartCoroutine(CPreloadPrefabs(fods, "Biome Objects", "Code", "Prefab", "Biome Objects/"));
			foreach (SheetRow row in SheetReader.ERead(fods, "Biome Objects"))
			{
				string prefab_name = row.GetString("Code");
				if (!SheetRow.Skip(prefab_name))
				{
					BiomeObjectData bob_data = new BiomeObjectData
					{
						code = prefab_name
					};
					string text6 = row.GetString("Prefab");
					AsyncOperationHandle<GameObject> loading = Addressables.LoadAssetAsync<GameObject>("Biome Objects/" + text6);
					yield return loading;
					bob_data.prefab = loading.Result;
					if (bob_data.prefab == null)
					{
						Debug.LogError("Biome Objects: " + prefab_name + " has unknown prefab '" + row.GetString("Prefab") + "'");
					}
					else
					{
						bob_data.title = row.GetString("Title");
						bob_data.description = row.GetString("Description");
						bob_data.exchangeTypes = TrailData.ParseListExchangeType(row.GetString("Exchange types"), "Biome object " + bob_data.code + ": ");
						bob_data.pickups = PickupCost.ParseList(row.GetString("Pickups"));
						bob_data.infinite = row.GetBool("Infinite");
						bob_data.fruit = PickupData.ParsePickupType(row.GetString("Fruit"));
						bob_data.unclickable = row.GetBool("Unclickable");
						bob_data.trailsPassThrough = row.GetBool("Trails pass through");
						bob_data.hardness = row.GetFloat("Hardness", 1f);
						bob_data.pollution = row.GetFloat("Pollution", 0f);
						biomeObjects.Add(bob_data);
					}
				}
			}
			foreach (SheetRow item9 in SheetReader.ERead(fods, "Plants"))
			{
				string text7 = item9.GetString("Code").Trim();
				if (!SheetRow.Skip(text7))
				{
					if (!Enum.TryParse<PlantType>(text7, out var result3))
					{
						Debug.LogWarning("Biome.Plants: code '" + text7 + "' invalid");
						continue;
					}
					PlantData plantData = new PlantData();
					plantData.type = result3;
					plantData.mass = item9.GetFloat("Mass", 1f);
					plantData.dominance = item9.GetFloat("Dominance", 1f);
					plantData.spreadDelay = item9.GetFloat("Spread_delay", 10f);
					plantData.wiltDelay = item9.GetFloat("Wilt_delay", 10f);
					plantData.growTime = item9.GetFloat("Grow_time", 10f);
					plantData.wiltTime = item9.GetFloat("Wilt_time", 10f);
					plantData.clustering = item9.GetFloat("Clustering", 80f) / 100f;
					plantData.evenClustering = item9.GetBool("Even_cluster");
					plantData.distMin = item9.GetFloat("Dist_min");
					plantData.distMax = item9.GetFloat("Dist_max");
					plantData.pollutionRange.min = item9.GetFloat("Pollution_min", 0f) / 100f;
					plantData.pollutionRange.max = item9.GetFloat("Pollution_max", 30f) / 100f;
					plantData.pollutionTolerance = item9.GetFloat("Pollution_tolerance", 30f) / 100f;
					plantData.scaleRange.min = item9.GetFloat("Scale_min", 0.8f);
					plantData.scaleRange.max = item9.GetFloat("Scale_max", 1.2f);
					plantData.ignoreGrooves = item9.GetBool("Ignore Grooves");
					plants.Add(plantData);
				}
			}
		}
		finally
		{
			caller.StopKoroutine(kid);
		}
	}

	private static IEnumerator CPreloadPrefabs(XmlDocument fods, string sheet, string required_col, string prefab_col, string prefix)
	{
		List<string> list = new List<string>();
		foreach (SheetRow item in SheetReader.ERead(fods, sheet))
		{
			if (!SheetRow.Skip(item.GetString(required_col)))
			{
				list.Add(prefix + item.GetString(prefab_col));
			}
		}
		yield return Addressables.LoadAssetsAsync<GameObject>(list, delegate
		{
		}, Addressables.MergeMode.Union);
	}

	public static Sprite GetBiomeIcon(BiomeType biome_type)
	{
		return biome_type switch
		{
			BiomeType.BLUE => Resources.Load<Sprite>("Biome Icons/GroundBlueBiome1"), 
			BiomeType.DESERT => Resources.Load<Sprite>("Biome Icons/GroundScrapara"), 
			BiomeType.JUNGLE => Resources.Load<Sprite>("Biome Icons/GroundStaticJungle"), 
			BiomeType.TOXIC => Resources.Load<Sprite>("Biome Icons/GroundToxic"), 
			BiomeType.CONCRETE => Resources.Load<Sprite>("Biome Icons/GroundConcrete 1"), 
			_ => Resources.Load<Sprite>("Biome Icons/" + biome_type), 
		};
	}
}
