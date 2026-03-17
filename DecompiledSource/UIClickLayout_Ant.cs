using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIClickLayout_Ant : UIClickLayout
{
	[Serializable]
	public class StatusEffectUI
	{
		public StatusEffect effect;

		public UITextImageButton obEffect;
	}

	[Header("Ant")]
	[SerializeField]
	private RectTransform rtCapabilities;

	[SerializeField]
	private RectTransform rtLinkInfo;

	[SerializeField]
	private RectTransform rtCarrying;

	[SerializeField]
	private TextMeshProUGUI lbCapabilities;

	[SerializeField]
	private TextMeshProUGUI lbEnergyName;

	[SerializeField]
	private TextMeshProUGUI lbEnergyAmount;

	[SerializeField]
	private TextMeshProUGUI lbHealthName;

	[SerializeField]
	private TextMeshProUGUI lbHealthAmount;

	[SerializeField]
	private TextMeshProUGUI lbHealthStatus;

	[SerializeField]
	private TextMeshProUGUI lbLinkInfo;

	[SerializeField]
	private TextMeshProUGUI lbCarrying;

	[SerializeField]
	private TextMeshProUGUI lbCarryingPickup;

	[SerializeField]
	private UILoadingBar uiEnergyBar;

	[SerializeField]
	private UILoadingBar uiHealthBar;

	[SerializeField]
	private UILoadingBar uiRadDeathBar;

	[SerializeField]
	private UITextImageButton prefabExchangeType;

	[SerializeField]
	private UIIconItem uiIconCarrying;

	[SerializeField]
	private List<StatusEffectUI> listStatusEffects = new List<StatusEffectUI>();

	private List<UITextImageButton> spawnedExchangeItems = new List<UITextImageButton>();

	public override void Init()
	{
		base.Init();
		prefabExchangeType.SetObActive(active: false);
	}

	public void SetLinkInfo(string txt)
	{
		rtLinkInfo.SetObActive(txt != "");
		lbLinkInfo.text = txt;
	}

	public void SetCapabilities(string desc)
	{
		rtCapabilities.SetObActive(active: true);
		lbCapabilities.text = desc;
		foreach (UITextImageButton spawnedExchangeItem in spawnedExchangeItems)
		{
			spawnedExchangeItem.SetObActive(active: false);
		}
	}

	public void AddCapability(TrailType _type)
	{
		AssetLinks.standard.GetTrailIcon(_type, out var col);
		AddCapability(TrailData.Get(_type).GetTitle(), col);
	}

	public void AddCapability(string s, Color col)
	{
		UITextImageButton uITextImageButton = null;
		foreach (UITextImageButton spawnedExchangeItem in spawnedExchangeItems)
		{
			if (!spawnedExchangeItem.isActiveAndEnabled)
			{
				uITextImageButton = spawnedExchangeItem;
				break;
			}
		}
		if (uITextImageButton == null)
		{
			uITextImageButton = UnityEngine.Object.Instantiate(prefabExchangeType, prefabExchangeType.transform.parent).GetComponent<UITextImageButton>();
			spawnedExchangeItems.Add(uITextImageButton);
		}
		uITextImageButton.SetObActive(active: true);
		uITextImageButton.SetText(s);
		uITextImageButton.SetImageColor(col);
	}

	public void HideCapabilities()
	{
		rtCapabilities.SetObActive(active: false);
	}

	public void SetEnergy(bool target, string name = "")
	{
		uiEnergyBar.SetObActive(target);
		lbEnergyName.text = name;
	}

	public void UpdateEnergy(string amount, float val)
	{
		lbEnergyAmount.text = amount;
		uiEnergyBar.SetBar(val);
	}

	public void SetHealth(string name)
	{
		lbHealthName.text = name;
		uiRadDeathBar.SetObActive(active: false);
	}

	public void UpdateHealth(string amount, float val)
	{
		lbHealthAmount.text = amount;
		uiHealthBar.SetBar(val);
	}

	public void UpdateRadDeath(float val)
	{
		uiRadDeathBar.SetObActive(active: true);
		uiRadDeathBar.SetBar(val);
	}

	public void UpdateStatusEffects(string status_text)
	{
		lbHealthStatus.text = status_text;
	}

	public void SetEffects()
	{
		foreach (StatusEffectUI listStatusEffect in listStatusEffects)
		{
			StatusEffectData statusEffectData = StatusEffectData.Get(listStatusEffect.effect);
			listStatusEffect.obEffect.SetText(statusEffectData.GetTitle());
			listStatusEffect.obEffect.SetHoverText(statusEffectData.GetHover());
		}
	}

	public void UpdateEffects(List<StatusEffect> _effects)
	{
		foreach (StatusEffectUI listStatusEffect in listStatusEffects)
		{
			listStatusEffect.obEffect.SetObActive(_effects.Contains(listStatusEffect.effect));
		}
	}

	public void UpdateEffectTitle(StatusEffect _effect, string _title)
	{
		foreach (StatusEffectUI listStatusEffect in listStatusEffects)
		{
			if (listStatusEffect.effect == _effect)
			{
				listStatusEffect.obEffect.SetText(_title);
			}
		}
	}

	public void SetCarrying(PickupType _type)
	{
		if (_type == PickupType.NONE)
		{
			rtCarrying.SetObActive(active: false);
			return;
		}
		rtCarrying.SetObActive(active: true);
		lbCarrying.text = Loc.GetUI("ANT_CURRENTLYCARRYING");
		PickupData pickupData = PickupData.Get(_type);
		lbCarryingPickup.text = pickupData.GetTitle();
		uiIconCarrying.SetImage(pickupData.GetIcon());
	}
}
