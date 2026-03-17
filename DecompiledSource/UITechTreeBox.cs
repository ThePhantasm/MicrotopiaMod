using System;
using System.Collections.Generic;
using UnityEngine;

public class UITechTreeBox : UIBase
{
	public static bool noHover;

	public string techCode;

	public TechTreeBoxType type;

	public BoxShape[] shapes;

	public Animator anim;

	public int animationCount = 2;

	public RectTransform rtShape;

	public UITechTreeBoxShapes prefabBoxShapes;

	public UITechTreeBoxShape shape;

	public List<string> requiredTechs;

	private Tech tech;

	private Vector2 startPos;

	private Vector2 targetPos;

	private float speed;

	private float timer;

	private float duration;

	private bool hovering;

	[Space(10f)]
	[SerializeField]
	private bool floatingAround = true;

	public bool populationLine;

	private float growSpeed = 10f;

	private float shrinkSpeed = 5f;

	public void Init(Action on_click)
	{
		tech = Tech.Get(techCode);
		Task _task;
		bool is_idea = tech.IsIdea(out _task);
		Color col;
		Sprite icon = tech.GetIcon(out col);
		if (icon != null)
		{
			shape.SetImage(icon);
			shape.SetImageColor(col);
		}
		shape.SetButton(delegate
		{
			on_click();
			if (is_idea)
			{
				SetupUiHover(_task);
			}
			else
			{
				UIHover.instance.Outit(this);
			}
		});
		shape.SetOnPointerEnter(delegate
		{
			if (!noHover)
			{
				hovering = true;
				SetupUiHover(_task);
				AudioManager.PlayUI(UISfx.TechHover);
			}
		});
		shape.SetOnPointerExit(delegate
		{
			hovering = false;
			UIHover.instance.Outit(this);
		});
		speed = UnityEngine.Random.Range(4f, 6f);
		if (floatingAround)
		{
			SetAnimatePos();
			timer = UnityEngine.Random.Range(0f, duration);
			UpdateBoxAnimation(0f);
		}
		UpdateBox();
	}

	private void SetupUiHover(Task _task)
	{
		bool flag = _task != null;
		UIHover.instance.Init(this);
		UIHover.instance.SetWidth(354f);
		UIHover.instance.SetTitle(tech.GetTitle());
		if (!DebugSettings.standard.demo || tech.inDemo)
		{
			if (flag)
			{
				UIHover.instance.SetText(_task.GetStory(), (Color.gray + Color.white) / 2f);
				UIHover.instance.SetText2(_task.GetShort());
			}
			else
			{
				UIHover.instance.SetTopMessage(Tech.GetUnlockMessage(tech.techType));
				string description = tech.GetDescription();
				if (description != "")
				{
					UIHover.instance.SetText(description);
				}
			}
			TechStatus status = tech.GetStatus();
			switch (status)
			{
			case TechStatus.NONE:
				UIHover.instance.SetTextFooter(Loc.GetUI("TECHTREE_NOTYET"));
				break;
			case TechStatus.OPEN:
			{
				if (flag)
				{
					if (_task.IsSatisfied(recalc: true))
					{
						UIHover.instance.SetTextFooter(Loc.GetUI("TECHTREE_OBJECTIVEREACHED"), Color.green);
					}
					else if (Instinct.GetCurrentTasks().Contains(_task))
					{
						UIHover.instance.SetTextFooter(Loc.GetUI("TECHTREE_IDEACURRENTLYTRACKED"), Color.white);
					}
					else
					{
						UIHover.instance.SetTextFooter(Loc.GetUI("TECHTREE_CLICKTOADDIDEA"), Color.white);
					}
					break;
				}
				UIHover.instance.SetChecklist(tech.costs);
				if (CanPurchase(out var need_inventor, out var inventor_tier, out var need_gyne, out var gyne_tier))
				{
					UIHover.instance.SetTextFooter(Loc.GetUI("TECHTREE_CLICKTOUNLOCK"), Color.white);
					break;
				}
				string text = "";
				if (need_inventor)
				{
					text = inventor_tier switch
					{
						3 => text + Loc.GetUI("TECHTREE_NEEDPOINTSTIER3"), 
						2 => text + Loc.GetUI("TECHTREE_NEEDPOINTSTIER2"), 
						_ => text + Loc.GetUI("TECHTREE_NEEDPOINTS"), 
					};
				}
				if (need_inventor && need_gyne)
				{
					text += "\n";
				}
				if (need_gyne)
				{
					text = gyne_tier switch
					{
						3 => text + Loc.GetUI("TECHTREE_NEEDGYNEPOINTSTIER3"), 
						2 => text + Loc.GetUI("TECHTREE_NEEDGYNEPOINTSTIER2"), 
						_ => text + Loc.GetUI("TECHTREE_NEEDGYNEPOINTS"), 
					};
				}
				UIHover.instance.SetTextFooter(text, (Color.red + Color.white) / 2f);
				break;
			}
			case TechStatus.DONE:
				if (flag)
				{
					UIHover.instance.SetTextFooter(Loc.GetUI("TECHTREE_COMPLETED"), Color.green);
				}
				else
				{
					UIHover.instance.SetTextFooter(Loc.GetUI("TECHTREE_UNLOCKED"), Color.green);
				}
				break;
			}
			if (status == TechStatus.OPEN || status == TechStatus.DONE)
			{
				BuildingData craft_building;
				bool flag2 = tech.IsRecipeUnlock(out craft_building);
				if (tech.RequiredToCreate(out var req_ants, out var req_pickups))
				{
					string txt = (flag2 ? Loc.GetUI("TECHTREE_REQUIRED_TO_CRAFT") : Loc.GetUI("TECHTREE_REQUIRED_TO_BUILD"));
					UIHover.instance.SetRequired(txt, req_ants, req_pickups);
				}
				if (flag2)
				{
					UIHover.instance.SetCreatedAt(craft_building.GetTitle(), AssetLinks.standard.GetBuildingThumbnail(craft_building.code));
				}
			}
			return;
		}
		if (flag)
		{
			if (tech.inDemoDescription)
			{
				UIHover.instance.SetText(_task.GetStory(), (Color.gray + Color.white) / 2f);
				UIHover.instance.SetText2(_task.GetShort());
			}
		}
		else if (tech.inDemoDescription)
		{
			string description2 = tech.GetDescription();
			if (description2 != "")
			{
				UIHover.instance.SetText(description2);
			}
		}
		UIHover.instance.SetTextFooter(Loc.GetUI("DEMO_AVAILABLEFULLVERSION"), Color.yellow);
	}

