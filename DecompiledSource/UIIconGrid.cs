using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIIconGrid
{
	private TMP_Text lbTitle;

	private List<UIIconItem> spawnedItems;

	private GridLayoutGroup gridLayout;

	private bool keepConstraints;

	private int desiredConstraints = -1;

	public UIIconGrid(TMP_Text lb_title, GridLayoutGroup grid, bool keep_constraints)
	{
		lbTitle = lb_title;
		spawnedItems = new List<UIIconItem>();
		gridLayout = grid;
		keepConstraints = keep_constraints;
	}

	private List<(T, string)> ToPairs<T>(List<T> list) where T : Enum
	{
		List<(T, string)> list2 = new List<(T, string)>();
		foreach (T item in list)
		{
			list2.Add((item, ""));
		}
		return list2;
	}

	private List<(T, string)> ToPairs<T>(Dictionary<T, int> dic, bool include_zero) where T : Enum
	{
		List<(T, string)> list = new List<(T, string)>();
		foreach (KeyValuePair<T, int> item in dic)
		{
			if (item.Value > 0 || include_zero)
			{
				list.Add((item.Key, $"x {item.Value}"));
			}
		}
		return list;
	}

	private List<(T, string)> ToPairs<T>(Dictionary<T, string> dic) where T : Enum
	{
		List<(T, string)> list = new List<(T, string)>();
		foreach (KeyValuePair<T, string> item in dic)
		{
			list.Add((item.Key, item.Value));
		}
		return list;
	}

	public void Update(string title, List<PickupType> pickups, string empty_msg)
	{
		Update(title, ToPairs(pickups), null, empty_msg, no_text: true);
	}

	public void Update(string title, Dictionary<PickupType, int> pickups, string empty_msg, bool no_text = false, bool include_zero = false)
	{
		Update(title, ToPairs(pickups, include_zero), null, empty_msg, no_text);
	}

	public void Update(string title, Dictionary<PickupType, string> pickups, string empty_msg, bool no_text = false)
	{
		Update(title, ToPairs(pickups), null, empty_msg, no_text);
	}

	public void Update(string title, List<(PickupType, string)> pickups, string empty_msg, bool no_text = false)
	{
		Update(title, pickups, null, empty_msg, no_text);
	}

	public void Update(string title, List<(PickupType, string)> pickups, List<(AntCaste, string)> ants, string empty_msg, bool no_text = false)
	{
		if (lbTitle != null)
		{
			lbTitle.Set(title);
		}
		int num = Mathf.Max(pickups.Count + (ants?.Count ?? 0), 1);
		int num2;
		if (spawnedItems.Count < num)
		{
			num2 = num - spawnedItems.Count;
			for (int i = 0; i < num2; i++)
			{
				UIIconItem component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem)), gridLayout.transform).GetComponent<UIIconItem>();
				spawnedItems.Add(component);
			}
		}
		foreach (UIIconItem spawnedItem in spawnedItems)
		{
			spawnedItem.SetObActive(active: false);
		}
		gridLayout.cellSize = new Vector2(no_text ? 50f : 100f, 50f);
		num2 = 0;
		if (ants != null)
		{
			foreach (var (antCaste, text) in ants)
			{
				if (no_text || !(text == ""))
				{
					spawnedItems[num2].SetObActive(active: true);
					spawnedItems[num2].Init(antCaste);
					spawnedItems[num2].SetHoverLocObjects((antCaste == AntCaste.NONE) ? "" : AntCasteData.Get(antCaste).title);
					spawnedItems[num2].SetRaycastTarget(target: true);
					spawnedItems[0].SetImageEnabled(target: true);
					if (!no_text)
					{
						spawnedItems[num2].SetExtraText(0, text);
					}
					num2++;
				}
			}
		}
		if (pickups != null)
		{
			foreach (var (pickupType, text2) in pickups)
			{
				if (no_text || !(text2 == ""))
				{
					spawnedItems[num2].SetObActive(active: true);
					spawnedItems[num2].Init(pickupType);
					spawnedItems[num2].SetHoverLocObjects((pickupType == PickupType.NONE) ? "" : PickupData.Get(pickupType).title);
					spawnedItems[num2].SetRaycastTarget(target: true);
					spawnedItems[0].SetImageEnabled(target: true);
					if (!no_text)
					{
						spawnedItems[num2].SetExtraText(0, text2);
					}
					num2++;
				}
			}
		}
		if (num2 == 0)
		{
			spawnedItems[0].SetObActive(active: true);
			spawnedItems[0].Init(empty_msg);
			spawnedItems[0].SetImageEnabled(target: false);
			gridLayout.cellSize = new Vector2(50f, 50f);
			if (!keepConstraints)
			{
				gridLayout.constraintCount = 1;
			}
		}
		else if (!keepConstraints)
		{
			if (desiredConstraints != -1)
			{
				gridLayout.constraintCount = Mathf.Min(num2, desiredConstraints);
			}
			else
			{
				gridLayout.constraintCount = Mathf.Min(num2, no_text ? 9 : 5);
			}
		}
	}

	public void SetDesiredConstraints(int c)
	{
		desiredConstraints = c;
	}
}
