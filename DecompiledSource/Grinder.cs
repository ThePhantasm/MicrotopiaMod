using UnityEngine;

public class Grinder : Factory
{
	private bool triedInsertWhileFull;

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		bool flag = base.CanInsert_Intake(_type, exchange, point, ref let_ant_wait, show_billboard);
		triedInsertWhileFull = show_billboard && !flag && OutputFull() && !spitOutProduct;
		UpdateBillboardTempory();
		return flag;
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		ClearBillboard();
		UpdateBillboard();
		return base.ExtractPickup(_type);
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (triedInsertWhileFull)
		{
			code_desc = "BUILDING_GRINDER_NEED_EMPTY";
			col = Color.red;
			return BillboardType.CROSS_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}

	protected override void ClearBillboard()
	{
		triedInsertWhileFull = false;
	}
}
