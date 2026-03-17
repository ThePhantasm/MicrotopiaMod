using UnityEngine;

public class RadarIslandScanner : Unlocker
{
	private bool isActive;

	[SerializeField]
	private AudioLink audioActiveLoop;

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (during_load && ShouldBeOpen())
		{
			anim.SetTrigger("StartOpen");
		}
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (!runWorld || currentStatus != BuildingStatus.COMPLETED)
		{
			return;
		}
		anim.SetBool(ClickableObject.paramOpen, ShouldBeOpen());
		bool flag = NuptialFlight.IsNuptialFlightActive() || isRevealing;
		if (isActive != flag)
		{
			isActive = flag;
			anim.SetBool(ClickableObject.paramDoAction, isActive);
			if (isActive)
			{
				StartLoopAudio(audioActiveLoop);
			}
			else
			{
				StopAudio();
			}
		}
	}

	private bool CanStartNuptialFlight()
	{
		if (NuptialFlight.IsNuptialFlightActive())
		{
			return false;
		}
		return NuptialFlight.GetSeenNuptialFlights() == 0;
	}

	private bool ShouldBeOpen()
	{
		if (!CanStartNuptialFlight() && !NuptialFlight.IsNuptialFlightActive())
		{
			return isRevealing;
		}
		return true;
	}

	public override UIClickType GetUiClickType_Intake()
	{
		if (NuptialFlight.GetSeenNuptialFlights() == 0)
		{
			return UIClickType.BUILDING_SMALL;
		}
		if ((NuptialFlight.GetSeenNuptialFlights() == 1 && !NuptialFlight.IsNuptialFlightActive()) || NuptialFlight.GetSeenNuptialFlights() > 1)
		{
			return base.GetUiClickType_Intake();
		}
		return UIClickType.BUILDING_SMALL;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		if (GetUiClickType_Intake() == UIClickType.BUILDING_SMALL && CanStartNuptialFlight())
		{
			ui_building.SetButton(UIClickButtonType.Generic1, delegate
			{
				NuptialFlight.StartFlight();
			}, InputAction.None);
		}
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		if (GetUiClickType_Intake() == UIClickType.BUILDING_SMALL)
		{
			if (CanStartNuptialFlight())
			{
				ui_click.UpdateButton(UIClickButtonType.Generic1, enabled: true, Loc.GetUI("BUILDING_RADAR_CONTACT"));
			}
			else
			{
				ui_click.UpdateButton(UIClickButtonType.Generic1, enabled: false);
			}
		}
	}

	public override bool CanTrack()
	{
		if (currentStatus != BuildingStatus.BUILDING && !UIGame.instance.IsTrackingBuilding(this))
		{
			return GetUiClickType_Intake() == UIClickType.UNLOCKER;
		}
		return true;
	}
}
