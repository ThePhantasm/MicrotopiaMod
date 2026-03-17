using System;
using System.Collections.Generic;
using UnityEngine;

public class UIBuildingMenu : UIBase
{
	[Serializable]
	public class GroupButton
	{
		public BuildingGroup group;

		public UITextImageButton button;

		public RectTransform showOnHover;
	}

	[Space(10f)]
	[SerializeField]
	private UIBuildingButton prefabBuildingButton;

	private List<UIBuildingButton> spawnedBuildingButtons = new List<UIBuildingButton>();

	private UIBuildingButtonHover buttonHover;

	private RectTransform followingRT;

	private TrailData hoveringTrail;

	private BuildingData hoveringBuilding;

	private Blueprint hoveringBlueprint;

	private string hoveringTitle_locUI;

	public static BuildingGroup currentBuildGroup;

	[SerializeField]
	private UIToolbarExtra uiToolbarExtra;

	private TrailType lastGateSelected = TrailType.GATE_CARRY;

	private TrailType lastCounterSelected = TrailType.GATE_COUNTER;

	[Space(10f)]
	[SerializeField]
	private List<GroupButton> groupButtons = new List<GroupButton>();

	public void Init(UIBuildingButtonHover hover)
	{
		buttonHover = hover;
		buttonHover.Show(target: false);
		uiToolbarExtra.Init(this);
	}

	public void UIUpdate()
	{
		uiToolbarExtra.UIUpdate();
		if (followingRT != null)
		{
			Vector2 vector = buttonHover.transform.position;
			vector.x = followingRT.transform.position.x;
			buttonHover.transform.position = vector;
		}
		if (hoveringTrail != null)
		{
			buttonHover.SetHover(hoveringTrail.GetTitle(), hoveringTrail.GetDescription());
		}
		if (hoveringBuilding != null)
		{
			buttonHover.SetHover(hoveringBuilding.GetTitleParent(), hoveringBuilding.GetDescription());
			Dictionary<PickupType, int> dictionary = hoveringBuilding.baseCosts.ToDictionary();
			if (dictionary.Count > 0)
			{
				buttonHover.SetInventory();
			}
			if (new List<string> { "BRIDGE_SMALL", "BRIDGE_MEDIUM", "BRIDGE_LARGE" }.Contains(hoveringBuilding.code))
			{
				buttonHover.UpdateInventory(Loc.GetUI("BUILDING_REQUIRES_SECTION"), dictionary);
			}
			else
			{
				buttonHover.UpdateInventory(Loc.GetUI("BUILDING_REQUIRES"), dictionary);
			}
		}
		if (hoveringBlueprint != null)
		{
			buttonHover.SetHover(hoveringBlueprint.name, "");
		}
		if (hoveringTitle_locUI != null)
		{
			buttonHover.SetHover(Loc.GetUI(hoveringTitle_locUI), "");
		}
		if (InputManager.buildGroupSelect != 0)
		{
			int buildGroupSelect = InputManager.buildGroupSelect;
			int num = ((buildGroupSelect > 0) ? (-1) : groupButtons.Count);
			for (int i = 0; i < groupButtons.Count; i++)
			{
				if (groupButtons[i].group == currentBuildGroup)
				{
					num = i;
				}
			}
			for (int j = 0; j < groupButtons.Count; j++)
			{
				num += buildGroupSelect;
				if (num >= groupButtons.Count)
				{
					num = 0;
				}
				if (num < 0)
				{
					num = groupButtons.Count - 1;
				}
				GroupButton groupButton = groupButtons[num];
				if (groupButton.button.gameObject.activeInHierarchy)
				{
					Gameplay.instance.SetTaskbar(groupButton.group);
					break;
				}
			}
		}
		else if (InputManager.trailTypeQuickSelect != TrailType.NONE)
		{
			TrailType trailTypeQuickSelect = InputManager.trailTypeQuickSelect;
			if ((trailTypeQuickSelect != TrailType.FLOOR_SELECTOR) ? Progress.IsUnlocked(trailTypeQuickSelect) : Progress.HasUnlocked(GeneralUnlocks.BLUEPRINTS))
			{
				OnClickTrailButton(trailTypeQuickSelect);
			}
		}
	}

