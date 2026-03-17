using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRecipe : UIBase
{
	[Header("UIRecipe")]
	[SerializeField]
	private RectTransform rtRecipeMenuPos;

	[SerializeField]
	private RectTransform rtRecipeIngredients;

	[SerializeField]
	private TextMeshProUGUI lbRecipe;

	[SerializeField]
	private TextMeshProUGUI lbStatus;

	[SerializeField]
	private TextMeshProUGUI lbPercentage;

	[SerializeField]
	private Slider slPercentage;

	[SerializeField]
	private UIIconItem iconRecipe;

	[SerializeField]
	private UIButton btChangeRecipe;

	[SerializeField]
	private UIIconList listIngredients;

	private bool showIngredients;

	public void SetRecipe(Action open_recipe_menu, bool show_ingredients)
	{
		btChangeRecipe.Init(delegate
		{
			open_recipe_menu();
		});
		showIngredients = show_ingredients;
		rtRecipeIngredients.SetObActive(showIngredients);
	}

	public void UpdateRecipe(Factory factory)
	{
		factory.GatherRecipeProgress(iconRecipe, showIngredients, out var ant_icons, out var pickup_icons, out var text, out var status, out var progress_text, out var progress_value);
		lbRecipe.text = text;
		if (showIngredients)
		{
			listIngredients.SpawnList(ant_icons, pickup_icons, Loc.GetUI("GENERIC_EMPTY"));
		}
		lbStatus.SetObActive(!string.IsNullOrEmpty(status));
		lbStatus.text = status;
		slPercentage.SetObActive(!string.IsNullOrEmpty(progress_text));
		slPercentage.value = progress_value;
		lbPercentage.SetObActive(!string.IsNullOrEmpty(progress_text));
		lbPercentage.text = progress_text;
	}

	public void SetChangeRecipeAllowed(bool allowed)
	{
		if (btChangeRecipe.SetObActive(allowed) && !allowed && UIRecipeMenu.instance != null)
		{
			UIRecipeMenu.instance.Show(target: false);
		}
	}

	public Vector3 GetRecipeMenuPos()
	{
		return rtRecipeMenuPos.position;
	}
}
