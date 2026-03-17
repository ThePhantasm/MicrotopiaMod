using UnityEngine;

public class InventorPad : Building
{
	[Header("Inventor Pad")]
	[SerializeField]
	private bool auto;

	[SerializeField]
	private Transform inventorPoint;

	private AntInventor inventor;

	private int inventorId = -1;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(InventorPresent());
		if (InventorPresent())
		{
			save.Write(inventor.linkId);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		if (save.ReadBool())
		{
			inventorId = save.ReadInt();
		}
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		if (inventorId != -1)
		{
			inventor = GameManager.instance.FindLink<AntInventor>(inventorId);
			UseBuilding(0, inventor, out var _);
		}
	}

	protected override void DoDelete()
	{
		if (InventorPresent())
		{
			inventor.transform.parent = null;
			inventor.transform.position = base.transform.position;
			inventor.SetCurrentTrail(null);
			inventor.SetColliders(target: true);
			inventor.SetAutoMode(target: false);
		}
		base.DoDelete();
	}

	public override bool OpenUiOnClick()
	{
		if (InventorPresent())
		{
			return inventor.OpenUiOnClick();
		}
		return true;
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		if (InventorPresent() && !auto)
		{
			inventor.OnSelected(is_selected, was_selected);
		}
	}

	public override bool IsClickable()
	{
		if (InventorPresent() && !auto)
		{
			return inventor.IsClickable();
		}
		return true;
	}

	public override bool ShouldPlayClickAudio()
	{
		if (InventorPresent())
		{
			return inventor.ShouldPlayClickAudio();
		}
		return base.ShouldPlayClickAudio();
	}

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		return !InventorPresent();
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		if (!(_ant is AntInventor antInventor))
		{
			Vector3 vector = base.transform.position - base.transform.forward * 11f + new Vector3(Random.insideUnitCircle.x, 0f, Random.insideUnitCircle.y);
			Quaternion rotation = Quaternion.LookRotation(Toolkit.LookVector(_ant.transform.position, vector.TargetYPosition(_ant.transform.position.y)));
			_ant.SetCurrentTrail(null);
			_ant.transform.SetPositionAndRotation(vector, rotation);
		}
		else
		{
			inventor = antInventor;
			inventor.transform.parent = base.transform;
			inventor.SetColliders(inventor.TechReady());
			if (auto)
			{
				inventor.SetAutoMode(target: true);
			}
			if (inventorPoint != null)
			{
				inventor.transform.SetPositionAndRotation(inventorPoint.position, inventorPoint.rotation);
			}
		}
		ant_entered = true;
		return 0f;
	}

	public override bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		if (InventorPresent() || !(ant is AntInventor))
		{
			return false;
		}
		if (AnyAntsOnBuildingTrails(trail))
		{
			return false;
		}
		return base.CheckIfGateIsSatisfied(ant, trail, out warning);
	}

	private bool InventorPresent()
	{
		if (inventor == null)
		{
			return false;
		}
		if (inventor.IsDead())
		{
			inventor = null;
			return false;
		}
		return true;
	}

	public override Vector3 GetInsertPos(Pickup pickup = null)
	{
		if (InventorPresent() && !auto)
		{
			return inventor.insertPoint.position;
		}
		return base.GetInsertPos((Pickup)null);
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (!InventorPresent())
		{
			return false;
		}
		return inventor.CanEatPickup(_type);
	}

	protected override void OnPickupArrival_Intake(Pickup p, ExchangePoint point)
	{
		if (incomingPickups_intake.Contains(p))
		{
			incomingPickups_intake.Remove(p);
		}
		if (InventorPresent())
		{
			inventor.EatPickup(p);
		}
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		if (InventorPresent())
		{
			inventor.SetHoverUI_Inventor(ui_hover, include_health: true);
		}
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		if (InventorPresent())
		{
			inventor.UpdateHoverUI_Inventor(ui_hover, include_health: true);
		}
	}

	public override UIClickType GetUiClickType_Intake()
	{
		if (InventorPresent())
		{
			return UIClickType.INVENTOR_PAD;
		}
		return UIClickType.BUILDING_SMALL;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		if (InventorPresent())
		{
			inventor.SetClickUI_Inventor((UIClickLayout_InventorPad)ui_building);
		}
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		if (InventorPresent())
		{
			inventor.UpdateClickUI_Inventor((UIClickLayout_InventorPad)ui_click);
		}
	}
}