	public void SetupButtons(BuildingGroup _group, TrailType selected_trail = TrailType.NONE, string selected_building = "")
	{
		List<BuildingGroup> unlockedBuildingGroups = Progress.GetUnlockedBuildingGroups();
		List<TrailData> unlockedTrailsInBuildMenu = Progress.GetUnlockedTrailsInBuildMenu();
		List<TrailData> list = new List<TrailData>();
		List<TrailData> list2 = new List<TrailData>();
		foreach (TrailData trail in PrefabData.trails)
		{
			if (trail.parentType == TrailType.COUNTER_PARENT && trail.type != TrailType.COUNTER_PARENT && Progress.HasUnlocked(trail.type))
			{
				list.Add(trail);
			}
			if (trail.parentType == TrailType.GATE && trail.type != TrailType.GATE && Progress.HasUnlocked(trail.type))
			{
				list2.Add(trail);
			}
		}
		List<BuildingData> unlockedBuildings = Progress.GetUnlockedBuildings();
		BuildingData buildingData = null;
		if (selected_building != "")
		{
			buildingData = BuildingData.Get(selected_building);
		}
		TrailData trailData = TrailData.Get(selected_trail);
		currentBuildGroup = _group;
		foreach (GroupButton groupButton in groupButtons)
		{
			groupButton.button.SetObActive(active: false);
		}
		if (unlockedBuildingGroups.Count > 1)
		{
			foreach (GroupButton bt in groupButtons)
			{
				UITextImageButton button = bt.button;
				BuildingGroup bg = bt.group;
				if (!unlockedBuildingGroups.Contains(bt.group))
				{
					continue;
				}
				button.Init("", delegate
				{
					Gameplay.instance.SetActivity(Activity.NONE);
					Gameplay.instance.SetTaskbar(bg);
				});
				button.SetImageEnabled(bg == _group);
				button.ResetOverlays();
				button.SetOnPointerEnter(delegate
				{
					bt.showOnHover.SetObActive(active: true);
				});
				button.SetOnPointerExit(delegate
				{
					bt.showOnHover.SetObActive(active: false);
				});
				bt.showOnHover.SetObActive(active: false);
				if (bg != _group)
				{
					if (bg == BuildingGroup.TRAILS)
					{
						foreach (TrailData item in unlockedTrailsInBuildMenu)
						{
							if (item.type == TrailType.COUNTER_PARENT)
							{
								bool flag = false;
								foreach (TrailData item2 in list)
								{
									if (Progress.HasNotUsedTrail(item2.type))
									{
										button.AddOverlay(OverlayTypes.NEW);
										flag = true;
										break;
									}
								}
								if (flag)
								{
									break;
								}
							}
							else if (item.type == TrailType.GATE)
							{
								bool flag2 = false;
								foreach (TrailData item3 in list2)
								{
									if (Progress.HasNotUsedTrail(item3.type))
									{
										button.AddOverlay(OverlayTypes.NEW);
										flag2 = true;
										break;
									}
								}
								if (flag2)
								{
									break;
								}
							}
							else if (Progress.HasNotUsedTrail(item.type))
							{
								button.AddOverlay(OverlayTypes.NEW);
								break;
							}
						}
					}
					else
					{
						foreach (BuildingData item4 in unlockedBuildings)
						{
							if (item4.group == bg && Progress.HasNotUsedBuilding(item4.code))
							{
								button.AddOverlay(OverlayTypes.NEW);
								break;
							}
						}
					}
				}
				button.SetObActive(active: true);
			}
		}
		prefabBuildingButton.SetObActive(active: false);
		foreach (UIBuildingButton spawnedBuildingButton in spawnedBuildingButtons)
		{
			spawnedBuildingButton.SetObActive(active: false);
		}
		Transform current_transform = null;
		switch (_group)
		{
		case BuildingGroup.TRAILS:
		case BuildingGroup.LOGIC:
		{
			for (int num4 = 0; num4 < unlockedTrailsInBuildMenu.Count; num4++)
			{
				TrailData data4 = unlockedTrailsInBuildMenu[num4];
				if (unlockedBuildingGroups.Contains(BuildingGroup.LOGIC) && ((_group == BuildingGroup.TRAILS && !data4.trailPages.Contains(0)) || (_group == BuildingGroup.LOGIC && !data4.trailPages.Contains(1))))
				{
					continue;
				}
				if (data4.type == TrailType.COUNTER_PARENT && list.Count == 1)
				{
					data4 = list[0];
				}
				if (data4.type == TrailType.GATE && list2.Count == 1)
				{
					data4 = list2[0];
				}
				Color col3;
				Sprite trailIcon3 = AssetLinks.standard.GetTrailIcon(data4.type, out col3);
				UIBuildingButton uIBuildingButton4 = SetButton(num4, trailIcon3, col3, InputManager.GetHotkey(data4.type), data4, null, null, delegate
				{
					ShowTutorial(data4.tutorial, delegate
					{
						OnClickTrailButton(data4.type);
					}, out var tut_active);
					if (!tut_active)
					{
						OnClickTrailButton(data4.type);
						SetHoveringBuildingButton(target: false);
					}
				});
				if (data4.type == selected_trail || data4.type == trailData.parentType)
				{
					uIBuildingButton4.AddOverlay(OverlayTypes.SELECTED);
					current_transform = uIBuildingButton4.transform;
				}
				if (data4.type == TrailType.COUNTER_PARENT)
				{
					foreach (TrailData item5 in list)
					{
						if (Progress.HasNotUsedTrail(item5.type))
						{
							uIBuildingButton4.AddOverlay(OverlayTypes.NEW);
							break;
						}
					}
				}
				else if (data4.type == TrailType.GATE)
				{
					foreach (TrailData item6 in list2)
					{
						if (Progress.HasNotUsedTrail(item6.type))
						{
							uIBuildingButton4.AddOverlay(OverlayTypes.NEW);
							break;
						}
					}
				}
				else if (Progress.HasNotUsedTrail(data4.type))
				{
					uIBuildingButton4.AddOverlay(OverlayTypes.NEW);
				}
			}
			break;
		}
		case BuildingGroup.BLUEPRINTS:
		{
			int num3 = 0;
			UIBuildingButton uIBuildingButton3 = SetButton(num3, AssetLinks.standard.spriteButtonBlueprints, Color.white, "", null, null, null, delegate
			{
				UIBlueprints uIBlueprints = UIBaseSingleton.Get(UIBlueprints.instance);
				uIBlueprints.Init();
				uIBlueprints.transform.SetAsLastSibling();
			}, "BLUEPRINTS_MENU");
			num3++;
			foreach (Blueprint blueprint in BlueprintManager.EBlueprintsInBar())
			{
				blueprint.UpdateLocked();
				bool locked = blueprint.locked;
				uIBuildingButton3 = SetButton(num3, blueprint.iconSprite, Color.white, "", null, null, blueprint, delegate
				{
					if (!locked)
					{
						Gameplay.instance.SelectBlueprint(blueprint);
					}
				});
				if (locked)
				{
					uIBuildingButton3.AddOverlay(OverlayTypes.LOCKED);
				}
				uIBuildingButton3.SetInteractable(!locked);
				num3++;
			}
			TrailData data3 = TrailData.Get(TrailType.FLOOR_SELECTOR);
			Color col2;
			Sprite trailIcon2 = AssetLinks.standard.GetTrailIcon(data3.type, out col2);
			uIBuildingButton3 = SetButton(num3, trailIcon2, col2, InputManager.GetHotkey(data3.type), data3, null, null, delegate
			{
				ShowTutorial(data3.tutorial, delegate
				{
					OnClickTrailButton(data3.type);
				}, out var tut_active);
				if (!tut_active)
				{
					OnClickTrailButton(data3.type);
					SetHoveringBuildingButton(target: false);
				}
			});
			if (data3.type == selected_trail)
			{
				uIBuildingButton3.AddOverlay(OverlayTypes.SELECTED);
			}
			num3++;
			break;
		}
		default:
		{
			int num = 0;
			for (int num2 = 0; num2 < unlockedBuildings.Count; num2++)
			{
				BuildingData data = unlockedBuildings[num2];
				if (data.group != _group)
				{
					continue;
				}
				UIBuildingButton uIBuildingButton = SetButton(num, AssetLinks.standard.GetBuildingThumbnail(data.code), Color.white, "", null, data, null, delegate
				{
					ShowTutorial(data.tutorial, delegate
					{
						OnClickBuildingButton(data.code);
					}, out var tut_active);
					if (!tut_active)
					{
						OnClickBuildingButton(data.code);
						SetHoveringBuildingButton(target: false);
					}
				});
				num++;
				if (data.code == selected_building || (buildingData != null && data.code == buildingData.parentBuilding))
				{
					uIBuildingButton.AddOverlay(OverlayTypes.SELECTED);
					current_transform = uIBuildingButton.transform;
				}
				if (Progress.HasNotUsedBuilding(data.code))
				{
					uIBuildingButton.AddOverlay(OverlayTypes.NEW);
				}
			}
			if (_group != BuildingGroup.FOUNDATION)
			{
				break;
			}
			TrailData data2 = TrailData.Get(TrailType.FLOOR_DEMOLISH);
			Color col;
			Sprite trailIcon = AssetLinks.standard.GetTrailIcon(data2.type, out col);
			UIBuildingButton uIBuildingButton2 = SetButton(num, trailIcon, col, InputManager.GetHotkey(data2.type), data2, null, null, delegate
			{
				ShowTutorial(data2.tutorial, delegate
				{
					OnClickTrailButton(data2.type);
				}, out var tut_active);
				if (!tut_active)
				{
					OnClickTrailButton(data2.type);
					SetHoveringBuildingButton(target: false);
				}
			});
			if (data2.type == selected_trail)
			{
				uIBuildingButton2.AddOverlay(OverlayTypes.SELECTED);
			}
			break;
		}
		}
		if (selected_trail != TrailType.NONE)
		{
			uiToolbarExtra.Setup(selected_trail, current_transform);
		}
		else if (selected_building != "")
		{
			uiToolbarExtra.Setup(selected_building, current_transform);
		}
		else
		{
			uiToolbarExtra.Setup();
		}
	}

