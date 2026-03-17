using UnityEngine;

public class BuildingAssigning
{
	private ClickableObject currentOb;

	private AssignType currentType = AssignType.FLIGHT;

	private Vector3? mousePosition;

	private ClickableObject obUnderMouse;

	private string hoveringError = "";

	private ClickableObject lastHighlighted;

	public void Start()
	{
	}

	public void Stop()
	{
		currentOb.HideAssignLines();
		currentOb = null;
		Gameplay.instance.HideGlobalAssignLines();
	}

	public void SetObject(ClickableObject _ob, AssignType _type)
	{
		currentOb = _ob;
		currentType = _type;
	}

	public ClickableObject GetCurrentOb()
	{
		return currentOb;
	}

	public void AssigningUpdate(Vector3? mouse_position, ClickableObject ob_under_mouse)
	{
		mousePosition = mouse_position;
		obUnderMouse = ob_under_mouse;
		bool flag = false;
		Vector3 vector = Vector3.zero;
		AssignLineStatus line_status = AssignLineStatus.WHITE;
		string error = "";
		if (obUnderMouse != null && currentOb.CanAssignTo(obUnderMouse, out error))
		{
			flag = true;
			vector = obUnderMouse.GetAssignLinePos(currentType);
			Gameplay.instance.AddHighlight(HighlightType.OUTLINE_WHITE, obUnderMouse);
			line_status = ((!(error != "")) ? AssignLineStatus.GREEN : AssignLineStatus.RED);
			if (obUnderMouse != lastHighlighted)
			{
				if (lastHighlighted != null)
				{
					lastHighlighted.OnHoverExit();
				}
				if (obUnderMouse != null)
				{
					obUnderMouse.OnHoverEnter();
				}
				lastHighlighted = obUnderMouse;
			}
		}
		else
		{
			if (mousePosition.HasValue)
			{
				flag = true;
				vector = mousePosition.Value.TargetYPosition(1f);
				line_status = AssignLineStatus.RED;
			}
			if (lastHighlighted != null)
			{
				lastHighlighted.OnHoverExit();
				lastHighlighted = null;
			}
		}
		if (flag)
		{
			if (Vector3.Distance(currentOb.transform.position, vector) > currentOb.AssigningMaxRange())
			{
				line_status = AssignLineStatus.RED;
				vector = currentOb.transform.position.TargetYPosition(1f) + Toolkit.LookVectorNormalized(currentOb.transform.position, vector) * currentOb.AssigningMaxRange();
			}
			Gameplay.instance.ShowAssignLine(currentOb.GetAssignLinePos(currentType), vector, currentType, line_status);
			currentOb.SetAssignLine(show: true);
		}
		else
		{
			Gameplay.instance.HideGlobalAssignLines();
			currentOb.SetAssignLine(show: false);
		}
		Gameplay.instance.AddHighlight(HighlightType.OUTLINE_WHITE, currentOb);
		Gameplay.instance.Clear3DCursor();
		if (error != hoveringError)
		{
			string text = "";
			if (error != "")
			{
				text = Loc.GetUI(error);
			}
			if (text != "")
			{
				UIHover.instance.Init(UIGame.instance);
				UIHover.instance.SetWidth();
				UIHover.instance.SetText(text, Color.red);
			}
			if (error == "" || text == "")
			{
				UIHover.instance.Outit(UIGame.instance);
			}
			hoveringError = error;
		}
	}

	public void ClickLeftDown()
	{
		ClickableObject ob = currentOb;
		if (obUnderMouse != null && currentOb.CanAssignTo(obUnderMouse, out var error) && error == "")
		{
			currentOb.Assign(obUnderMouse);
			switch (currentOb.ActionAfterAssign())
			{
			case AfterAssignAction.SELECT_SELF:
				Deselect();
				Gameplay.instance.Select(ob);
				break;
			case AfterAssignAction.SELECT_OTHER:
				Deselect();
				Gameplay.instance.Select(obUnderMouse);
				break;
			case AfterAssignAction.CONTINUE:
				break;
			}
		}
	}

	public void Deselect()
	{
		Gameplay.instance.SetActivity(Activity.NONE);
		UIHover.instance.Outit();
	}
}
