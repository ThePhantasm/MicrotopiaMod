using UnityEngine;

public class LarvaStorage : Stockpile
{
	[SerializeField]
	private Transform[] randomRots;

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		Transform[] array = randomRots;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Rotate(0f, Random.Range(0f, 360f), 0f);
		}
	}

	protected override void UpdateTopPoint()
	{
	}

	protected override string CapacityInfo()
	{
		return Loc.GetUI("BUILDING_LARVAE_CAPACITY", data.storageCapacity.ToString());
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (triedInsert != PickupState.NONE && !allowedStates.Contains(triedInsert))
		{
			code_desc = "BUILDING_LARVASTO_WRONG";
			col = Color.red;
			return BillboardType.CROSS_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}
}