	private UIBuildingButton SetButton(int index, Sprite sprite, Color color, string hotkey, TrailData trail_data, BuildingData building_data, Blueprint blueprint, Action action, string text = null)
	{
		UIBuildingButton bt;
		if (index >= spawnedBuildingButtons.Count)
		{
			bt = UnityEngine.Object.Instantiate(prefabBuildingButton, prefabBuildingButton.transform.parent);
			spawnedBuildingButtons.Add(bt);
		}
		else
		{
			bt = spawnedBuildingButtons[index];
		}
		bt.SetObActive(active: true);
		bt.Init(action);
		bt.SetImage(sprite);
		bt.SetImageColor(color);
		bt.ResetOverlays();
		bt.SetInteractable(target: true);
		bt.SetOnPointerEnter(delegate
		{
			SetHoveringBuildingButton(target: true, bt.rtBase, trail_data, building_data, blueprint, text);
		});
		bt.SetOnPointerExit(delegate
		{
			SetHoveringBuildingButton(target: false);
		});
		bt.SetHotkey(hotkey);
		return bt;
	}

	public void ShowTutorial(Tutorial _tut, Action on_close, out bool tut_active)
	{
		if (UIGame.instance.SetTutorial(_tut, on_close))
		{
			tut_active = true;
		}
		else
		{
			tut_active = false;
		}
	}

