using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIClickLayout_Unlocker : UIClickLayout_Building
{
	[Header("Unlocker")]
	[SerializeField]
	private RectTransform recipeMenuPos;

	[SerializeField]
	private TextMeshProUGUI lbWillUnlock;

	[SerializeField]
	private TextMeshProUGUI lbUnlockResult;

	[SerializeField]
	private TextMeshProUGUI lbStory;

	[SerializeField]
	private UITextImageButton btChange;

	[SerializeField]
	private UITextImageButton btDoUnlock;

	[SerializeField]
	private UITextImageButton btDoUnlock_disabled;

	[SerializeField]
	private UITextImageButton uiSprite;

	private UIRecipeMenu uiRecipeMenu;

	public override void Clear()
	{
		base.Clear();
		if (uiRecipeMenu != null)
		{
			UnityEngine.Object.Destroy(uiRecipeMenu.gameObject);
		}
		uiRecipeMenu = null;
	}

	public void SetUnlocker(Unlocker unlocker, Action on_unlock)
	{
		btDoUnlock.SetButton(on_unlock);
		if (unlocker.unlockerType == UnlockerType.IslandReveal)
		{
			btDoUnlock.SetText(Loc.GetUI("UNLOCKER_REVEAL"));
			btDoUnlock_disabled.SetText(Loc.GetUI("UNLOCKER_REVEAL"));
			btDoUnlock_disabled.SetHoverText(Loc.GetUI("UNLOCKER_REVEAL_NOT_YET"));
			lbStory.text = "";
		}
		else
		{
			btDoUnlock.SetText(Loc.GetUI("UNLOCKER_UNLOCK"));
			btDoUnlock_disabled.SetText(Loc.GetUI("UNLOCKER_UNLOCK"));
			btDoUnlock_disabled.SetHoverText(Loc.GetUI("UNLOCKER_UNLOCK_NOT_YET"));
			lbStory.text = Loc.GetObject("BUILD_ANCIENTTECH_DESC");
		}
		btDoUnlock.SetObActive(active: false);
		btDoUnlock_disabled.SetObActive(active: false);
		SetUnlockName(unlocker);
		SetChangeButton(unlocker);
	}

	public void SetUnlockButton(bool target)
	{
		btDoUnlock.SetObActive(target);
		btDoUnlock_disabled.SetObActive(!target);
	}

	private void SetUnlockName(Unlocker unlocker)
	{
		unlocker.GetUnlockInfo(out var verb, out var result, out var sprite);
		lbWillUnlock.text = verb;
		lbUnlockResult.text = result;
		if (sprite != null)
		{
			UnlockRecipeData current_unlock = unlocker.GetCurrentUnlock();
			uiSprite.SetImage(sprite);
			if (current_unlock.unlockBuildings.Count != 0)
			{
				BuildingData data = BuildingData.Get(current_unlock.unlockBuildings[0]);
				uiSprite.SetOnPointerEnter(delegate
				{
					UIHover.instance.Init(this);
					UIHover.instance.SetWidth(354f);
					UIHover.instance.SetHoverOrientation(HoverOrientation.BottomMid);
					UIHover.instance.SetTitle(data.GetTitle());
					UIHover.instance.SetTopMessage(Tech.GetUnlockMessage(TechType.BUILDING));
					UIHover.instance.SetText(data.GetDescription());
					List<AntCaste> list = new List<AntCaste>();
					List<PickupType> list2 = new List<PickupType>();
					foreach (string unlockBuilding in current_unlock.unlockBuildings)
					{
						foreach (PickupCost baseCost in BuildingData.Get(unlockBuilding).baseCosts)
						{
							if (!list2.Contains(baseCost.type))
							{
								list2.Add(baseCost.type);
							}
						}
					}
					if (list.Count > 0 || list2.Count > 0)
					{
						string uI = Loc.GetUI("TECHTREE_REQUIRED_TO_BUILD");
						UIHover.instance.SetRequired(uI, list, list2);
					}
				});
				uiSprite.SetOnPointerExit(delegate
				{
					UIHover.instance.Outit(this);
				});
			}
			else if (current_unlock.unlockRecipes.Count != 0)
			{
				FactoryRecipeData data2 = FactoryRecipeData.Get(current_unlock.unlockRecipes[0]);
				uiSprite.SetOnPointerEnter(delegate
				{
					UIHover.instance.Init(this);
					UIHover.instance.SetWidth(354f);
					UIHover.instance.SetHoverOrientation(HoverOrientation.BottomMid);
					UIHover.instance.SetTitle(data2.GetTitle());
					if (data2.productPickups.Count > 0)
					{
						UIHover.instance.SetTopMessage(Tech.GetUnlockMessage(TechType.PICKUP));
						UIHover.instance.SetText(PickupData.Get(data2.productPickups[0].type).GetDescription());
					}
					else if (data2.productAnts.Count > 0)
					{
						UIHover.instance.SetTopMessage(Tech.GetUnlockMessage(TechType.ANT));
						UIHover.instance.SetText(AntCasteData.Get(data2.productAnts[0].type).GetDescription());
					}
					List<AntCaste> list = new List<AntCaste>();
					List<PickupType> list2 = new List<PickupType>();
					foreach (string unlockRecipe in current_unlock.unlockRecipes)
					{
						FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(unlockRecipe);
						foreach (PickupCost item in factoryRecipeData.costsPickup)
						{
							if (!list2.Contains(item.type))
							{
								list2.Add(item.type);
							}
						}
						foreach (AntCasteAmount item2 in factoryRecipeData.costsAnt)
						{
							if (!list.Contains(item2.type))
							{
								list.Add(item2.type);
							}
						}
					}
					if (list.Count > 0 || list2.Count > 0)
					{
						string uI = Loc.GetUI("TECHTREE_REQUIRED_TO_CRAFT");
						UIHover.instance.SetRequired(uI, list, list2);
					}
				});
				uiSprite.SetOnPointerExit(delegate
				{
					UIHover.instance.Outit(this);
				});
			}
			else
			{
				uiSprite.SetOnPointerEnter(delegate
				{
				});
				uiSprite.SetOnPointerExit(delegate
				{
				});
			}
			uiSprite.SetObActive(active: true);
		}
		else
		{
			uiSprite.SetObActive(active: false);
		}
	}

	public void SetChangeButton(Unlocker unlocker)
	{
		if (unlocker.unlockerType != UnlockerType.IslandReveal || unlocker.GetAvailableBiomeRevealsCount() <= 1)
		{
			btChange.SetObActive(active: false);
			return;
		}
		btChange.SetObActive(active: true);
		btChange.SetButton(delegate
		{
			if (uiRecipeMenu == null)
			{
				uiRecipeMenu = UIBaseSingleton.Get(UIRecipeMenu.instance);
			}
			uiRecipeMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiRecipeMenu.SetPosition(recipeMenuPos.position);
			uiRecipeMenu.SetRecipes(unlocker, delegate
			{
				SetUnlockName(unlocker);
			});
			uiRecipeMenu.Show(target: true);
		});
	}
}
