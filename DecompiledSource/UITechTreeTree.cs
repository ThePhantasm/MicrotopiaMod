using System;
using System.Collections.Generic;
using Radishmouse;
using UnityEngine;
using UnityEngine.UI;

public class UITechTreeTree : UIBase
{
	public List<TechTreeType> types = new List<TechTreeType>();

	public List<UITechTreeBox> listBoxes = new List<UITechTreeBox>();

	public UILineRenderer prefabLineRenderer;

	public UITechTreeLine prefabTechTreeLine;

	public UITechTreeLine prefabTechTreeLine_pop;

	public Transform lineParent;

	public Scrollbar scrollBar;

	public DragZoomRect dragZoomRect;

	private Dictionary<string, UITechTreeBox> dicBoxes;

	public List<UITechTreeLine> spawnedTechTreeLines = new List<UITechTreeLine>();

	private List<string> missingTechs = new List<string>();

	private static List<string> animateOnOpen = new List<string>();

	public void Init(TechTreeType _type, bool first_time, Action on_tech_unlock)
	{
		foreach (UITechTreeBox box in listBoxes)
		{
			if (first_time)
			{
				Tech tech = Tech.Get(box.techCode);
				box.Init(delegate
				{
					if (!DebugSettings.standard.demo || tech.inDemo)
					{
						if (tech.IsIdea(out var _task))
						{
							if (_task.IsSatisfied(recalc: true))
							{
								box.DoOnClickVisual();
								AudioManager.PlayUI(UISfx.TechClick1Or2);
								_task.Achieve();
								_task.SetStatus(TaskStatus.COMPLETED);
								if (!_task.idea)
								{
									Instinct.SetFirstUncompletedInstinct();
								}
								Gameplay.DoRefreshUnlocks();
							}
							else if (!Instinct.GetCurrentTasks().Contains(_task))
							{
								_task.SetStatus(TaskStatus.CURRENT);
								AudioManager.PlayUI(UISfx.TechIdeaEnable);
							}
							else
							{
								_task.SetStatus(TaskStatus.NONE);
								AudioManager.PlayUI(UISfx.TechIdeaDisable);
							}
							UIGame.instance.SetupTasks();
							UIGame.instance.OpenTargetTask(_task.code);
						}
						else
						{
							box.DoOnClickVisual();
							foreach (InventorPointsCost cost in tech.costs)
							{
								Progress.RemoveInventorPoints(cost.type, cost.amount);
							}
							TechTree.GiveTech(tech.code);
							AudioManager.PlayUI(UISfx.TechClick1Or2);
						}
						foreach (UITechTreeBox listBox in listBoxes)
						{
							listBox.SetInteractable();
						}
						box.UpdateBox();
						SetLinesProgress(instant: false, editor: false);
						on_tech_unlock();
					}
				});
			}
			box.UpdateBox();
		}
		if (first_time)
		{
			if (scrollBar != null)
			{
				scrollBar.value = 0f;
			}
			foreach (UITechTreeLine spawnedTechTreeLine in spawnedTechTreeLines)
			{
				spawnedTechTreeLine.InitMaterial();
			}
		}
		if (dragZoomRect != null)
		{
			dragZoomRect.Init(first_time);
		}
		SetLinesProgress(instant: true, editor: false);
	}

	public void TechTreeUpdate(bool editor)
	{
		int num = 0;
		foreach (UITechTreeBox listBox in listBoxes)
		{
			if (!editor)
			{
				listBox.UpdateBoxAnimation(Time.deltaTime);
			}
			if (dicBoxes == null)
			{
				CreateDicBoxes();
			}
			foreach (string requiredTech in listBox.requiredTechs)
			{
				if (!dicBoxes.ContainsKey(requiredTech))
				{
					if (!missingTechs.Contains(requiredTech))
					{
						Debug.LogError(listBox.techCode + ": Couldn't find tech " + requiredTech);
						missingTechs.Add(requiredTech);
					}
					continue;
				}
				UITechTreeLine uITechTreeLine = spawnedTechTreeLines[num];
				uITechTreeLine.UpdateLine(dicBoxes[requiredTech].GetAnchoredPos(), listBox.GetAnchoredPos());
				if (editor)
				{
					uITechTreeLine.SetObActive(active: true);
				}
				num++;
			}
		}
	}

	public void SetLinesProgress(bool instant, bool editor)
	{
		int num = 0;
		foreach (UITechTreeBox listBox in listBoxes)
		{
			if (dicBoxes == null)
			{
				CreateDicBoxes();
			}
			foreach (string requiredTech in listBox.requiredTechs)
			{
				if (!dicBoxes.ContainsKey(requiredTech))
				{
					if (!missingTechs.Contains(requiredTech))
					{
						Debug.LogError(listBox.techCode + ": Couldn't find tech " + requiredTech);
						missingTechs.Add(requiredTech);
					}
					continue;
				}
				UITechTreeLine uITechTreeLine = spawnedTechTreeLines[num];
				if (editor)
				{
					uITechTreeLine.SetLineInstant(1f);
				}
				else
				{
					Tech tech = Tech.Get(dicBoxes[requiredTech].techCode);
					if (tech.GetStatus() != TechStatus.DONE)
					{
						uITechTreeLine.SetLineInstant(0f);
					}
					else
					{
						uITechTreeLine.SetObActive(active: true);
						if (animateOnOpen.Contains(tech.code) || !instant)
						{
							uITechTreeLine.StartLine(uITechTreeLine.GetLength(), listBox.UpdateBox);
							if (animateOnOpen.Contains(tech.code))
							{
								animateOnOpen.Remove(tech.code);
							}
						}
						else
						{
							uITechTreeLine.SetLineInstant(1f);
						}
					}
				}
				num++;
			}
		}
	}

	public static void AddToAnimateOnOpen(string code)
	{
		if (!animateOnOpen.Contains(code))
		{
			animateOnOpen.Add(code);
		}
	}

	public void CreateTechTreeLines()
	{
		if (!(prefabTechTreeLine != null))
		{
			return;
		}
		prefabTechTreeLine.SetObActive(active: false);
		List<GameObject> list = new List<GameObject>();
		foreach (Transform item in lineParent)
		{
			list.Add(item.gameObject);
		}
		list.ForEach(delegate(GameObject child)
		{
			UnityEngine.Object.DestroyImmediate(child);
		});
		if (lineParent != null)
		{
			List<UITechTreeLine> list2 = new List<UITechTreeLine>();
			foreach (UITechTreeBox listBox in listBoxes)
			{
				for (int num = 0; num < listBox.requiredTechs.Count; num++)
				{
					UITechTreeLine component = UnityEngine.Object.Instantiate(listBox.populationLine ? prefabTechTreeLine_pop : prefabTechTreeLine, lineParent).GetComponent<UITechTreeLine>();
					list2.Add(component);
				}
			}
			foreach (UITechTreeLine item2 in list2)
			{
				item2.SetObActive(active: false);
			}
			spawnedTechTreeLines = list2;
		}
		SetLinesProgress(instant: true, editor: true);
	}

	private void CreateDicBoxes()
	{
		dicBoxes = new Dictionary<string, UITechTreeBox>();
		foreach (UITechTreeBox listBox in listBoxes)
		{
			dicBoxes.Add(listBox.techCode, listBox);
		}
	}
}
