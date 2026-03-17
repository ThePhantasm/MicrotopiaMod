using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITextImageButton : UIBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	protected TextMeshProUGUI lbText;

	[SerializeField]
	protected List<TextMeshProUGUI> listExtraTexts = new List<TextMeshProUGUI>();

	[SerializeField]
	protected Image imImage;

	[SerializeField]
	protected UIButton btButton;

	public List<UITextImageButton_Overlay> overlays = new List<UITextImageButton_Overlay>();

	private Action onPointerEnter;

	private Action onPointerExit;

	private List<string> listExtraStrings = new List<string>();

	public virtual void Init()
	{
		listExtraStrings = new List<string>();
		foreach (TextMeshProUGUI listExtraText in listExtraTexts)
		{
			_ = listExtraText;
			listExtraStrings.Add("");
		}
		SetExtraText();
		ResetOverlays();
	}

	public virtual void Init(string _text)
	{
		Init();
		SetText(_text);
	}

	public virtual void Init(Action on_click)
	{
		Init();
		SetButton(on_click);
	}

	public virtual void Init(string _text, Action on_click)
	{
		Init();
		SetText(_text);
		SetButton(on_click);
	}

	public void SetText(string txt)
	{
		if (lbText != null)
		{
			lbText.text = txt;
		}
	}

	public void SetTextColor(Color col)
	{
		if (lbText != null)
		{
			lbText.color = col;
		}
	}

	public string GetText()
	{
		if (lbText == null)
		{
			return "NO_TEXT";
		}
		return lbText.text;
	}

	public void SetExtraText(int n = -1, string txt = "")
	{
		if (listExtraTexts.Count > 0 && n >= listExtraTexts.Count)
		{
			Debug.LogError(base.name + ": Trying to set extra text " + n + ", can't go higher than " + (listExtraTexts.Count - 1));
			return;
		}
		if (n != -1)
		{
			listExtraStrings[n] = txt;
			for (int i = 0; i < listExtraTexts.Count; i++)
			{
				if (listExtraStrings[i] == "")
				{
					listExtraTexts[i].SetObActive(active: false);
					continue;
				}
				listExtraTexts[i].text = listExtraStrings[i];
				listExtraTexts[i].SetObActive(active: true);
			}
			return;
		}
		foreach (TextMeshProUGUI listExtraText in listExtraTexts)
		{
			listExtraText.SetObActive(active: false);
		}
	}

	public void SetImage(Sprite sprite)
	{
		if (imImage != null)
		{
			if (sprite != null)
			{
				imImage.sprite = sprite;
				SetImageEnabled(target: true);
			}
			else
			{
				SetImageEnabled(target: false);
			}
		}
	}

	public void SetImageEnabled(bool target)
	{
		if (imImage != null)
		{
			imImage.enabled = target;
		}
	}

	public void SetImageColor(Color col)
	{
		if (imImage != null)
		{
			imImage.color = col;
		}
	}

	public void SetButton(Action on_click)
	{
		if (!(btButton != null))
		{
			return;
		}
		if (on_click != null)
		{
			btButton.button.interactable = true;
			if (btButton.button.image != null)
			{
				btButton.button.image.raycastTarget = true;
			}
			btButton.Init(delegate
			{
				on_click();
				if (UIHover.instance != null)
				{
					UIHover.instance.Outit(this);
				}
			});
		}
		else
		{
			btButton.button.interactable = false;
		}
	}

	public void ResetOverlays()
	{
		foreach (UITextImageButton_Overlay overlay in overlays)
		{
			overlay.SetActive(target: false);
		}
	}

	public void AddOverlay(OverlayTypes _type, string txt = "NULL")
	{
		foreach (UITextImageButton_Overlay overlay in overlays)
		{
			if (overlay.type == _type)
			{
				overlay.SetActive(target: true);
				if (txt != "NULL")
				{
					overlay.SetText(txt);
				}
			}
		}
	}

	public virtual void SetInteractable(bool target)
	{
		if (btButton != null)
		{
			btButton.interactable = target;
		}
	}

	public bool IsInteractable()
	{
		if (btButton != null)
		{
			return btButton.button.interactable;
		}
		return false;
	}

	public void SetHoverText(string _hover)
	{
		SetOnPointerEnter(delegate
		{
			UIHover.instance.Init(this);
			UIHover.instance.SetWidth();
			UIHover.instance.SetText(_hover);
		});
		SetOnPointerExit(delegate
		{
			UIHover.instance.Outit(this);
		});
	}

	public void SetHoverLocUI(string _code)
	{
		SetOnPointerEnter(delegate
		{
			string uI = Loc.GetUI(_code);
			if (uI != "")
			{
				UIHover.instance.Init(this);
				UIHover.instance.SetWidth();
				UIHover.instance.SetText(uI);
			}
		});
		SetOnPointerExit(delegate
		{
			UIHover.instance.Outit(this);
		});
	}

	public void SetHoverLocObjects(string _code)
	{
		SetOnPointerEnter(delegate
		{
			if (_code != "")
			{
				string text = Loc.GetObject(_code);
				if (text != "")
				{
					UIHover.instance.Init(this);
					UIHover.instance.SetWidth();
					UIHover.instance.SetText(text);
				}
			}
		});
		SetOnPointerExit(delegate
		{
			UIHover.instance.Outit(this);
		});
	}

	public void SetHoverInventory(Sprite icon, string title, string desc, string footer)
	{
		SetOnPointerEnter(delegate
		{
			UIHover.instance.Init(this);
			UIHover.instance.SetWidth(automatic: false, 350f);
			UIHover.instance.SetContentInventory(icon, title, desc, footer);
		});
		SetOnPointerExit(delegate
		{
			UIHover.instance.Outit(this);
		});
	}

	public void SetHoverRecipe(string recipe_code)
	{
		SetOnPointerEnter(delegate
		{
			UIHover.instance.Init(this);
			UIHover.instance.SetWidth();
			UIHover.instance.SetContentRecipe(recipe_code);
		});
		SetOnPointerExit(delegate
		{
			UIHover.instance.Outit(this);
		});
	}

	public void SetOnPointerEnter(Action action)
	{
		onPointerEnter = action;
	}

	public void SetOnPointerExit(Action action)
	{
		onPointerExit = action;
	}

	public virtual void OnPointerEnter(PointerEventData event_data)
	{
		if (onPointerEnter != null)
		{
			onPointerEnter();
		}
	}

	public virtual void OnPointerExit(PointerEventData event_data)
	{
		if (onPointerExit != null)
		{
			onPointerExit();
		}
	}

	public void DoOnPointerEnter()
	{
		if (onPointerEnter != null)
		{
			onPointerEnter();
		}
	}

	public void DoOnPointerExit()
	{
		if (onPointerExit != null)
		{
			onPointerExit();
		}
	}

	public void SetRaycastTarget(bool target)
	{
		btButton.button.image.raycastTarget = target;
	}

	public void Click()
	{
		btButton.Click();
	}
}
