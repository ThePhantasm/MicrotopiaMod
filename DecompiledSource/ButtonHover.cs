using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public GameObject hoverObject;

	public UISfx sfxHover = UISfx.MenuButtonHover;

	private float timeExit;

	private void Awake()
	{
		hoverObject.SetObActive(active: false);
		Image component = hoverObject.GetComponent<Image>();
		if (component != null)
		{
			component.raycastTarget = false;
		}
		timeExit = 0f;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (hoverObject.SetObActive(active: true))
		{
			AudioManager.PlayUI(sfxHover);
		}
		timeExit = 0f;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		timeExit = Time.time;
	}

	private void Update()
	{
		if (timeExit > 0f && Time.time > timeExit + 0.1f)
		{
			hoverObject.SetObActive(active: false);
			timeExit = 0f;
		}
	}

	private void OnDisable()
	{
		hoverObject.SetObActive(active: false);
	}
}
