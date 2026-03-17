using System.Collections.Generic;
using UnityEngine;

public class UIToolbarExtra : MonoBehaviour
{
	[SerializeField]
	private RectTransform rtButtons;

	[SerializeField]
	private RectTransform rtBackground;

	[SerializeField]
	private UIBuildingButtonHover buttonHover;

	private RectTransform followingRT;

	private TrailData hoveringTrail;

	private BuildingData hoveringBuilding;

	private UIBuildingMenu uiToolbar;

	public UIBuildingButton prefabButton;

	private List<UIBuildingButton> spawnedButtons = new List<UIBuildingButton>();

	public void Init(UIBuildingMenu ui_toolbar)
	{
		uiToolbar = ui_toolbar;
		Setup();
		SetHoveringButton(target: false);
	}

	public void Setup()
	{
		rtBackground.SetObActive(active: false);
		prefabButton.SetObActive(active: false);
		foreach (UIBuildingButton spawnedButton in spawnedButtons)
		{
			spawnedButton.SetObActive(active: false);
		}
	}

	public void SetPosition(Transform current_transform)
	{
		if (current_transform != null)
		{
			Vector2 vector = rtButtons.transform.position;
			vector.x = current_transform.position.x;
			rtButtons.transform.position = vector;
		}
	}

	public void Setup(TrailType selected_trail, Transform current_transform)
	{
		Setup();
		if (selected_trail == TrailType.NONE)
		{
			return;
		}
		TrailType parentType = TrailData.Get(selected_trail).parentType;
		if (parentType == TrailType.NONE)
		{
			return;
		}
		List<TrailData> list = new List<TrailData>();
		foreach (TrailData trail in PrefabData.trails)
		{
			if (trail.parentType == parentType && Progress.HasUnlocked(trail.type) && trail.type != TrailType.GATE && trail.type != TrailType.COUNTER_PARENT)
			{
				list.Add(trail);
			}
		}
		if (list.Count < 2)
		{
			return;
		}
		if (spawnedButtons.Count < list.Count)
		{
			int num = list.Count - spawnedButtons.Count;
			for (int i = 0; i < num; i++)
			{
				UIBuildingButton component = Object.Instantiate(prefabButton.gameObject, prefabButton.transform.parent).GetComponent<UIBuildingButton>();
				spawnedButtons.Add(component);
			}
		}
		rtBackground.SetObActive(active: true);
		for (int j = 0; j < list.Count; j++)
		{
			TrailData data = list[j];
			UIBuildingButton bt = spawnedButtons[j];
			Color col;
			Sprite trailIcon = AssetLinks.standard.GetTrailIcon(data.type, out col);
			bt.Init(data.GetTitle(), delegate
			{
				uiToolbar.ShowTutorial(data.tutorial, delegate
				{
					uiToolbar.OnClickTrailButton(data.type);
				}, out var tut_active);
				if (!tut_active)
				{
					uiToolbar.OnClickTrailButton(data.type);
				}
				SetHoveringButton(target: false);
			});
			bt.SetImage(trailIcon);
			bt.SetImageColor(col);
			bt.ResetOverlays();
			if (data.type == selected_trail)
			{
				bt.AddOverlay(OverlayTypes.SELECTED);
			}
			if (Progress.HasNotUsedTrail(data.type))
			{
				bt.AddOverlay(OverlayTypes.NEW);
			}
			bt.SetInteractable(target: true);
			bt.SetOnPointerEnter(delegate
			{
				SetHoveringButton_Trail(target: true, bt.rtBase, data);
			});
			bt.SetOnPointerExit(delegate
			{
				SetHoveringButton(target: false);
			});
			bt.SetObActive(active: true);
			bt.SetHotkey(InputManager.GetHotkey(data.type));
		}
		SetPosition(current_transform);
	}

	public void Setup(string selected_building, Transform current_transform)
	{
		Setup();
		if (selected_building == "")
		{
			return;
		}
		string parentBuilding = BuildingData.Get(selected_building).parentBuilding;
		if (parentBuilding == "")
		{
			return;
		}
		List<BuildingData> list = new List<BuildingData>();
		foreach (BuildingData building in PrefabData.buildings)
		{
			if (building.parentBuilding == parentBuilding && Progress.HasUnlockedBuilding(building.code))
			{
				list.Add(building);
			}
		}
		if (list.Count < 2)
		{
			return;
		}
		if (spawnedButtons.Count < list.Count)
		{
			int num = list.Count - spawnedButtons.Count;
			for (int i = 0; i < num; i++)
			{
				UIBuildingButton component = Object.Instantiate(prefabButton.gameObject, prefabButton.transform.parent).GetComponent<UIBuildingButton>();
				spawnedButtons.Add(component);
			}
		}
		rtBackground.SetObActive(active: true);
		for (int j = 0; j < list.Count; j++)
		{
			BuildingData data = list[j];
			UIBuildingButton bt = spawnedButtons[j];
			Sprite buildingThumbnail = AssetLinks.standard.GetBuildingThumbnail(data.code);
			bt.Init(data.GetTitle(), delegate
			{
				uiToolbar.OnClickBuildingButton(data.code);
				SetHoveringButton(target: false);
			});
			bt.SetImage(buildingThumbnail);
			bt.ResetOverlays();
			if (data.code == selected_building)
			{
				bt.AddOverlay(OverlayTypes.SELECTED);
			}
			if (Progress.HasNotUsedBuilding(data.code))
			{
				bt.AddOverlay(OverlayTypes.NEW);
			}
			bt.SetInteractable(target: true);
			bt.SetOnPointerEnter(delegate
			{
				SetHoveringButton_Building(target: true, bt.rtBase, data);
			});
			bt.SetOnPointerExit(delegate
			{
				SetHoveringButton(target: false);
			});
			bt.SetObActive(active: true);
			bt.SetHotkey("");
		}
		SetPosition(current_transform);
	}

	public void UIUpdate()
	{
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
		else if (hoveringBuilding != null)
		{
			buttonHover.SetHover(hoveringBuilding.GetTitle(), hoveringBuilding.GetDescription());
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
	}

	private void SetHoveringButton_Trail(bool target, RectTransform rt = null, TrailData trail_data = null)
	{
		hoveringTrail = trail_data;
		hoveringBuilding = null;
		SetHoveringButton(target, rt);
	}

	private void SetHoveringButton_Building(bool target, RectTransform rt = null, BuildingData building_data = null)
	{
		hoveringTrail = null;
		hoveringBuilding = building_data;
		SetHoveringButton(target, rt);
	}

	private void SetHoveringButton(bool target, RectTransform rt = null)
	{
		followingRT = rt;
		buttonHover.SetObActive(target);
	}
}
