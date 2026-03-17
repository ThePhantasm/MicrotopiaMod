using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIClickLayout_Building : UIClickLayout
{
	[Header("Building")]
	[SerializeField]
	private RectTransform rtInventory;

	[SerializeField]
	private TextMeshProUGUI lbInventory;

	[SerializeField]
	[FormerlySerializedAs("gridInventory")]
	private GridLayoutGroup inventoryGridLayoutGroup;

	[NonSerialized]
	public UIIconGrid inventoryGrid;

	protected override void MyAwake()
	{
		inventoryGrid = new UIIconGrid(lbInventory, inventoryGridLayoutGroup, keep_constraints: false);
	}

	public void SetInventory(bool target)
	{
		rtInventory.SetObActive(target);
	}
}
