using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITask_TrackBuilding : UITask
{
	private enum InfoType
	{
		None,
		BuildProgress,
		Factory,
		Unlocker,
		Scanner
	}

	[Header("Track Building")]
	[SerializeField]
	private TMP_Text lbTitle;

	[Header("Track Building")]
	[SerializeField]
	private TMP_Text lbStatus;

	[SerializeField]
	private UIButton btClose;

	[SerializeField]
	private UIButton btGoToBuilding;

	[SerializeField]
	private GridLayoutGroup gridLayoutGroup;

	[SerializeField]
	private GameObject obMainIcon;

	[SerializeField]
	private UIIconItem mainIcon;

	private bool buildProgress;

	private Factory factory;

	private Unlocker unlocker;

	private RadarIslandScanner scanner;

	private string building_name;

	private UIIconGrid inventoryGrid;

	private int prevHash;

	private InfoType infoType;

	private bool buildingDone;

	public Building building { get; private set; }

	protected override void MyAwake()
	{
		base.MyAwake();
		inventoryGrid = new UIIconGrid(null, gridLayoutGroup, keep_constraints: true);
	}

	public void Init(Building b, Action on_click_toggleOpen)
	{
		Init(on_click_toggleOpen);
		building = b;
		factory = b as Factory;
		scanner = b as RadarIslandScanner;
		unlocker = ((scanner == null) ? (b as Unlocker) : null);
		buildProgress = b.currentStatus == BuildingStatus.BUILDING || (factory == null && unlocker == null && scanner == null);
		building_name = building.data.GetTitle();
		btClose.Init(delegate
		{
			CloseTracker();
		});
		btGoToBuilding.Init(delegate
		{
			if (building != null)
			{
				CamController.instance.View(building.transform);
				Gameplay.instance.Select(building);
			}
		});
		prevHash = -1;
		infoType = InfoType.None;
		buildingDone = false;
	}

	private void CloseTracker()
	{
		UIGame.instance.TrackBuilding(building, track: false);
	}

	public override void UIUpdate()
	{
		base.UIUpdate();
		InfoType infoType = (buildProgress ? InfoType.BuildProgress : ((factory != null) ? InfoType.Factory : ((unlocker != null) ? InfoType.Unlocker : ((!(scanner != null)) ? InfoType.BuildProgress : InfoType.Scanner))));
		bool refresh_needed = infoType != this.infoType;
		this.infoType = infoType;
		if (this.infoType == InfoType.BuildProgress)
		{
			if (!buildingDone && building.currentStatus != BuildingStatus.BUILDING)
			{
				buildingDone = true;
				AudioManager.PlayUI(UISfx.TrackedBuildingComplete);
			}
			lbTitle.text = building_name + " " + Loc.GetUI("GENERIC_PERCENTAGE", buildingDone ? "100" : building.GetProgressText());
		}
		else
		{
			lbTitle.text = building_name;
		}
		if (open)
		{
			UpdateContents(ref refresh_needed);
			if (refresh_needed)
			{
				Open(target: true);
			}
		}
	}

	public override void Open(bool target, bool instant = false)
	{
		if (target && infoType != InfoType.None)
		{
			bool refresh_needed = true;
			UpdateContents(ref refresh_needed);
		}
		base.Open(target, instant);
	}

	private void UpdateContents(ref bool refresh_needed)
	{
		int num = 0;
		int num2;
		switch (infoType)
		{
		case InfoType.BuildProgress:
			foreach (KeyValuePair<PickupType, int> item in building.dicCollectedPickups_build)
			{
				num += item.Value;
			}
			num = (int)(num + building.currentStatus);
			num2 = num;
			break;
		case InfoType.Factory:
			foreach (KeyValuePair<PickupType, int> item2 in factory.dicCollectedPickups_intake)
			{
				num += item2.Value;
			}
			num += factory.GetNAntsInside();
			num2 = factory.GetProcessingRecipe().GetHashCode() ^ num;
			break;
		case InfoType.Unlocker:
			foreach (KeyValuePair<PickupType, int> item3 in this.unlocker.dicCollectedPickups_intake)
			{
				num += item3.Value;
			}
			num += this.unlocker.GetNAntsInside();
			num += (this.unlocker.AnythingToUnlock() ? 1 : 0);
			num2 = this.unlocker.GetUnlockCode().GetHashCode() ^ num;
			break;
		case InfoType.Scanner:
			foreach (KeyValuePair<PickupType, int> item4 in scanner.dicCollectedPickups_intake)
			{
				num += item4.Value;
			}
			num = (int)(num + scanner.GetUiClickType());
			num2 = scanner.GetUnlockCode().GetHashCode() ^ num;
			break;
		default:
			num2 = -1;
			break;
		}
		if (num2 == prevHash && !refresh_needed)
		{
			return;
		}
		prevHash = num2;
		switch (infoType)
		{
		case InfoType.BuildProgress:
			if (buildingDone || building.currentStatus != BuildingStatus.BUILDING)
			{
				SetGridVisible(vis: false, ref refresh_needed);
				lbStatus.SetObActive(active: true);
				lbStatus.text = Loc.GetUI("BUILDING_PERCENTAGECOMPLETED", "100");
			}
			else
			{
				SetGridVisible(vis: true, ref refresh_needed);
				inventoryGrid.Update(Loc.GetUI("BUILDING_BUILDMATERIALS"), building.GetBuildProgressPairs(), "");
				lbStatus.SetObActive(active: false);
			}
			obMainIcon.SetObActive(active: false);
			break;
		case InfoType.Factory:
		{
			obMainIcon.SetObActive(active: true);
			factory.GatherRecipeProgress(mainIcon, show_ingredients: true, out var ant_icons2, out var pickup_icons2, out var text, out var status, out var _, out var _);
			lbStatus.SetObActive(active: true);
			lbStatus.text = status;
			mainIcon.SetHoverText(text);
			FillGrid(ant_icons2, pickup_icons2, ref refresh_needed);
			break;
		}
		case InfoType.Unlocker:
		case InfoType.Scanner:
		{
			Unlocker unlocker = this.unlocker;
			switch (infoType)
			{
			case InfoType.Unlocker:
				if (!this.unlocker.AnythingToUnlock())
				{
					CloseTracker();
					return;
				}
				break;
			case InfoType.Scanner:
				if (scanner.GetUiClickType() != UIClickType.UNLOCKER)
				{
					CloseTracker();
					return;
				}
				unlocker = scanner;
				break;
			}
			unlocker.GetUnlockInfo(out var _, out var result, out var sprite);
			obMainIcon.SetObActive(active: true);
			mainIcon.Init(sprite);
			mainIcon.SetHoverText(result);
			unlocker.GatherRecipeProgress(out var ant_icons, out var pickup_icons, out var go);
			if (go)
			{
				SetGridVisible(vis: false, ref refresh_needed);
				lbStatus.SetObActive(active: true);
				if (infoType == InfoType.Unlocker)
				{
					lbStatus.text = Loc.GetUI("BUILDING_PERCENTAGECOMPLETED", "100");
				}
				else
				{
					lbStatus.text = Loc.GetUI("GENERIC_PERCENTAGE", "100");
				}
			}
			else
			{
				SetGridVisible(vis: true, ref refresh_needed);
				FillGrid(ant_icons, pickup_icons, ref refresh_needed);
				lbStatus.SetObActive(active: false);
			}
			break;
		}
		default:
			Debug.LogWarning($"UITask_TrackBuilding: unknown InfoType {infoType} for {building.data.code}");
			break;
		}
	}

	private void SetGridVisible(bool vis, ref bool refresh_needed)
	{
		if (gridLayoutGroup.SetObActive(vis))
		{
			refresh_needed = true;
		}
	}

	private void FillGrid(List<(AntCaste, string)> ant_icons, List<(PickupType, string)> pickup_icons, ref bool refresh_needed)
	{
		if ((ant_icons == null || ant_icons.Count == 0) && (pickup_icons == null || pickup_icons.Count == 0))
		{
			SetGridVisible(vis: false, ref refresh_needed);
			return;
		}
		SetGridVisible(vis: true, ref refresh_needed);
		inventoryGrid.Update("", pickup_icons, ant_icons, "");
	}

	public override TaskID GetUID()
	{
		return TaskID.Building(building);
	}
}
