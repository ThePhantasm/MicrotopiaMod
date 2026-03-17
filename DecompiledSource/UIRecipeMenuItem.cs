using UnityEngine;

public class UIRecipeMenuItem : UITextImageButton
{
	[SerializeField]
	private UIIconItem uiIcon;

	public void InitRecipeMenuItem_factory(string recipe)
	{
		if (recipe == "")
		{
			uiIcon.SetObActive(active: false);
			lbText.text = Loc.GetUI("GENERIC_NONE");
			return;
		}
		FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(recipe);
		uiIcon.SetObActive(active: true);
		uiIcon.Init(factoryRecipeData);
		lbText.text = factoryRecipeData.GetTitle();
	}

	public void InitRecipeMenuItem_unlocker(string unlock)
	{
		if (unlock == "")
		{
			uiIcon.SetObActive(active: false);
			lbText.text = Loc.GetUI("GENERIC_NONE");
		}
		else
		{
			UnlockRecipeData unlockRecipeData = UnlockRecipeData.Get(unlock);
			uiIcon.SetObActive(active: false);
			lbText.text = unlockRecipeData.GetTitle();
		}
	}
}
