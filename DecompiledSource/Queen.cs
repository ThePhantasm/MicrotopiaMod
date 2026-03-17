using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Queen : Building
{
	private enum LarvaProductionStage
	{
		None,
		Producing,
		Spawning,
		Ready
	}

	[Header("Queen")]
	public Transform larvaSpawnPoint;

	public Transform larvaLandArea;

	public Transform[] antLandPoints;

	public List<GameObject> obsCarryingAnts = new List<GameObject>();

	public List<GameObject> obsWings = new List<GameObject>();

	public GameObject obShadow;

	[NonSerialized]
	public float energy = -1f;

	public float spawnLarvaTime;

	public float birthTotalTime;

	public float feedArrivalTime;

	public float feedTotalTime;

	public float flyInTime;

	public float startupTotalTime;

	private float tFeed;

	private float tStartup;

	private bool bDroppedAnts;

	private LarvaProductionStage larvaProductionStage;

	private float tLarvaProductionStage;

	private float tLarvaTimer;

	private Pickup carryingLarva;

	[NonSerialized]
	public int nTimesFed;

	private Hunger hunger;

	private int bonusLarva;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(energy);
		save.Write((int)larvaProductionStage);
		save.Write(tLarvaProductionStage);
		save.Write((!(carryingLarva == null)) ? carryingLarva.linkId : 0);
		save.Write(tFeed);
		save.Write(bDroppedAnts);
		save.Write(nTimesFed);
		save.Write(bonusLarva);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		energy = save.ReadFloat();
		larvaProductionStage = (LarvaProductionStage)save.ReadInt();
		tLarvaProductionStage = save.ReadFloat();
		carryingLarva = GameManager.instance.FindLink<Pickup>(save.ReadInt());
		if (carryingLarva != null)
		{
			carryingLarva.SetStatus(PickupStatus.IN_CONTAINER, extractPoint.transform);
		}
		tFeed = save.ReadFloat();
		bDroppedAnts = save.ReadBool();
		nTimesFed = save.ReadInt();
		bonusLarva = save.ReadInt();
	}

	public override void Init(bool during_load = false)
	{
		tFeed = float.MaxValue;
		tStartup = float.MaxValue;
		foreach (GameObject obsWing in obsWings)
		{
			obsWing.SetObActive(active: false);
		}
		foreach (GameObject obsCarryingAnt in obsCarryingAnts)
		{
			obsCarryingAnt.SetObActive(active: false);
		}
		base.Init(during_load);
		if (during_load)
		{
			anim.SetBool("SkipFlyIn", value: true);
			if (!bDroppedAnts)
			{
				StartCoroutine(CDropAnts(instant: true));
			}
			if (GameManager.instance.theater)
			{
				larvaProductionStage = LarvaProductionStage.None;
			}
			else
			{
				StartHunger();
			}
		}
		CountAsAnt(caa: true);
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		if (currentStatus == BuildingStatus.BUILDING)
		{
			UpdateBuildProgress();
		}
		if (!runWorld)
		{
			return;
		}
		if (tStartup < startupTotalTime)
		{
			if (tStartup.TriggersAtTime(0f, dt))
			{
				foreach (GameObject obsWing in obsWings)
				{
					obsWing.SetObActive(active: true);
				}
				foreach (GameObject obsCarryingAnt in obsCarryingAnts)
				{
					obsCarryingAnt.SetObActive(active: true);
				}
				anim.SetBool("FlyIn", value: true);
				ChangeAudioAttachment(anim.transform);
				PlayAudio(AudioLinks.standard.queenFlyIn);
			}
			if (tStartup.TriggersAtTime(flyInTime, dt))
			{
				foreach (GameObject obsWing2 in obsWings)
				{
					obsWing2.SetObActive(active: false);
				}
				StartCoroutine(CDropAnts(instant: false));
				StartHunger();
			}
			tStartup += dt;
			if (tStartup >= startupTotalTime)
			{
				ChangeAudioAttachment(base.transform);
			}
		}
		else if (tFeed < feedTotalTime)
		{
			if (tFeed.TriggersAtTime(0f, dt))
			{
				anim.SetTrigger("DoFeed");
			}
			tFeed += dt;
		}
		if (hunger == null)
		{
			return;
		}
		hunger.Process(dt);
		tLarvaTimer += dt;
		switch (larvaProductionStage)
		{
		case LarvaProductionStage.None:
			if (GameManager.instance.GetAntCount() < hunger.maxPopulation || DebugSettings.standard.FreeLarvae() || bonusLarva > 0)
			{
				larvaProductionStage = LarvaProductionStage.Producing;
			}
			break;
		case LarvaProductionStage.Producing:
		{
			if (GameManager.instance.GetAntCount() >= hunger.maxPopulation && !DebugSettings.standard.FreeLarvae() && bonusLarva == 0)
			{
				larvaProductionStage = LarvaProductionStage.None;
				break;
			}
			float num = 60f / hunger.larvaRate;
			if (tLarvaTimer > num || DebugSettings.standard.FreeLarvae() || bonusLarva > 0)
			{
				PlayAudioShort(WorldSfx.QueenLarvaSpawn);
				anim.SetTrigger("DoBirth");
				larvaProductionStage = LarvaProductionStage.Spawning;
				tLarvaProductionStage = birthTotalTime;
				tLarvaTimer = 0f;
			}
			break;
		}
		case LarvaProductionStage.Spawning:
			tLarvaProductionStage -= dt * hunger.queenAnimationSpeed;
			if (tLarvaProductionStage < birthTotalTime - spawnLarvaTime && carryingLarva == null)
			{
				carryingLarva = GameManager.instance.SpawnPickup(PickupType.LARVAE_T1, extractPoint.transform.position, extractPoint.transform.rotation);
				extractablePickupsChanged = true;
				carryingLarva.SetStatus(PickupStatus.IN_CONTAINER, extractPoint.transform);
				if (bonusLarva > 0)
				{
					bonusLarva--;
				}
				if (UIGame.instance != null)
				{
					UIGame.instance.CountInventory();
				}
			}
			if (tLarvaProductionStage < 0f)
			{
				larvaProductionStage = LarvaProductionStage.Ready;
			}
			break;
		case LarvaProductionStage.Ready:
			if (carryingLarva == null)
			{
				if (GameManager.instance.GetAntCount() < hunger.maxPopulation || DebugSettings.standard.FreeLarvae() || bonusLarva > 0)
				{
					larvaProductionStage = LarvaProductionStage.Producing;
				}
				else
				{
					larvaProductionStage = LarvaProductionStage.None;
				}
			}
			break;
		}
	}

	public void StartHunger()
	{
		hunger = new Hunger(this);
		UpdateAnimationSpeed();
		if (obShadow != null)
		{
			UnityEngine.Object.Destroy(obShadow);
		}
	}

	public void AddEnergy(float f)
	{
		energy += f;
		if (hunger != null)
		{
			hunger.EnergyChanged();
		}
	}

	public float GetStartupProgress()
	{
		if (currentStatus != BuildingStatus.BUILDING && currentStatus != BuildingStatus.COMPLETED)
		{
			return 0f;
		}
		return tStartup / startupTotalTime;
	}

	public void AddBonusLarva(int n)
	{
		bonusLarva += n;
	}

	public void UpdateAnimationSpeed()
	{
		float value = ((hunger != null) ? hunger.queenAnimationSpeed : 1f);
		anim.SetFloat(ClickableObject.paramSpeed, value);
	}

	public override void PlaceBuilding()
	{
		base.PlaceBuilding();
		if (DebugSettings.standard.instantQueen)
		{
			anim.SetBool("SkipFlyIn", value: true);
			if (!bDroppedAnts)
			{
				StartCoroutine(CDropAnts(instant: true));
			}
			if (hunger == null)
			{
				StartHunger();
			}
		}
		else
		{
			tStartup = 0f;
			obShadow.transform.parent = null;
			obShadow.SetActive(value: true);
		}
	}

	protected override bool UseHoverCollider()
	{
		foreach (Queen item in GameManager.instance.EQueens())
		{
			if (item.currentStatus == BuildingStatus.COMPLETED)
			{
				return false;
			}
		}
		return true;
	}

	protected override bool CanInsert_Intake(PickupType _type, ExchangeType exchange, ExchangePoint point, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange == ExchangeType.BUILDING_IN)
		{
			if (tFeed < feedTotalTime)
			{
				let_ant_wait = true;
				return false;
			}
			return PickupData.Get(_type).energyAmount > 0f;
		}
		return base.CanInsert_Intake(_type, exchange, point, ref let_ant_wait, show_billboard);
	}

	protected override void PrepareForPickup_Intake(Pickup _pickup, ExchangePoint _point)
	{
		base.PrepareForPickup_Intake(_pickup, _point);
		tFeed = 0f;
	}

	protected override void OnPickupArrival_Intake(Pickup _pickup, ExchangePoint point)
	{
		base.OnPickupArrival_Intake(_pickup, point);
		_pickup.Delete();
		PlayAudioShort(WorldSfx.QueenEat);
		AddEnergy(_pickup.data.energyAmount);
		nTimesFed++;
		Progress.AddPickupFedToQueen(_pickup.type);
	}

	public override bool CanExtract(ExchangeType exchange, ref bool let_ant_wait, bool show_billboard = false)
	{
		if (exchange != ExchangeType.BUILDING_OUT)
		{
			return false;
		}
		if (larvaProductionStage == LarvaProductionStage.Ready && carryingLarva != null)
		{
			return true;
		}
		if (larvaProductionStage != LarvaProductionStage.None)
		{
			let_ant_wait = true;
		}
		return false;
	}

	public override List<PickupType> GetExtractablePickupsInternal()
	{
		if (carryingLarva == null)
		{
			return ConnectableObject.emptyPickupList;
		}
		return new List<PickupType> { carryingLarva.type };
	}

	public override Pickup ExtractPickup(PickupType _type)
	{
		if (_type != carryingLarva.type)
		{
			return null;
		}
		Pickup result = carryingLarva;
		carryingLarva = null;
		extractablePickupsChanged = true;
		return result;
	}

	private IEnumerator CDropAnts(bool instant)
	{
		List<Transform> land_points = new List<Transform>(antLandPoints);
		for (int i = 0; i < obsCarryingAnts.Count; i++)
		{
			Transform transform = land_points[UnityEngine.Random.Range(0, land_points.Count)];
			land_points.Remove(transform);
			obsCarryingAnts[i].transform.parent = null;
			Quaternion target_rot = Quaternion.LookRotation(Toolkit.LookVector(base.transform.position, transform.position));
			PlayAudioShort(WorldSfx.AntHop);
			StartCoroutine(Toolkit.CJumpTo(obsCarryingAnts[i].transform, transform, target_rot, instant ? 0f : 0.5f, GlobalValues.standard.curveParabola, Vector3.Distance(obsCarryingAnts[i].transform.position, transform.position) * 0.5f));
			yield return new WaitForSeconds(instant ? 0f : 0.3f);
		}
		yield return new WaitForSeconds(instant ? 0f : 0.2f);
		foreach (GameObject obsCarryingAnt in obsCarryingAnts)
		{
			GameManager.instance.SpawnAnt(AntCaste.SENTRY, obsCarryingAnt.transform.position, obsCarryingAnt.transform.rotation);
			obsCarryingAnt.SetObActive(active: false);
		}
		Platform.current.GainAchievement(Achievement.LAND_QUEEN);
		bDroppedAnts = true;
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		ui_hover.SetTitle(data.GetTitle());
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
	}

	protected override void SetHoverBottomButtons_old(UIHoverClickOb ui_hover)
	{
		ui_hover.SetBottomButtons(null, delegate
		{
			Gameplay.instance.StartRelocate(this);
		}, null);
	}

	public override string ExchangeDescription(ExchangeType _type)
	{
		return _type switch
		{
			ExchangeType.BUILDING_IN => Loc.GetUI("BUILDING_QUEEN_FEED"), 
			ExchangeType.BUILDING_OUT => Loc.GetUI("BUILDING_QUEEN_TAKE"), 
			_ => base.ExchangeDescription(_type), 
		};
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		base.SetClickUi(ui_click);
		ui_click.SetButtonHover(UIClickButtonType.Relocate, "CLICKBOTBUT_HOVER_RELOCATE_QUEEN");
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}

	protected override void DoDelete()
	{
		if (carryingLarva != null)
		{
			carryingLarva.Delete();
		}
		if (obShadow != null)
		{
			UnityEngine.Object.Destroy(obShadow);
		}
		base.DoDelete();
	}
}
