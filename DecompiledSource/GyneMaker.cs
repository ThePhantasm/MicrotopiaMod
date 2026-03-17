using System;
using System.Collections.Generic;
using UnityEngine;

public class GyneMaker : Factory
{
	[Serializable]
	public class CocoonMeshList
	{
		public AntCaste productGyne;

		public List<CocoonMesh> cocoonMeshes = new List<CocoonMesh>();
	}

	[Serializable]
	public class CocoonMesh
	{
		public List<GameObject> meshes;

		public int antCount;
	}

	[Header("Gyne Maker")]
	public List<CocoonMeshList> meshOrder = new List<CocoonMeshList>();

	public List<Animator> gyneAnims = new List<Animator>();

	private bool noPrincess;

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		if (storedRecipe != "")
		{
			List<Ant> list = new List<Ant>();
			Dictionary<AntCaste, int> dictionary = FactoryRecipeData.Get(storedRecipe).costsAnt.ToDictionary();
			Dictionary<AntCaste, int> dictionary2 = new Dictionary<AntCaste, int>();
			foreach (Ant item in antsInside)
			{
				if (!dictionary.ContainsKey(item.caste))
				{
					Debug.LogError(item.caste.ToString() + " inside gyne maker not found in recipe, cleaning up");
					list.Add(item);
				}
				else if (!dictionary2.ContainsKey(item.caste))
				{
					dictionary2.Add(item.caste, 1);
				}
				else if (dictionary2[item.caste] < dictionary[item.caste])
				{
					dictionary2[item.caste]++;
				}
				else
				{
					Debug.LogError("Too many " + item.caste.ToString() + " found in gyne maker, cleaning up");
					list.Add(item);
				}
			}
			foreach (Ant item2 in list)
			{
				antsInside.Remove(item2);
				item2.Delete();
			}
		}
		UpdateMesh();
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		UpdateMesh();
	}

	public override IEnumerable<Animator> EPausableAnimators()
	{
		foreach (Animator item in base.EPausableAnimators())
		{
			yield return item;
		}
		foreach (Animator gyneAnim in gyneAnims)
		{
			if (gyneAnim != null)
			{
				yield return gyneAnim;
			}
		}
	}

	private int GetTotalAntsInside()
	{
		int num = 0;
		foreach (Ant item in antsInside)
		{
			_ = item;
			num++;
		}
		return num;
	}

	public override bool DemolishWarning(out string msg)
	{
		if (GetTotalAntsInside() > 0)
		{
			msg = Loc.GetUI("BUILDING_GYNEMAKER_DEMOLISHWARNING");
			return true;
		}
		return base.DemolishWarning(out msg);
	}

	protected override bool AntsDieInside()
	{
		return true;
	}

	private void UpdateMesh()
	{
		foreach (CocoonMeshList item in meshOrder)
		{
			foreach (CocoonMesh cocoonMesh in item.cocoonMeshes)
			{
				foreach (GameObject mesh in cocoonMesh.meshes)
				{
					mesh.SetObActive(active: false);
				}
			}
		}
		if (!(GetStoredRecipe() != ""))
		{
			return;
		}
		AntCaste type = FactoryRecipeData.Get(GetStoredRecipe()).productAnts[0].type;
		int totalAntsInside = GetTotalAntsInside();
		foreach (CocoonMeshList item2 in meshOrder)
		{
			if (item2.productGyne != type)
			{
				continue;
			}
			for (int i = 0; i < item2.cocoonMeshes.Count; i++)
			{
				List<CocoonMesh> cocoonMeshes = item2.cocoonMeshes;
				if (totalAntsInside < cocoonMeshes[i].antCount || (i != cocoonMeshes.Count - 1 && totalAntsInside >= cocoonMeshes[i + 1].antCount))
				{
					continue;
				}
				foreach (GameObject mesh2 in cocoonMeshes[i].meshes)
				{
					mesh2.SetObActive(active: true);
				}
			}
		}
	}

	protected override void BuildProduct(FactoryRecipeData recipe, bool free)
	{
		base.BuildProduct(recipe, free);
		UpdateMesh();
	}

	private IEnumerable<Ant> EAnyAntsInside(Ant ant)
	{
		yield return ant;
		foreach (Ant item in antsInside)
		{
			if (item != null)
			{
				yield return item;
			}
		}
		foreach (Trail gateTrail in gateTrails)
		{
			foreach (Ant item2 in EAntsOnBuildingTrails(gateTrail))
			{
				yield return item2;
			}
		}
	}

	public override bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		if (ant.GetCarryingPickupsCount() > 0)
		{
			warning = "ANT_CANT_ENTER_CARRY";
			return false;
		}
		if (antsWaitingToMove.Count > 0)
		{
			return false;
		}
		noPrincess = false;
		ClearBillboard();
		UpdateBillboard(cancel_temporary: true);
		bool flag = false;
		Ant ant2 = null;
		foreach (Ant item in EAnyAntsInside(ant))
		{
			if (item.caste == AntCaste.PRINCESS)
			{
				ant2 = item;
			}
			else
			{
				flag = true;
			}
			if (flag && ant2 != null)
			{
				break;
			}
		}
		allowChangeRecipe = ant2 == null || !flag;
		if (ant2 == null)
		{
			noPrincess = true;
			UpdateBillboardTempory();
			return false;
		}
		return base.CheckIfGateIsSatisfied(ant, trail, out warning);
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		float result = base.UseBuilding(_entrance, _ant, out ant_entered);
		if (_ant.data.caste != AntCaste.PRINCESS)
		{
			_ant.transform.position += UnityEngine.Random.insideUnitSphere * 5f;
			_ant.transform.position.SetY(_ant.transform.position.y + 2.5f);
		}
		UpdateMesh();
		return result;
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (noPrincess)
		{
			code_desc = "BUILDING_GYNEMAKER_NEEDPRINCESS";
			col = Color.yellow;
			return BillboardType.EXCLAMATION;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}
}
