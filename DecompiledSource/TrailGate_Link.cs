using System;
using System.Collections.Generic;
using UnityEngine;

public class TrailGate_Link : TrailGate
{
	[Header("Trail Link")]
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

	private List<Ant> linkedAnts = new List<Ant>();

	private List<Transform> dividers = new List<Transform>();

	public static TrailGate_Link curSelected;

	private int[] antIds;

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_LINK;
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Link trailGate_Link = other as TrailGate_Link;
		crewSize = trailGate_Link.crewSize;
		if (copy_mode == GateCopyMode.Settings)
		{
			return;
		}
		linkedAnts.Clear();
		foreach (Ant linkedAnt in trailGate_Link.linkedAnts)
		{
			linkedAnts.Add(linkedAnt);
		}
	}

	public override void WriteConfig(ISaveContainer save)
	{
		save.Write(crewSize);
		save.Write(linkedAnts.Count);
		foreach (Ant linkedAnt in linkedAnts)
		{
			save.Write(linkedAnt.linkId);
		}
	}

	public override void ReadConfig(ISaveContainer save)
	{
		crewSize = save.ReadInt();
		int num = save.ReadInt();
		antIds = new int[num];
		for (int i = 0; i < num; i++)
		{
			antIds[i] = save.ReadInt();
		}
	}

	public override void LoadLinks()
	{
		base.LoadLinks();
		linkedAnts.Clear();
		int[] array = antIds;
		foreach (int id in array)
		{
			Ant item = GameManager.instance.FindLink<Ant>(id);
			linkedAnts.Add(item);
		}
	}

	public override void UpdateVisual(float dt)
	{
		base.UpdateVisual(dt);
		List<ClickableObject> list = new List<ClickableObject>();
		foreach (Ant linkedAnt in linkedAnts)
		{
			if (linkedAnt != null && (curSelected == this || Gameplay.instance.IsSelected(linkedAnt)))
			{
				list.Add(linkedAnt);
			}
		}
		ShowAssignLines(list, AssignType.LINK);
		UpdateAssignLines();
		int count = linkedAnts.Count;
		externalControl = count < crewSize;
		if (count == 0 || crewSize == 0)
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
		float z = Mathf.Clamp01((float)count / (float)crewSize);
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
			int count2 = dividers.Count;
			for (int j = 0; j < count2; j++)
			{
				if (j == 0 || j >= count || j >= crewSize)
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
		CheckLinkedAnts();
		bool flag = linkedAnts.Count < crewSize || linkedAnts.Contains(_ant);
		if (final)
		{
			ShowAllowAnt(flag, entering: true, chain_satisfied);
		}
		return flag;
	}

	public override void EnterGate(Ant _ant)
	{
		base.EnterGate(_ant);
		Assign(_ant);
	}

	public void CheckLinkedAnts()
	{
		List<Ant> list = new List<Ant>();
		foreach (Ant linkedAnt in linkedAnts)
		{
			if (linkedAnt == null || (linkedAnt.IsDead() && linkedAnt.moveState != MoveState.DeadAndDisabled))
			{
				list.Add(linkedAnt);
			}
		}
		foreach (Ant item in list)
		{
			linkedAnts.Remove(item);
		}
	}

	public override void Assign(ClickableObject target, bool add = true)
	{
		if (!(target is Ant item))
		{
			Debug.LogError("Wrong assignment");
			return;
		}
		if (add)
		{
			if (!linkedAnts.Contains(item))
			{
				linkedAnts.Add(item);
			}
		}
		else if (linkedAnts.Contains(item))
		{
			linkedAnts.Remove(item);
		}
		UpdateBillboard();
	}

	public override IEnumerable<ClickableObject> EAssignedObjects()
	{
		foreach (Ant linkedAnt in linkedAnts)
		{
			yield return linkedAnt;
		}
	}

	public override AssignType GetAssignType()
	{
		return AssignType.GATE;
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.TRAILGATE_LINK;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		UIClickLayout_TrailGateLink obj = (UIClickLayout_TrailGateLink)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_GATELINK"));
		obj.SetLink(this, show_panel: true);
		obj.SetButton(UIClickButtonType.Clear, delegate
		{
			linkedAnts.Clear();
		}, InputAction.Delete);
	}

	public override void SetClickUi_LogicControl(UIClickLayout ui_click)
	{
		UIClickLayout_TrailGateLink obj = (UIClickLayout_TrailGateLink)ui_click;
		obj.SetTitle(Loc.GetObject("TRAIL_GATELINK"));
		obj.SetLink(this, show_panel: false);
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		UIClickLayout_TrailGateLink obj = (UIClickLayout_TrailGateLink)ui_click;
		obj.UpdateLink(linkedAnts);
		obj.UpdateButton(UIClickButtonType.Clear, linkedAnts.Count > 0, Loc.GetUI("GATE_CLEAR_LINKS"));
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		CheckLinkedAnts();
		if (is_selected != was_selected)
		{
			if (is_selected)
			{
				curSelected = this;
				return;
			}
			curSelected = null;
			HideAssignLines();
		}
	}
}
