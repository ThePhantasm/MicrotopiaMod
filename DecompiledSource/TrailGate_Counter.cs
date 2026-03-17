using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailGate_Counter : TrailGate
{
	[Header("Counter Gate")]
	[SerializeField]
	private Transform progressBar;

	[SerializeField]
	private Transform dividerPrefab;

	[SerializeField]
	private float widthProgress = 2.8f;

	[SerializeField]
	private float widthDivider = 0.05f;

	[NonSerialized]
	public int crewSize = 1;

	public const int maxCrewSize = 50;

	private float nAnts;

	private List<Transform> dividers = new List<Transform>();

	public static TrailGate_Counter curSelected;

	public static List<Trail> trailsToCheck = new List<Trail>();

	private HashSet<Trail> counterArea = new HashSet<Trail>();

	private List<(Building, int)> counterArea_buildings = new List<(Building, int)>();

	private bool areaInvalid;

	private bool updateArea;

	[NonSerialized]
	public AreaMode areaMode = AreaMode.StopAtGates_IncludeFlowingBack;

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_COUNTER;
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Counter trailGate_Counter = other as TrailGate_Counter;
		crewSize = trailGate_Counter.crewSize;
		areaMode = trailGate_Counter.areaMode;
		updateArea = true;
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(crewSize);
		save.Write((int)areaMode);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		crewSize = save.ReadInt();
		if (save.GetVersion() >= 87)
		{
			areaMode = (AreaMode)save.ReadInt();
		}
		else
		{
			areaMode = AreaMode.StopAtGates_IncludeFlowingBack;
		}
		updateArea = true;
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load: false);
		if (!during_load)
		{
			areaMode = AreaMode.StopAtGates_IncludeFlowingBack;
		}
		externalControl = true;
		updateArea = true;
	}

	protected override void GateUpdate()
	{
		if (!GameManager.instance.runEditing)
		{
			return;
		}
		if (!updateArea)
		{
			foreach (Trail item in trailsToCheck)
			{
				if (item != null && counterArea.Contains(item))
				{
					updateArea = true;
					break;
				}
			}
		}
		if (updateArea)
		{
			counterArea = ownerTrail.GetCounterArea(areaMode, out areaInvalid, out counterArea_buildings);
			updateArea = false;
		}
		base.GateUpdate();
	}

	public override void UpdateVisual(float dt)
	{
		base.UpdateVisual(dt);
		if (nAnts == 0f || crewSize == 0)
		{
			progressBar.SetObActive(active: false);
			{
				foreach (Transform divider in dividers)
				{
					divider.SetObActive(active: false);
				}
				return;
			}
		}
		float z = Mathf.Clamp01(nAnts / (float)crewSize);
		progressBar.SetObActive(active: true);
		progressBar.localScale = new Vector3(1f, 1f, z);
		if (crewSize <= 20)
		{
			if (dividers.Count < crewSize)
			{
				int num = crewSize - dividers.Count;
				for (int i = 0; i < num; i++)
				{
					Transform item = UnityEngine.Object.Instantiate(dividerPrefab, dividerPrefab.parent).transform;
					dividers.Add(item);
				}
			}
			int count = dividers.Count;
			for (int j = 0; j < count; j++)
			{
				if (j == 0 || (float)j >= nAnts || j >= crewSize)
				{
					dividers[j].SetObActive(active: false);
					continue;
				}
				dividers[j].position = progressBar.position + (widthProgress + widthDivider) / (float)crewSize * (float)j * progressBar.forward;
				dividers[j].SetObActive(active: true);
			}
			return;
		}
		foreach (Transform divider2 in dividers)
		{
			divider2.SetObActive(active: false);
		}
	}

	public override bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		UpdateBillboard();
		if (areaInvalid)
		{
			return false;
		}
		nAnts = 0f;
		foreach (Trail item in counterArea)
		{
			nAnts += item.currentAnts.Count;
		}
		foreach (var (building, entrance) in counterArea_buildings)
		{
			if (building == null || building.deleted)
			{
				updateArea = true;
			}
			else
			{
				nAnts += building.GetCounterAntCount(entrance);
			}
		}
		bool flag = nAnts < (float)crewSize;
		if (final)
		{
			ShowAllowAnt(flag, entering: true, chain_satisfied);
		}
		return flag;
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.TRAILGATE_COUNTER;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_TrailGateCounter ui_counter = (UIClickLayout_TrailGateCounter)ui_click;
		ui_counter.SetTitle(Loc.GetObject("TRAIL_ASSIGNER"));
		ui_counter.SetCounter(this);
		ui_counter.SetButton(UIClickButtonType.ChangeMode, delegate
		{
			_ = Enum.GetNames(typeof(AreaMode)).Length;
			SwitchAreaMode();
			updateArea = true;
			UIClickLayout_TrailGateCounter uIClickLayout_TrailGateCounter2 = ui_counter;
			string[] array2 = new string[1];
			int num2 = (int)areaMode;
			array2[0] = num2.ToString();
			uIClickLayout_TrailGateCounter2.UpdateButton(UIClickButtonType.ChangeMode, enabled: true, Loc.GetUI("AREAMODE_MODE", array2));
			ui_counter.SetButtonHover(UIClickButtonType.ChangeMode, GetAreaModeDescription());
			ui_click.StartCoroutine(CShowHoverNextFrame(ui_counter.GetButton(UIClickButtonType.ChangeMode).btButton_better, Loc.GetUI(GetAreaModeDescription())));
		}, InputAction.None);
		UIClickLayout_TrailGateCounter uIClickLayout_TrailGateCounter = ui_counter;
		string[] array = new string[1];
		int num = (int)areaMode;
		array[0] = num.ToString();
		uIClickLayout_TrailGateCounter.UpdateButton(UIClickButtonType.ChangeMode, enabled: true, Loc.GetUI("AREAMODE_MODE", array));
		ui_counter.SetButtonHover(UIClickButtonType.ChangeMode, GetAreaModeDescription());
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		((UIClickLayout_TrailGateCounter)ui_click).UpdateCounter(this);
	}

	private string GetAreaModeDescription()
	{
		return areaMode switch
		{
			AreaMode.StopNever => "AREAMODE_STOPNEVER", 
			AreaMode.StopAtGates_IncludeFlowingBack => "AREAMODE_STOPATGATES_INCLUDEFLOW", 
			AreaMode.StopAtGates_IncludeFlowingBack_StopAtEnds => "AREAMODE_STOPATGATES_INCLUDEFLOW_STOPATENDS", 
			AreaMode.StopAtGates => "AREAMODE_STOPATGATES", 
			AreaMode.StopAtEnds => "AREAMODE_STOPATENDS", 
			_ => "", 
		};
	}

	private IEnumerator CShowHoverNextFrame(UIBase ui, string txt)
	{
		yield return 1;
		UIHover.instance.Init(ui);
		UIHover.instance.SetWidth();
		UIHover.instance.SetText(txt);
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		if (is_selected != was_selected)
		{
			if (is_selected)
			{
				curSelected = this;
			}
			else
			{
				curSelected = null;
			}
		}
	}

	public void HighLightLoop()
	{
		TrailPart.HighLight(counterArea, (!areaInvalid) ? TrailStatus.HOVERING : TrailStatus.HOVERING_ERROR, also_building: true);
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (areaInvalid)
		{
			code_desc = "GATE_COUNTER_ERROR";
			col = Color.red;
			return BillboardType.CROSS_SMALL;
		}
		return BillboardType.NONE;
	}

	private void SwitchAreaMode()
	{
		switch (areaMode)
		{
		case AreaMode.StopNever:
			areaMode = AreaMode.StopAtGates_IncludeFlowingBack;
			break;
		case AreaMode.StopAtGates_IncludeFlowingBack:
			areaMode = AreaMode.StopAtGates_IncludeFlowingBack_StopAtEnds;
			break;
		case AreaMode.OldCalculation:
			areaMode = AreaMode.StopAtGates_IncludeFlowingBack_StopAtEnds;
			break;
		case AreaMode.StopAtGates_IncludeFlowingBack_StopAtEnds:
			areaMode = AreaMode.StopAtGates;
			break;
		case AreaMode.StopAtGates:
			areaMode = AreaMode.StopAtEnds;
			break;
		case AreaMode.StopAtEnds:
			areaMode = AreaMode.StopNever;
			break;
		}
	}
}
