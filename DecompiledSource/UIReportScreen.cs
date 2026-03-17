using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIReportScreen : UIBaseSingleton
{
	public static UIReportScreen instance;

	[SerializeField]
	private UITextImageButton btOk;

	[SerializeField]
	private TextMeshProUGUI lbCompleted;

	[SerializeField]
	private TextMeshProUGUI lbGynesDrones;

	[SerializeField]
	private TextMeshProUGUI lbTimePassed;

	[SerializeField]
	private TextMeshProUGUI lbAntsCreated;

	[SerializeField]
	private TextMeshProUGUI lbMatsGained;

	[SerializeField]
	private UITextImageButton prefabAntBox;

	[SerializeField]
	private UITextImageButton prefabMatBox;

	private bool firstTime = true;

	private List<UITextImageButton> listAntBoxes = new List<UITextImageButton>();

	private List<UITextImageButton> listMatBoxes = new List<UITextImageButton>();

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Init(int nup_flight)
	{
		base.Show(target: true);
		if (firstTime)
		{
			btOk.SetButton(delegate
			{
				GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
			});
			firstTime = false;
		}
		GameManager.instance.SetStatus(GameStatus.MENU);
		NuptialFlightData flightData = NuptialFlight.GetFlightData(nup_flight);
		lbCompleted.text = Loc.GetUI("REPORT_COMPLETED", (nup_flight + 1).ToString(), flightData.nDrones.ToString());
		lbGynesDrones.text = Loc.GetUI("REPORT_GYNES_FLOWN", flightData.GetNGynesFlown(rounded: true).ToString()) + "\n" + Loc.GetUI("REPORT_DRONES_ATTRACTED", flightData.nDronesAttracted.ToString());
		float num = 0f;
		float num2 = (float)flightData.GetEndTime();
		if (nup_flight > 0)
		{
			num = (float)NuptialFlight.GetFlightData(nup_flight - 1).GetEndTime();
			lbTimePassed.text = Loc.GetUI("REPORT_TIME_SINCE_LAST", (num2 - num).Unit(PhysUnit.TIME_MINUTES));
		}
		else
		{
			lbTimePassed.text = Loc.GetUI("REPORT_TIME_SINCE_START", (num2 - num).Unit(PhysUnit.TIME_MINUTES));
		}
		prefabAntBox.SetObActive(active: false);
		List<AntCasteHistoryStats> antCasteTotals = History.GetAntCasteTotals(num, num2);
		int num3 = 0;
		int num4 = antCasteTotals.Count - listAntBoxes.Count;
		for (int num5 = 0; num5 < num4; num5++)
		{
			UITextImageButton component = Object.Instantiate(prefabAntBox.gameObject, prefabAntBox.transform.parent).GetComponent<UITextImageButton>();
			listAntBoxes.Add(component);
		}
		for (int num6 = 0; num6 < antCasteTotals.Count; num6++)
		{
			AntCasteHistoryStats antCasteHistoryStats = antCasteTotals[num6];
			UITextImageButton uITextImageButton = listAntBoxes[num6];
			uITextImageButton.Init();
			uITextImageButton.SetObActive(active: true);
			AntCasteData antCasteData = AntCasteData.Get(antCasteHistoryStats.antCaste);
			uITextImageButton.SetImage(antCasteData.GetIcon());
			uITextImageButton.SetText(antCasteData.GetTitleFull());
			uITextImageButton.SetExtraText(0, antCasteHistoryStats.nBorn.ToString());
			uITextImageButton.SetExtraText(1, antCasteHistoryStats.nRepurposed.ToString());
			uITextImageButton.SetExtraText(2, antCasteHistoryStats.nDied.ToString());
			num3 += antCasteHistoryStats.nBorn;
		}
		for (int num7 = antCasteTotals.Count; num7 < listAntBoxes.Count; num7++)
		{
			listAntBoxes[num7].SetObActive(active: false);
		}
		lbAntsCreated.text = Loc.GetUI("REPORT_ANTS_CREATED", num3.ToString());
	}
}