	private bool CanPurchase(out bool need_inventor, out int inventor_tier, out bool need_gyne, out int gyne_tier)
	{
		need_inventor = false;
		inventor_tier = 1;
		need_gyne = false;
		gyne_tier = 1;
		if (tech.GetStatus() == TechStatus.OPEN)
		{
			return tech.CostReached(out need_inventor, out inventor_tier, out need_gyne, out gyne_tier);
		}
		return false;
	}

	private bool CanPurchase()
	{
		bool need_inventor;
		int inventor_tier;
		bool need_gyne;
		int gyne_tier;
		return CanPurchase(out need_inventor, out inventor_tier, out need_gyne, out gyne_tier);
	}

	public void SetInteractable()
	{
		shape.SetInteractable(CanPurchase());
	}

	public void UpdateBox()
	{
		shape.Init(tech.GetTitle());
		TechStatus status = tech.GetStatus();
		shape.UpdateBox(status);
		if (status == TechStatus.DONE)
		{
			shape.StartCompleted();
		}
		if (tech.IsIdea(out var _task))
		{
			if (Instinct.GetCurrentTasks().Contains(_task))
			{
				shape.AddOverlay(OverlayTypes.TRACKING);
			}
			if (status == TechStatus.DONE)
			{
				shape.AddOverlay(OverlayTypes.COMPLETED);
			}
		}
		shape.SetInteractable(CanPurchase());
	}

	public void DoOnClickVisual()
	{
		shape.Complete();
	}

	public void UpdateBoxAnimation(float dt)
	{
		if (hovering)
		{
			if (noHover)
			{
				hovering = false;
				UIHover.instance.Outit(this);
				return;
			}
			Vector3 vector = Vector3.one * 1.2f;
			if (rtShape.localScale != vector)
			{
				rtShape.localScale = Vector2.Lerp(rtShape.localScale, vector, growSpeed * dt);
				if (Vector3.Distance(rtShape.localScale, vector) < 0.01f)
				{
					rtShape.localScale = vector;
				}
			}
			return;
		}
		if (rtShape.localScale != Vector3.one)
		{
			rtShape.localScale = Vector2.Lerp(rtShape.localScale, Vector3.one, shrinkSpeed * dt);
			if (Vector3.Distance(rtShape.localScale, Vector3.one) < 0.01f)
			{
				rtShape.localScale = Vector3.one;
			}
		}
		if (floatingAround)
		{
			timer += dt;
			rtShape.localPosition = startPos + (targetPos - startPos) * GlobalValues.standard.curveSIn.Evaluate(Mathf.Clamp01(timer / duration));
			if (timer >= duration)
			{
				timer = 0f;
				SetAnimatePos();
			}
		}
	}

	private void SetAnimatePos()
	{
		rtShape.localPosition = targetPos;
		startPos = targetPos;
		targetPos = UnityEngine.Random.insideUnitCircle * 15f;
		duration = Vector2.Distance(startPos, targetPos) / UnityEngine.Random.Range(2f, 5f);
	}

	public UITechTreeBoxShape GetShape()
	{
		int childCount = rtShape.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			UnityEngine.Object.DestroyImmediate(rtShape.transform.GetChild(0).gameObject);
		}
		UITechTreeBoxShapes component = UnityEngine.Object.Instantiate(prefabBoxShapes, rtShape).GetComponent<UITechTreeBoxShapes>();
		component.transform.localPosition = Vector3.zero;
		UITechTreeBoxShape uITechTreeBoxShape = null;
		BoxShape[] array = component.shapes;
		foreach (BoxShape boxShape in array)
		{
			boxShape.shape.SetObActive(active: false);
			if (boxShape.type == type)
			{
				uITechTreeBoxShape = boxShape.shape;
			}
		}
		if (uITechTreeBoxShape == null)
		{
			uITechTreeBoxShape = component.shapes[0].shape;
		}
		uITechTreeBoxShape.SetObActive(active: true);
		uITechTreeBoxShape.transform.SetParent(rtShape, worldPositionStays: false);
		uITechTreeBoxShape.ResetPosition();
		UnityEngine.Object.DestroyImmediate(component.gameObject);
		return uITechTreeBoxShape;
	}

	public Vector2 GetAnchoredPos()
	{
		return rtBase.anchoredPosition + rtShape.anchoredPosition;
	}
}
