using System;
using System.Collections.Generic;
using UnityEngine;

public class UIRecipeMenu : UIBaseSingleton
{
	public static UIRecipeMenu instance;

	[SerializeField]
	private int scrollViewItemCount = 10;

	[SerializeField]
	private RectTransform rtRegular;

	[SerializeField]
	private RectTransform rtScrollView;

	[SerializeField]
	private UIRecipeMenuItem itemPrefab_regular;

	[SerializeField]
	private UIRecipeMenuItem itemPrefab_scrollView;

	private List<UIRecipeMenuItem> spawnedItems_regular = new List<UIRecipeMenuItem>();

	private List<UIRecipeMenuItem> spawnedItems_scrollView = new List<UIRecipeMenuItem>();

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void SetRecipes(Factory _factory)
	{
		List<string> list = new List<string>();
		list.Add("");
		list.AddRange(_factory.EFactoryRecipes());
		rtRegular.SetObActive(list.Count <= scrollViewItemCount);
		rtScrollView.SetObActive(list.Count > scrollViewItemCount);
		UIRecipeMenuItem uIRecipeMenuItem = ((list.Count > scrollViewItemCount) ? itemPrefab_scrollView : itemPrefab_regular);
		List<UIRecipeMenuItem> list2 = ((list.Count > scrollViewItemCount) ? spawnedItems_scrollView : spawnedItems_regular);
		uIRecipeMenuItem.SetObActive(active: false);
		if (list2.Count < list.Count)
		{
			int num = list.Count - list2.Count;
			for (int i = 0; i < num; i++)
			{
				UIRecipeMenuItem item = UnityEngine.Object.Instantiate(uIRecipeMenuItem, uIRecipeMenuItem.transform.parent);
				list2.Add(item);
			}
		}
		foreach (UIRecipeMenuItem item2 in list2)
		{
			item2.SetObActive(active: false);
		}
		for (int j = 0; j < list.Count; j++)
		{
			string _rec = list[j];
			list2[j].InitRecipeMenuItem_factory(_rec);
			list2[j].SetButton(delegate
			{
				_factory.SetStoredRecipe(_rec);
				Show(target: false);
			});
			if (_rec != "")
			{
				list2[j].SetHoverRecipe(_rec);
			}
			list2[j].ResetOverlays();
			if (_rec == _factory.GetStoredRecipe())
			{
				list2[j].AddOverlay(OverlayTypes.SELECTED);
			}
			list2[j].SetObActive(active: true);
		}
	}

	public void SetRecipes(Unlocker _unlocker, Action on_select)
	{
		List<string> list = new List<string>();
		list.AddRange(_unlocker.EAvailableBiomeReveals());
		rtRegular.SetObActive(list.Count <= scrollViewItemCount);
		rtScrollView.SetObActive(list.Count > scrollViewItemCount);
		UIRecipeMenuItem uIRecipeMenuItem = ((list.Count > scrollViewItemCount) ? itemPrefab_scrollView : itemPrefab_regular);
		List<UIRecipeMenuItem> list2 = ((list.Count > scrollViewItemCount) ? spawnedItems_scrollView : spawnedItems_regular);
		uIRecipeMenuItem.SetObActive(active: false);
		if (list2.Count < list.Count)
		{
			int num = list.Count - list2.Count;
			for (int i = 0; i < num; i++)
			{
				UIRecipeMenuItem item = UnityEngine.Object.Instantiate(uIRecipeMenuItem, uIRecipeMenuItem.transform.parent);
				list2.Add(item);
			}
		}
		foreach (UIRecipeMenuItem item2 in list2)
		{
			item2.SetObActive(active: false);
		}
		for (int j = 0; j < list.Count; j++)
		{
			string u = list[j];
			list2[j].InitRecipeMenuItem_unlocker(u);
			list2[j].SetButton(delegate
			{
				_unlocker.SetUnlock(u);
				on_select();
				Show(target: false);
			});
			list2[j].ResetOverlays();
			if (u == _unlocker.GetCurrentUnlock().code)
			{
				list2[j].AddOverlay(OverlayTypes.SELECTED);
			}
			list2[j].SetObActive(active: true);
		}
	}
}
