using System.Collections.Generic;
using UnityEngine;

public class UITechTree : UIBaseSingleton
{
	public static UITechTree instance;

	public List<UITechTreeTree> techTrees = new List<UITechTreeTree>();

	public UITextImageButton btClose;

	public UITechCurrency prefabInventorPoints;

	[Space(10f)]
	public float zoomIntensity = 0.5f;

	public Vector2 zoomRange = new Vector2(1f, 5f);

	private bool firstTime = true;

	private UITechTreeTree currentTree;

	private List<UITechCurrency> spawnedInventorPoints = new List<UITechCurrency>();

	private float showTime;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Init()
	{
		base.Show(target: true);
		TechTreeType type = ((!DebugSettings.standard.demo) ? TechTreeType.REGULAR : TechTreeType.DEMO);
		currentTree = GetTree(type);
		currentTree.Init(type, firstTime, delegate
		{
			SetInventorPoints();
		});
		if (firstTime)
		{
			btClose.Init();
			btClose.SetButton(delegate
			{
				GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
			});
			firstTime = false;
		}
		GameManager.instance.SetStatus(GameStatus.MENU);
		SetInventorPoints();
		showTime = Time.time;
	}

	public override void Show(bool target)
	{
		if (!target)
		{
			if (UIHover.instance != null)
			{
				UIHover.instance.StartClose();
			}
			if (UIGame.instance != null)
			{
				UIGame.instance.UpdateTechTreeButtonCurrencies();
			}
		}
		base.Show(target);
	}

	public void SetInventorPoints()
	{
		prefabInventorPoints.SetObActive(active: false);
		Dictionary<InventorPoints, int> dicInventorPoints = Progress.GetDicInventorPoints(preview: true);
		List<InventorPoints> listInventorPoints = Progress.GetListInventorPoints();
		if (spawnedInventorPoints.Count < listInventorPoints.Count)
		{
			int num = listInventorPoints.Count - spawnedInventorPoints.Count;
			for (int i = 0; i < num; i++)
			{
				UITechCurrency item = Object.Instantiate(prefabInventorPoints, prefabInventorPoints.transform.parent);
				spawnedInventorPoints.Add(item);
			}
		}
		foreach (UITechCurrency spawnedInventorPoint in spawnedInventorPoints)
		{
			spawnedInventorPoint.SetObActive(active: false);
		}
		for (int j = 0; j < listInventorPoints.Count; j++)
		{
			if (dicInventorPoints.ContainsKey(listInventorPoints[j]))
			{
				spawnedInventorPoints[j].Init(listInventorPoints[j], dicInventorPoints[listInventorPoints[j]].ToString());
				spawnedInventorPoints[j].SetHoverLocUI(TechTree.GetInventorPointsCode(listInventorPoints[j]));
				spawnedInventorPoints[j].AddOverlay(OverlayTypes.BACKGROUND);
				spawnedInventorPoints[j].SetObActive(active: true);
			}
		}
	}

	public UITechTreeTree GetTree(TechTreeType _type)
	{
		UITechTreeTree uITechTreeTree = null;
		foreach (UITechTreeTree techTree in techTrees)
		{
			if (techTree.types.Contains(_type))
			{
				uITechTreeTree = techTree;
				break;
			}
		}
		if (uITechTreeTree == null)
		{
			Debug.LogError("Tech Tree: No tree found for type " + _type);
			uITechTreeTree = techTrees[0];
		}
		foreach (UITechTreeTree techTree2 in techTrees)
		{
			techTree2.SetObActive(techTree2 == uITechTreeTree);
		}
		return uITechTreeTree;
	}

	public void TechTreeUpdate()
	{
		if (currentTree != null)
		{
			currentTree.TechTreeUpdate(editor: false);
		}
		if (InputManager.techTree && showTime < Time.time - 0.1f)
		{
			GameManager.instance.CloseAllMenuUI(resume_last_gamestate: true);
		}
	}
}
