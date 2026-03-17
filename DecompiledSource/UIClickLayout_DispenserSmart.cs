using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIClickLayout_DispenserSmart : UIClickLayout_Building
{
	[SerializeField]
	private TextMeshProUGUI lbIcons;

	[SerializeField]
	private UITextImageButton btIcons;

	[SerializeField]
	private GridLayoutGroup gridIcons;

	private UIItemMenu uiItemMenu;

	private List<UIIconItem> spawnedIcons = new List<UIIconItem>();

	public override void Clear()
	{
		base.Clear();
		if (uiItemMenu != null)
		{
			uiItemMenu.DoApply();
			UnityEngine.Object.Destroy(uiItemMenu.gameObject);
		}
		uiItemMenu = null;
	}

	public void SetIcons(string _txt, string hover_loc, List<PickupType> pickups, List<PickupState> allowed_states, Action on_apply)
	{
		lbIcons.text = _txt;
		btIcons.SetButton(delegate
		{
			if (uiItemMenu == null)
			{
				uiItemMenu = UIBaseSingleton.Get(UIItemMenu.instance);
			}
			uiItemMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiItemMenu.SetPosition(btIcons.rtBase.transform.position);
			uiItemMenu.InitPickupTypes(pickups, allowed_states, on_apply, default(PickupType));
			uiItemMenu.Show(target: true);
		});
		btIcons.SetHoverText(hover_loc);
	}

	public void SetIcons(string _txt, string hover_loc, List<PickupType> pickups, List<PickupType> available_types, Action on_apply)
	{
		lbIcons.text = _txt;
		btIcons.SetButton(delegate
		{
			if (uiItemMenu == null)
			{
				uiItemMenu = UIBaseSingleton.Get(UIItemMenu.instance);
			}
			uiItemMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiItemMenu.SetPosition(btIcons.rtBase.transform.position);
			uiItemMenu.InitPickupTypes(pickups, available_types, on_apply, default(PickupType));
			uiItemMenu.Show(target: true);
		});
		btIcons.SetHoverText(hover_loc);
	}

	public void UpdateIcons(List<PickupType> pickups)
	{
		int num;
		if (spawnedIcons.Count < pickups.Count)
		{
			num = pickups.Count - spawnedIcons.Count;
			for (int i = 0; i < num; i++)
			{
				UIIconItem component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem)), gridIcons.transform).GetComponent<UIIconItem>();
				spawnedIcons.Add(component);
			}
		}
		foreach (UIIconItem spawnedIcon in spawnedIcons)
		{
			spawnedIcon.SetObActive(active: false);
		}
		num = Mathf.Min(pickups.Count, 5);
		for (int j = 0; j < num; j++)
		{
			spawnedIcons[j].SetObActive(active: true);
			if (j < 4)
			{
				spawnedIcons[j].Init(pickups[j]);
			}
			else
			{
				spawnedIcons[j].Init("...");
			}
		}
		gridIcons.constraintCount = num;
	}
}
