using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoldButton : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
{
	private bool buttonActive;

	private float timeClick;

	private Action<bool> onClick;

	private const float DELAY_FIRST = 0.5f;

	private const float DELAY_MORE = 0.05f;

	public void Init(Action<bool> on_click)
	{
		SetActive(active: false, on_init: true);
		onClick = on_click;
	}

	public void OnPointerDown(PointerEventData event_data)
	{
		SetActive(active: true);
	}

	public void OnPointerUp(PointerEventData event_data)
	{
		SetActive(active: false);
	}

	private void OnDestroy()
	{
		SetActive(active: false);
	}

	private void OnDisable()
	{
		SetActive(active: false);
	}

	private void SetActive(bool active, bool on_init = false)
	{
		if (buttonActive != active || on_init)
		{
			buttonActive = active;
			if (active)
			{
				onClick(obj: true);
				timeClick = Time.time + 0.5f;
			}
			else
			{
				timeClick = float.MaxValue;
			}
		}
	}

	private void Update()
	{
		if (buttonActive && Time.time > timeClick)
		{
			onClick(obj: false);
			timeClick = Time.time + 0.05f;
		}
	}
}
