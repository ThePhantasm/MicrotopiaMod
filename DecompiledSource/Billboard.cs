using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Billboard : UIBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public Canvas canvas;

	public GraphicRaycaster graphicRaycaster;

	public Animator anim;

	public List<BillboardScreen> screens = new List<BillboardScreen>();

	private BillboardScreen currentScreen;

	private string codeDesc;

	private Color colDesc;

	public void Init(BillboardType _type, string code_desc, string txt_onBillboard, Color col)
	{
		canvas.worldCamera = Camera.main;
		GameManager.instance.AddBilboard(this, _type != BillboardType.NONE);
		foreach (BillboardScreen screen in screens)
		{
			if (screen.type == _type)
			{
				currentScreen = screen;
				if (!currentScreen.ob.activeSelf)
				{
					currentScreen.ob.SetObActive(active: true);
				}
				if (currentScreen.lbBillboard != null)
				{
					currentScreen.lbBillboard.SetText(txt_onBillboard);
				}
				anim.SetBool("Bobbing", currentScreen.bobbing);
			}
			else
			{
				screen.ob.SetObActive(active: false);
			}
		}
		BillboardUpdate();
		codeDesc = code_desc;
		colDesc = col;
	}

	public void BillboardUpdate()
	{
		if (currentScreen != null && !(currentScreen.ob == null) && !(Camera.main == null))
		{
			currentScreen.ob.transform.rotation = Quaternion.LookRotation(Toolkit.LookVector(Camera.main.transform.position, currentScreen.ob.transform.position), Camera.main.transform.up);
			graphicRaycaster.enabled = Gameplay.instance.ShouldUIBeInteractable() && Gameplay.instance.GetTrailType() == TrailType.NONE;
			canvas.enabled = UIGlobal.instance.canvas.enabled;
		}
	}

	public virtual void OnPointerEnter(PointerEventData event_data)
	{
		string text = "";
		if (codeDesc != "")
		{
			text = Loc.GetUI(codeDesc);
		}
		if (text != "")
		{
			UIHover.instance.Init(this);
			UIHover.instance.SetWidth();
			UIHover.instance.SetText(text, colDesc);
		}
	}

	public virtual void OnPointerExit(PointerEventData event_data)
	{
		UIHover.instance.Outit(this);
	}
}
