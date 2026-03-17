using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITask_NuptialFlight : UITask
{
	[Header("Nuptial Flight")]
	[SerializeField]
	private TMP_Text lbTitle;

	[SerializeField]
	private TMP_Text lbTimeInfo;

	[SerializeField]
	private TMP_Text lbTime;

	[SerializeField]
	private TMP_Text lbNWaiting;

	[SerializeField]
	private TMP_Text lbNTotal;

	[SerializeField]
	private TMP_Text lbNRecord;

	[SerializeField]
	private Slider slFlightProgress;

	public override void Init(Action on_click_toggleOpen)
	{
		base.Init(on_click_toggleOpen);
		UpdateStats();
	}

	public void UpdateStats()
	{
		int num = 0;
		foreach (GyneTower item in GameManager.instance.EBuildings<GyneTower>())
		{
			if (item.HasGyne())
			{
				num++;
			}
		}
		lbNWaiting.text = num.ToString();
		int num2 = 0;
		int a = 0;
		foreach (NuptialFlightData item2 in NuptialFlight.EFlightData())
		{
			int num3 = 0;
			foreach (KeyValuePair<AntCaste, int> dicFlownGyne in item2.dicFlownGynes)
			{
				num3 += dicFlownGyne.Value;
			}
			num2 += num3;
			a = Mathf.Max(a, num3);
		}
		lbNTotal.text = num2.ToString();
		lbNRecord.text = a.ToString();
	}

	public override void UIUpdate()
	{
		base.UIUpdate();
		NuptialFlightData currentFlight = NuptialFlight.GetCurrentFlight();
		if (currentFlight == null || currentFlight.stage == NuptialFlightStage.NONE)
		{
			lbTimeInfo.text = Loc.GetUI("NUPFLIGHT_TIME_UNTIL");
			lbTime.text = Loc.GetUI("GENERIC_???");
			lbTime.SetObActive(active: true);
			slFlightProgress.SetObActive(active: false);
			return;
		}
		if (currentFlight.stage == NuptialFlightStage.WARM_UP || currentFlight.stage == NuptialFlightStage.ACTIVE)
		{
			lbTimeInfo.text = Loc.GetUI("NUPFLIGHT_CURRENTLY_ACTIVE");
			lbTime.SetObActive(active: false);
			slFlightProgress.SetObActive(active: true);
			slFlightProgress.value = NuptialFlight.GetCurrentFlight().GetProgress();
			return;
		}
		lbTimeInfo.text = Loc.GetUI("NUPFLIGHT_TIME_UNTIL");
		lbTime.SetObActive(active: true);
		slFlightProgress.SetObActive(active: false);
		if (currentFlight.stage == NuptialFlightStage.WAITING)
		{
			lbTime.text = ((float)(currentFlight.timeStart - GameManager.instance.gameTime)).Unit(PhysUnit.TIME_MINUTES);
		}
		else if (currentFlight.stage == NuptialFlightStage.FLY_OFF)
		{
			NuptialFlightData nextFlight = NuptialFlight.GetNextFlight();
			if (nextFlight == null)
			{
				lbTime.text = Loc.GetUI("GENERIC_???");
			}
			else
			{
				lbTime.text = ((float)(nextFlight.timeStart - GameManager.instance.gameTime)).Unit(PhysUnit.TIME_MINUTES);
			}
		}
	}

	public override TaskID GetUID()
	{
		return TaskID.Nup();
	}
}
