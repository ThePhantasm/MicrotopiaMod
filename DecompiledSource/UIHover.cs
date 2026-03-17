using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHover : UIBaseSingleton
{
	public static UIHover instance;

	[SerializeField]
	private RectTransform rtContentBase;

	[SerializeField]
	private RectTransform rtContentInventory;

	[SerializeField]
	private RectTransform rtContentRecipe;

	[SerializeField]
	private ContentSizeFitter csfBase;

	[SerializeField]
	private ContentSizeFitter csfInventory;

	[SerializeField]
	private ContentSizeFitter csfRecipe;

	[Header("Content Base")]
	[SerializeField]
	private RectTransform rtTitle;

	[SerializeField]
	private RectTransform rtText;

	[SerializeField]
	private RectTransform rtText2;

	[SerializeField]
	private RectTransform rtFooter;

	[SerializeField]
	private RectTransform rtInventory;

	[SerializeField]
	private RectTransform rtChecklist;

	[SerializeField]
	private RectTransform rtOutline;

	[SerializeField]
	private RectTransform rtCreatedAt;

	[SerializeField]
	private RectTransform rtTopMessage;

	[SerializeField]
	private RectTransform rtRequired;

	[SerializeField]
	private TextMeshProUGUI lbTitle;

	[SerializeField]
	private TextMeshProUGUI lbText;

	[SerializeField]
	private TextMeshProUGUI lbText2;

	[SerializeField]
	private TextMeshProUGUI lbTextFooter;

	[SerializeField]
	private TextMeshProUGUI lbInventory;

	[SerializeField]
	private TextMeshProUGUI lbStatus;

	[SerializeField]
	private TextMeshProUGUI lbCosts;

	[SerializeField]
	private TextMeshProUGUI lbCreatedAtBuilding;

	[SerializeField]
	private TextMeshProUGUI lbTopMessage;

	[SerializeField]
	private TextMeshProUGUI lbRequired;

	[SerializeField]
	private Image imCreatedAtBuilding;

	[SerializeField]
	private GridLayoutGroup gridInventory;

	[SerializeField]
	private UITechCurrency prefabInventorPoints;

	[SerializeField]
	private UIIconItem prefabRequiredIcon;

	[Header("Content Inventory")]
	[SerializeField]
	private Image imIcon;

	[SerializeField]
	private TextMeshProUGUI lbInventoryTitle;

	[SerializeField]
	private TextMeshProUGUI lbInventoryDesc;

	[SerializeField]
	private TextMeshProUGUI lbInventoryFooter;

	[Header("Content Recipe")]
	[SerializeField]
	private Image imRecipe;

	[SerializeField]
	private TextMeshProUGUI lbRecipeTitle;

	[SerializeField]
	private TextMeshProUGUI lbRecipeTime;

	[SerializeField]
	private UIIconItem prefabRecipeIngredient_noText;

	[SerializeField]
	private UIIconItem prefabRecipeIngredient_text;

	[Space(10f)]
	private Vector2 offset = new Vector2(10f, -10f);

	[SerializeField]
	private List<HoverOffset> hoverOffsets = new List<HoverOffset>();

	private UIBase currentHoveringUI;

	private ClickableObject currentHoveringOb;

	private List<UIIconItem> spawnedInventory = new List<UIIconItem>();

	private List<UITechCurrency> spawnedChecklist = new List<UITechCurrency>();

	private List<UIIconItem> spawnedRequiredIcons = new List<UIIconItem>();

	private List<UIIconItem> spawnedRecipeIngredients_noText = new List<UIIconItem>();

	private List<UIIconItem> spawnedRecipeIngredients_text = new List<UIIconItem>();

	private HoverOrientation hoverOrientation = HoverOrientation.TopLeft;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Init(UIBase _ui)
	{
		ClearHover();
		currentHoveringUI = _ui;
		currentHoveringOb = null;
		UpdateHover();
		this.SetObActive(active: true);
		prefabInventorPoints.SetObActive(active: false);
		base.transform.SetAsLastSibling();
	}

	public void Init(ClickableObject _ob)
	{
		ClearHover();
		currentHoveringOb = _ob;
		currentHoveringUI = null;
		UpdateHover();
		this.SetObActive(active: true);
		prefabInventorPoints.SetObActive(active: false);
	}

	public void Outit()
	{
		currentHoveringUI = null;
		this.SetObActive(active: false);
	}

	public void Outit(UIBase _ui)
	{
		if (currentHoveringUI == _ui)
		{
			currentHoveringUI = null;
			this.SetObActive(active: false);
		}
	}

	public void Outit(ClickableObject _ob)
	{
		if (currentHoveringOb == _ob)
		{
			currentHoveringOb = null;
			this.SetObActive(active: false);
		}
	}

	public void ClearHover()
	{
		rtContentBase.SetObActive(active: true);
		rtContentInventory.SetObActive(active: false);
		rtContentRecipe.SetObActive(active: false);
		SetWidth();
		SetOutline(target: false);
		currentHoveringUI = null;
		rtTitle.SetObActive(active: false);
		rtText.SetObActive(active: false);
		rtText2.SetObActive(active: false);
		rtFooter.SetObActive(active: false);
		rtInventory.SetObActive(active: false);
		rtChecklist.SetObActive(active: false);
		rtRequired.SetObActive(active: false);
		rtCreatedAt.SetObActive(active: false);
		rtTopMessage.SetObActive(active: false);
		lbStatus.text = "";
		SetHoverOrientation(HoverOrientation.TopLeft);
	}

	public void SetHoverOrientation(HoverOrientation ho)
	{
		hoverOrientation = ho;
		switch (hoverOrientation)
		{
		case HoverOrientation.TopLeft:
			rtBase.pivot = new Vector2(0f, 1f);
			break;
		case HoverOrientation.BottomMid:
			rtBase.pivot = new Vector2(0.5f, 0f);
			break;
		}
		foreach (HoverOffset hoverOffset in hoverOffsets)
		{
			if (hoverOffset.orientation == hoverOrientation)
			{
				offset = hoverOffset.offset;
				break;
			}
		}
	}

	public void UpdateHover()
	{
		if (!(currentHoveringUI != null) && !(currentHoveringOb != null))
		{
			return;
		}
		if (currentHoveringUI != null && !currentHoveringUI.isActiveAndEnabled)
		{
			Outit(currentHoveringUI);
			return;
		}
		if (currentHoveringOb != null && !currentHoveringOb.isActiveAndEnabled)
		{
			Outit(currentHoveringOb);
			return;
		}
		float scale = UIGlobal.GetScale();
		float num = InputManager.mousePosition.x + offset.x;
		float num2 = InputManager.mousePosition.y + offset.y;
		switch (hoverOrientation)
		{
		case HoverOrientation.TopLeft:
			num = Mathf.Clamp(num, 0f, (float)Screen.width - rtBase.sizeDelta.x * scale);
			num2 = Mathf.Clamp(num2, rtBase.sizeDelta.y * scale, (float)Screen.height * scale);
			break;
		case HoverOrientation.BottomMid:
			num = Mathf.Clamp(num, rtBase.sizeDelta.x * scale / 2f, (float)Screen.width * scale - rtBase.sizeDelta.x * scale / 2f);
			num2 = Mathf.Clamp(num2, 0f, (float)Screen.height - rtBase.sizeDelta.y * scale);
			break;
		}
		rtBase.position = new Vector2(num, num2);
	}

	public void SetWidth(float width)
	{
		SetWidth(automatic: false, width);
	}

	public void SetWidth(bool automatic = true, float width = -1f)
	{
		csfBase.horizontalFit = (automatic ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained);
		if (!automatic && width != -1f)
		{
			rtBase.sizeDelta = new Vector2(width, rtBase.sizeDelta.y);
		}
	}

	public void SetTitle(string txt)
	{
		rtTitle.SetObActive(active: true);
		lbTitle.Set(txt);
	}

	public void SetTopMessage(string txt)
	{
		rtTopMessage.SetObActive(active: true);
		lbTopMessage.Set(txt);
	}

	public void SetText(string txt)
	{
		SetText(txt, Color.white);
	}

	public void SetText(string txt, Color col)
	{
		rtText.SetObActive(active: true);
		lbText.Set(txt);
		lbText.color = col;
	}

	public void SetText2(string txt)
	{
		SetText2(txt, Color.white);
	}

	public void SetText2(string txt, Color col)
	{
		rtText2.SetObActive(active: true);
		lbText2.Set(txt);
		lbText2.color = col;
	}

	public void SetTextFooter(string txt)
	{
		SetTextFooter(txt, Color.white);
	}

	public void SetTextFooter(string txt, Color col)
	{
		rtFooter.SetObActive(active: true);
		lbTextFooter.Set(txt);
		lbTextFooter.color = col;
	}

	public void SetInventory(string _title, Dictionary<PickupType, int> _pickups)
	{
		Dictionary<PickupType, string> dictionary = new Dictionary<PickupType, string>();
		foreach (KeyValuePair<PickupType, int> _pickup in _pickups)
		{
			if (_pickup.Value > 0)
			{
				dictionary.Add(_pickup.Key, "x " + _pickup.Value);
			}
		}
		if (dictionary.Count > 0)
		{
			SetInventory(_title, dictionary);
		}
	}

	public void SetInventory(string txt, Dictionary<PickupType, string> _pickups)
	{
		rtInventory.SetObActive(active: true);
		lbInventory.Set(txt);
		int num = Mathf.Max(_pickups.Count, 1);
		int num2;
		if (spawnedInventory.Count < num)
		{
			num2 = num - spawnedInventory.Count;
			for (int i = 0; i < num2; i++)
			{
				UIIconItem component = Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem)), gridInventory.transform).GetComponent<UIIconItem>();
				component.SetRaycastTarget(target: false);
				spawnedInventory.Add(component);
			}
		}
		foreach (UIIconItem item in spawnedInventory)
		{
			item.SetObActive(active: false);
		}
		gridInventory.cellSize = new Vector2(100f, 50f);
		num2 = 0;
		foreach (KeyValuePair<PickupType, string> _pickup in _pickups)
		{
			if (!(_pickup.Value == ""))
			{
				spawnedInventory[num2].SetObActive(active: true);
				spawnedInventory[num2].Init(_pickup.Key);
				spawnedInventory[num2].SetExtraText(0, _pickup.Value);
				num2++;
			}
		}
	}

	public void SetChecklist(List<InventorPointsCost> costs)
	{
		rtChecklist.SetObActive(active: true);
		lbCosts.text = Loc.GetUI("TECHTREE_COSTTOUNLOCK");
		if (spawnedChecklist.Count < costs.Count)
		{
			int num = costs.Count - spawnedChecklist.Count;
			for (int i = 0; i < num; i++)
			{
				UITechCurrency component = Object.Instantiate(prefabInventorPoints, prefabInventorPoints.transform.parent).GetComponent<UITechCurrency>();
				component.SetObActive(active: false);
				spawnedChecklist.Add(component);
			}
		}
		foreach (UITechCurrency item in spawnedChecklist)
		{
			item.SetObActive(active: false);
		}
		for (int j = 0; j < costs.Count; j++)
		{
			spawnedChecklist[j].Init(costs[j].type, costs[j].amount.ToString());
			spawnedChecklist[j].SetObActive(active: true);
		}
	}

	public void SetChecklist(string txt, List<string> checks)
	{
		rtChecklist.SetObActive(active: true);
		lbCosts.Set(txt);
		if (spawnedChecklist.Count < checks.Count)
		{
			int num = checks.Count - spawnedChecklist.Count;
			for (int i = 0; i < num; i++)
			{
				UITechCurrency component = Object.Instantiate(prefabInventorPoints, prefabInventorPoints.transform.parent).GetComponent<UITechCurrency>();
				component.SetObActive(active: false);
				spawnedChecklist.Add(component);
			}
		}
		foreach (UITechCurrency item in spawnedChecklist)
		{
			item.SetObActive(active: false);
		}
		for (int j = 0; j < checks.Count; j++)
		{
			spawnedChecklist[j].Init(checks[j]);
			spawnedChecklist[j].SetImage(null);
			spawnedChecklist[j].SetObActive(active: true);
		}
	}

	public void SetCreatedAt(string title, Sprite img)
	{
		rtCreatedAt.SetObActive(active: true);
		lbCreatedAtBuilding.Set(title);
		imCreatedAtBuilding.sprite = img;
	}

	public void SetRequired(string txt, List<AntCaste> req_ants, List<PickupType> req_pickups)
	{
		rtRequired.SetObActive(active: true);
		lbRequired.text = txt;
		int num = req_ants.Count + req_pickups.Count;
		if (spawnedRequiredIcons.Count < num)
		{
			prefabRequiredIcon.SetObActive(active: false);
			int num2 = num - spawnedRequiredIcons.Count;
			for (int i = 0; i < num2; i++)
			{
				UIIconItem item = Object.Instantiate(prefabRequiredIcon, prefabRequiredIcon.transform.parent);
				spawnedRequiredIcons.Add(item);
			}
		}
		foreach (UIIconItem spawnedRequiredIcon in spawnedRequiredIcons)
		{
			spawnedRequiredIcon.SetObActive(active: false);
		}
		for (int j = 0; j < req_ants.Count; j++)
		{
			spawnedRequiredIcons[j].SetObActive(active: true);
			spawnedRequiredIcons[j].SetImage(AntCasteData.Get(req_ants[j]).GetIcon());
		}
		for (int k = 0; k < req_pickups.Count; k++)
		{
			int index = req_ants.Count + k;
			spawnedRequiredIcons[index].SetObActive(active: true);
			spawnedRequiredIcons[index].SetImage(PickupData.Get(req_pickups[k]).GetIcon());
		}
	}

	public void SetLbStatus(string txt, Color col)
	{
		lbStatus.Set(txt);
		lbStatus.color = col;
	}

	public void SetOutline(bool target)
	{
		rtOutline.SetObActive(target);
	}

	public void SetContentInventory(Sprite icon, string title, string desc, string footer)
	{
		rtContentBase.SetObActive(active: false);
		rtContentInventory.SetObActive(active: true);
		imIcon.sprite = icon;
		lbInventoryTitle.Set(title);
		lbInventoryDesc.Set(desc);
		lbInventoryFooter.Set(footer);
	}

	public void SetContentRecipe(string code)
	{
		rtContentBase.SetObActive(active: false);
		rtContentRecipe.SetObActive(active: true);
		FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get(code);
		imRecipe.sprite = factoryRecipeData.GetIcon();
		lbRecipeTitle.text = factoryRecipeData.GetTitle();
		lbRecipeTime.text = Loc.GetUI("RECIPE_DURATION", factoryRecipeData.processTime.Unit(PhysUnit.TIME));
		int num = factoryRecipeData.costsAnt.Count;
		int num2 = 0;
		if (factoryRecipeData.costsAnt.Count <= 2)
		{
			for (int i = 0; i < factoryRecipeData.costsAnt.Count; i++)
			{
				num += factoryRecipeData.costsAnt[i].intValue;
			}
		}
		else
		{
			num2 += factoryRecipeData.costsAnt.Count;
		}
		foreach (PickupCost item3 in factoryRecipeData.costsPickup)
		{
			if (item3.intValue == 1)
			{
				num++;
			}
			else
			{
				num2++;
			}
		}
		if (spawnedRecipeIngredients_noText.Count < num)
		{
			prefabRecipeIngredient_noText.SetObActive(active: false);
			int num3 = num - spawnedRecipeIngredients_noText.Count;
			for (int j = 0; j < num3; j++)
			{
				UIIconItem item = Object.Instantiate(prefabRecipeIngredient_noText, prefabRecipeIngredient_noText.transform.parent);
				spawnedRecipeIngredients_noText.Add(item);
			}
		}
		if (spawnedRecipeIngredients_text.Count < num2)
		{
			prefabRecipeIngredient_text.SetObActive(active: false);
			int num4 = num2 - spawnedRecipeIngredients_text.Count;
			for (int k = 0; k < num4; k++)
			{
				UIIconItem item2 = Object.Instantiate(prefabRecipeIngredient_text, prefabRecipeIngredient_text.transform.parent);
				spawnedRecipeIngredients_text.Add(item2);
			}
		}
		foreach (UIIconItem item4 in spawnedRecipeIngredients_noText)
		{
			item4.SetObActive(active: false);
		}
		foreach (UIIconItem item5 in spawnedRecipeIngredients_text)
		{
			item5.SetObActive(active: false);
		}
		int num5 = 0;
		int num6 = 0;
		for (int l = 0; l < factoryRecipeData.costsAnt.Count; l++)
		{
			if (factoryRecipeData.costsAnt.Count <= 2)
			{
				for (int m = 0; m < factoryRecipeData.costsAnt[l].intValue; m++)
				{
					spawnedRecipeIngredients_noText[num5].SetObActive(active: true);
					spawnedRecipeIngredients_noText[num5].Init();
					spawnedRecipeIngredients_noText[num5].SetImage(AntCasteData.Get(factoryRecipeData.costsAnt[l].type).GetIcon());
					num5++;
				}
			}
			else
			{
				spawnedRecipeIngredients_text[num6].SetObActive(active: true);
				spawnedRecipeIngredients_text[num6].Init();
				spawnedRecipeIngredients_text[num6].SetImage(AntCasteData.Get(factoryRecipeData.costsAnt[l].type).GetIcon());
				spawnedRecipeIngredients_text[num6].SetExtraText(0, "x" + factoryRecipeData.costsAnt[l].intValue);
				num6++;
			}
		}
		for (int n = 0; n < factoryRecipeData.costsPickup.Count; n++)
		{
			if (factoryRecipeData.costsPickup[n].intValue == 1)
			{
				spawnedRecipeIngredients_noText[num5].SetObActive(active: true);
				spawnedRecipeIngredients_noText[num5].Init();
				spawnedRecipeIngredients_noText[num5].SetImage(PickupData.Get(factoryRecipeData.costsPickup[n].type).GetIcon());
				num5++;
			}
			else
			{
				spawnedRecipeIngredients_text[num6].SetObActive(active: true);
				spawnedRecipeIngredients_text[num6].Init();
				spawnedRecipeIngredients_text[num6].SetImage(PickupData.Get(factoryRecipeData.costsPickup[n].type).GetIcon());
				spawnedRecipeIngredients_text[num6].SetExtraText(0, "x" + factoryRecipeData.costsPickup[n].intValue);
				num6++;
			}
		}
	}
}
