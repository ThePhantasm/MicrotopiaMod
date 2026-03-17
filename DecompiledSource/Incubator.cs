using System.Collections.Generic;
using UnityEngine;

public class Incubator : Factory
{
	[Header("Incubator")]
	public GameObject obCocoonT1;

	public GameObject obCocoonT2;

	public GameObject obCocoonT3;

	public GameObject obLarvaT1;

	public GameObject obLarvaT2;

	public GameObject obLarvaT3;

	public GameObject obAntT1;

	public GameObject obAntT2;

	public GameObject obAntT3;

	public Animator animCocoonT1;

	public Animator animCocoonT2;

	public Animator animCocoonT3;

	public Animator animAntT1;

	public Animator animAntT2;

	public Animator animAntT3;

	public float windUpTime;

	public float larvaDespawnTime;

	public float antSpawnTime;

	public float antDoStandUpTime;

	private float tGrow;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(tGrow);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		tGrow = save.ReadFloat();
		ShowLarva(show: false);
		ShowCocoon(show: false);
		ShowAnt(show: false);
		if (tGrow < processAnimationDuration)
		{
			if (tGrow < antSpawnTime)
			{
				ShowCocoon(show: true);
				SetAnimCocoon("DoGrow");
			}
			else
			{
				ShowAnt(show: true);
			}
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (!during_load)
		{
			ShowCocoon(show: false);
			ShowLarva(show: false);
			ShowAnt(show: false);
			tGrow = float.MaxValue;
		}
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (runWorld && tGrow < processAnimationDuration)
		{
			if (tGrow.TriggersAtTime(windUpTime, dt))
			{
				ShowCocoon(show: true);
				SetAnimCocoon("DoGrow");
			}
			if (tGrow.TriggersAtTime(larvaDespawnTime, dt))
			{
				ShowLarva(show: false);
			}
			if (tGrow.TriggersAtTime(antSpawnTime, dt))
			{
				ShowAnt(show: true);
			}
			if (tGrow.TriggersAtTime(antDoStandUpTime, dt))
			{
				SetAnimAnt("DoStandUp");
			}
			tGrow += dt;
		}
	}

	private void ShowCocoon(bool show)
	{
		ShowTierOb(obCocoonT1, obCocoonT2, obCocoonT3, show);
	}

	private void ShowAnt(bool show)
	{
		ShowTierOb(obAntT1, obAntT2, obAntT3, show);
	}

	private void ShowLarva(bool show)
	{
		ShowTierOb(obLarvaT1, obLarvaT2, obLarvaT3, show);
	}

	private void ShowTierOb(GameObject t1, GameObject t2, GameObject t3, bool show)
	{
		if (show)
		{
			(processingRecipe.productAnts[0].type switch
			{
				AntCaste.WORKER_SMALL_T1 => t1, 
				AntCaste.WORKER_SMALL_T2 => t2, 
				AntCaste.WORKER_SMALL_T3 => t3, 
				_ => obCocoonT1, 
			}).SetObActive(active: true);
		}
		else
		{
			t1.SetObActive(active: false);
			t2.SetObActive(active: false);
			t3.SetObActive(active: false);
		}
	}

	private void SetAnimCocoon(string anim)
	{
		if (animCocoonT1.isActiveAndEnabled)
		{
			animCocoonT1.SetTrigger(anim);
		}
		if (animCocoonT2.isActiveAndEnabled)
		{
			animCocoonT2.SetTrigger(anim);
		}
		if (animCocoonT3.isActiveAndEnabled)
		{
			animCocoonT3.SetTrigger(anim);
		}
	}

	private void SetAnimAnt(string anim)
	{
		if (animAntT1.isActiveAndEnabled)
		{
			animAntT1.SetTrigger(anim);
		}
		if (animAntT2.isActiveAndEnabled)
		{
			animAntT2.SetTrigger(anim);
		}
		if (animAntT3.isActiveAndEnabled)
		{
			animAntT3.SetTrigger(anim);
		}
	}

	protected override void SetProcess(bool target, bool on_init = false)
	{
		if (isProcessing != target || on_init)
		{
			CountAsAnt(target);
			if (target)
			{
				tGrow = 0f;
				if (processingRecipe == null)
				{
					Debug.LogWarning("Incubator: Processing but no recipe", base.gameObject);
				}
				else
				{
					ShowLarva(show: true);
				}
			}
			else
			{
				tGrow = float.MaxValue;
			}
		}
		base.SetProcess(target, on_init);
	}

	protected override Ant SpawnProductAnt(AntCaste ant_caste, List<TrailGate_Link> link_gates)
	{
		SetAnimCocoon("Reset");
		SetAnimAnt("Reset");
		ShowCocoon(show: false);
		ShowLarva(show: false);
		ShowAnt(show: false);
		Ant result = base.SpawnProductAnt(ant_caste, link_gates);
		EnableSpawnedAnt();
		if (ground.biome.biomeType == BiomeType.DESERT)
		{
			Progress.nLarvaeGrownInDesert++;
		}
		return result;
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange != ExchangeType.BUILDING_IN || antsWaitingToExit.Count > 0)
		{
			return false;
		}
		return base.CanInsert_Intake(_type, exchange, point, ref let_ant_wait, show_billboard);
	}

	public override IEnumerable<Animator> EPausableAnimators()
	{
		foreach (Animator item in base.EPausableAnimators())
		{
			yield return item;
		}
		yield return animCocoonT1;
		yield return animCocoonT2;
		yield return animCocoonT3;
		yield return animAntT1;
		yield return animAntT2;
		yield return animAntT3;
	}

	public override void SetHoverMode(bool hover)
	{
		base.SetHoverMode(hover);
		if (hover)
		{
			ShowCocoon(show: false);
			ShowLarva(show: false);
			ShowAnt(show: false);
		}
	}
}