	public void OnClickTrailButton(TrailType tt)
	{
		TrailType trailType = tt;
		switch (tt)
		{
		case TrailType.COUNTER_PARENT:
			trailType = lastCounterSelected;
			break;
		case TrailType.GATE:
			trailType = lastGateSelected;
			break;
		}
		Progress.UseTrail(trailType);
		Gameplay.instance.SetTrailType(trailType);
		TrailData trailData = TrailData.Get(trailType);
		if (trailData.parentType == TrailType.COUNTER_PARENT)
		{
			lastCounterSelected = trailType;
		}
		else if (trailData.parentType == TrailType.GATE)
		{
			lastGateSelected = trailType;
		}
	}

	public void OnClickBuildingButton(string building_code)
	{
		BuildingData buildingData = BuildingData.Get(building_code);
		Gameplay.instance.SetTaskbar(buildingData.group);
		Gameplay.instance.StartBuilding(building_code);
	}

	public void SetHoveringBuildingButton(bool target, RectTransform rt = null, TrailData trail_data = null, BuildingData building_data = null, Blueprint blueprint = null, string locUI = null)
	{
		followingRT = rt;
		hoveringTrail = trail_data;
		hoveringBuilding = building_data;
		hoveringBlueprint = blueprint;
		hoveringTitle_locUI = locUI;
		buttonHover.SetObActive(target);
	}
}
