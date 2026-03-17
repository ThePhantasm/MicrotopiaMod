using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : KoroutineBehaviour, IPointerUpHandler, IEventSystemHandler
{
	[Header("UI Button")]
	public Button button;

	[NonSerialized]
	public Action onClick;

	private Image imControl;

	private Image imControlBG;

	private bool _interactable;

	public UISfx sfxClick = UISfx.MenuButtonClick;

	public bool interactable
	{
		get
		{
			return _interactable;
		}
		set
		{
			_interactable = value;
			if (button != null)
			{
				button.interactable = _interactable;
			}
			if (imControl != null)
			{
				imControl.enabled = _interactable;
				imControlBG.enabled = _interactable;
			}
		}
	}

	public UIButton Init(Action _onClick)
	{
		if (button != null)
		{
			Navigation navigation = button.navigation;
			navigation.mode = Navigation.Mode.None;
			button.navigation = navigation;
		}
		if (_onClick != null)
		{
			onClick = _onClick;
			if (button != null)
			{
				button.onClick.RemoveAllListeners();
				button.onClick.AddListener(delegate
				{
					AudioManager.PlayUI(sfxClick);
					onClick();
				});
			}
		}
		if (imControl != null)
		{
			imControl.SetObActive(active: false);
		}
		if (imControlBG != null)
		{
			imControlBG.SetObActive(active: false);
		}
		return this;
	}

	public void Click()
	{
		if (onClick != null)
		{
			onClick();
		}
	}

	private Image CreateImage(string name, Sprite sprite, Color color, float size)
	{
		GameObject obj = new GameObject(name, typeof(RectTransform));
		RectTransform component = obj.GetComponent<RectTransform>();
		component.SetParent(base.transform, worldPositionStays: false);
		Image image = obj.AddComponent<Image>();
		image.sprite = sprite;
		image.color = color;
		image.raycastTarget = false;
		component.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
		component.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		component.anchoredPosition = new Vector3(base.gameObject.GetComponent<RectTransform>().rect.width * -0.5f - 10f, 0f);
		return image;
	}

	public bool SetVisible(bool vis)
	{
		button.SetVisible(vis);
		return vis;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		TryClick(eventData);
	}

	public bool TryClick(PointerEventData eventData = null)
	{
		if (button != null)
		{
			return false;
		}
		onClick?.Invoke();
		eventData?.Use();
		return true;
	}
}
