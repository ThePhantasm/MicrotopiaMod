using UnityEngine;

public class GyneTower : Building
{
	[SerializeField]
	private Transform gyneParent;

	private Ant waitingGyne;

	private int gyneId = -1;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(waitingGyne != null);
		if (waitingGyne != null)
		{
			save.Write(waitingGyne.linkId);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		if (save.ReadBool())
		{
			gyneId = save.ReadInt();
		}
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		if (gyneId != -1)
		{
			waitingGyne = GameManager.instance.FindLink<Ant>(gyneId);
			UseBuilding(0, waitingGyne, out var _);
		}
	}

	protected override void DoDelete()
	{
		if (waitingGyne != null)
		{
			waitingGyne.transform.parent = null;
			waitingGyne.transform.position = base.transform.position;
			waitingGyne.SetCurrentTrail(null);
			waitingGyne.SetColliders(target: true);
			waitingGyne.SetMoveState(MoveState.Normal);
		}
		base.DoDelete();
	}

	public override bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		if (waitingGyne != null || !ant.data.isGyne)
		{
			return false;
		}
		if (GetAntsOnTrails().Count > 0)
		{
			return false;
		}
		return base.CheckIfGateIsSatisfied(ant, trail, out warning);
	}

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		if (!base.TryUseBuilding(_entrance, _ant))
		{
			return false;
		}
		if (waitingGyne != null)
		{
			return false;
		}
		return true;
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		waitingGyne = _ant;
		waitingGyne.SetCurrentTrail(null);
		waitingGyne.SetMoveState(MoveState.Waiting);
		if (gyneParent != null)
		{
			waitingGyne.transform.position = gyneParent.position;
			waitingGyne.transform.parent = gyneParent;
			waitingGyne.SetColliders(target: false);
		}
		if (NuptialFlight.IsNuptialFlightActive())
		{
			StartGyne();
		}
		else
		{
			UIGame.instance.UpdateNuptialFlightStats();
		}
		ant_entered = false;
		return 0f;
	}

	public override void Relocate(Vector3 pos, Quaternion rot)
	{
		Transform parent = null;
		if (waitingGyne != null && gyneParent == null)
		{
			parent = waitingGyne.transform.parent;
			waitingGyne.transform.SetParent(base.transform, worldPositionStays: true);
		}
		base.Relocate(pos, rot);
		if (waitingGyne != null && gyneParent == null)
		{
			waitingGyne.transform.SetParent(parent, worldPositionStays: true);
		}
	}

	public bool HasGyne()
	{
		return waitingGyne != null;
	}

	public void StartGyne()
	{
		if (!(waitingGyne == null))
		{
			NuptialFlightActor nuptialFlightActor = NuptialFlight.SpawnActor(waitingGyne.caste);
			nuptialFlightActor.transform.position = waitingGyne.transform.position;
			nuptialFlightActor.transform.rotation = waitingGyne.transform.rotation;
			nuptialFlightActor.InitFreeForm();
			NuptialFlight.AddGyneFlown(waitingGyne.data.caste);
			History.RegisterAntEnd(waitingGyne, repurposed: true);
			waitingGyne.Delete();
			waitingGyne = null;
			UIGame.instance.UpdateNuptialFlightStats();
		}
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetInfo();
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		if (waitingGyne == null)
		{
			ui_hover.UpdateInfo(Loc.GetUI("BUILDING_GYNE_EMPTY"));
		}
		else
		{
			ui_hover.UpdateInfo(Loc.GetUI("BUILDING_GYNE_PRESENT"));
		}
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		if (waitingGyne == null)
		{
			ui_click.SetInfo(Loc.GetUI("BUILDING_GYNE_EMPTY"));
		}
		else
		{
			ui_click.SetInfo(Loc.GetUI("BUILDING_GYNE_PRESENT"));
		}
	}
}
