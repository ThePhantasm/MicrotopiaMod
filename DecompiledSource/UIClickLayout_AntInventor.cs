using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIClickLayout_AntInventor : UIClickLayout_Ant
{
	[Header("Inventor")]
	[SerializeField]
	private RectTransform rtInventory;

	[SerializeField]
	private TextMeshProUGUI lbInventory;

	[SerializeField]
	[FormerlySerializedAs("gridInventory")]
	private GridLayoutGroup inventoryGridLayout;

	[NonSerialized]
	public UIIconGrid inventoryGrid;

	protected override void MyAwake()
	{
		base.MyAwake();
		inventoryGrid = new UIIconGrid(lbInventory, inventoryGridLayout, keep_constraints: false);
	}

	public void SetInventory(bool target)
	{
		rtInventory.SetObActive(target);
	}
}
