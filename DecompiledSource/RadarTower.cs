using UnityEngine;

public class RadarTower : Building
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
		bool flag = NuptialFlight.IsNuptialFlightActive();
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
		if (!CanStartNuptialFlight())
		{
			return NuptialFlight.IsNuptialFlightActive();
		}
		return true;
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		if (CanStartNuptialFlight())
		{
			ui_hover.SetButtonWithText(delegate
			{
				NuptialFlight.StartFlight();
			}, clear_on_click: true, Loc.GetUI("BUILDING_RADAR_CONTACT"));
		}
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		if (CanStartNuptialFlight())
		{
			ui_hover.UpdateButtonWithText(Loc.GetUI("BUILDING_RADAR_CONTACT"));
		}
		else
		{
			ui_hover.UpdateButtonWithText("", enabled: false);
		}
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		if (CanStartNuptialFlight())
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
