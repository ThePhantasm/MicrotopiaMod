using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITask_InstinctIdea : UITask
{
	[Header("Instinct / Idea")]
	[SerializeField]
	private RectTransform rtCompleteFaded;

	[SerializeField]
	private TMP_Text lbTitle;

	[SerializeField]
	private TMP_Text lbStory;

	[SerializeField]
	private TMP_Text lbDescription;

	[SerializeField]
	private TMP_Text lbTip;

	[SerializeField]
	private UIButton btClose;

	[SerializeField]
	private UIButton btComplete;

	[SerializeField]
	private UITaskItem prefabItem;

	[SerializeField]
	private List<GameObject> obsInstinct;

	[SerializeField]
	private List<GameObject> obsIdea;

	private Task currentTask;

	private List<UITaskItem> itemList = new List<UITaskItem>();

	private int recalcTaskI;

	public void Init(Task _task, Action on_click_toggleOpen)
	{
		Init(on_click_toggleOpen);
		currentTask = _task;
		foreach (GameObject item in obsInstinct)
		{
			item.SetObActive(!currentTask.idea);
		}
		foreach (GameObject item2 in obsIdea)
		{
			item2.SetObActive(currentTask.idea);
		}
		prefabItem.SetObActive(active: false);
		btClose.Init(delegate
		{
			if (currentTask.status == TaskStatus.CURRENT)
			{
				currentTask.SetStatus(TaskStatus.NONE);
			}
			UIGame.instance.RefreshTasks();
		});
		btClose.SetObActive(currentTask.idea);
		btComplete.Init(delegate
		{
			if (currentTask != null)
			{
				currentTask.Achieve();
				currentTask.SetStatus(TaskStatus.COMPLETED);
				if (currentTask.idea)
				{
					UITechTreeTree.AddToAnimateOnOpen(currentTask.code);
				}
				else
				{
					Instinct.SetFirstUncompletedInstinct();
				}
				Gameplay.DoRefreshUnlocks();
				UIGame.instance.RefreshTasks();
			}
		});
		string text = "";
		if (currentTask.idea)
		{
			text = Tech.Get(currentTask.code).GetTitle();
		}
		else
		{
			int instinctNumber = Instinct.GetInstinctNumber(currentTask.code);
			text = instinctNumber switch
			{
				-1 => Loc.GetUI("INSTINCT_TITLE", "???"), 
				0 => Loc.GetUI("INSTINCT_TITLE_INTRO"), 
				_ => Loc.GetUI("INSTINCT_TITLE", instinctNumber.ToString()), 
			};
		}
		SetText(lbTitle, text);
		SetText(lbStory, currentTask.GetStory());
		SetText(lbDescription, currentTask.GetShort());
		SetText(lbTip, currentTask.GetTip());
		foreach (UITaskItem item3 in itemList)
		{
			item3.SetObActive(active: false);
		}
		if (itemList.Count < currentTask.subTasks.Count)
		{
			int num = currentTask.subTasks.Count - itemList.Count;
			for (int num2 = 0; num2 < num; num2++)
			{
				UITaskItem component = UnityEngine.Object.Instantiate(prefabItem, prefabItem.transform.parent).GetComponent<UITaskItem>();
				itemList.Add(component);
			}
		}
		for (int num3 = 0; num3 < currentTask.subTasks.Count; num3++)
		{
			itemList[num3].SetObActive(active: true);
			itemList[num3].SetText((currentTask.subTasks.Count == 1) ? "" : currentTask.subTasks[num3].GetDesc());
		}
	}

	public override void UIUpdate()
	{
		base.UIUpdate();
		for (int i = 0; i < currentTask.subTasks.Count; i++)
		{
			SubTask subTask = currentTask.subTasks[i];
			subTask.RecalcValues(i == recalcTaskI);
			itemList[i].SetSlider(Mathf.Clamp(subTask.valueCurrent, 0f, subTask.valueRequired), subTask.valueRequired);
		}
		if (++recalcTaskI >= currentTask.subTasks.Count)
		{
			recalcTaskI = 0;
		}
		bool flag = currentTask.IsSatisfied();
		btComplete.SetObActive(flag);
		rtCompleteFaded.SetObActive(!flag);
	}

	private void SetText(TMP_Text lb, string txt)
	{
		if (txt == "")
		{
			lb.SetObActive(active: false);
			return;
		}
		lb.text = txt;
		lb.SetObActive(active: true);
	}

	public Task GetTask()
	{
		return currentTask;
	}

	public override TaskID GetUID()
	{
		return TaskID.Instinct(currentTask);
	}
}
