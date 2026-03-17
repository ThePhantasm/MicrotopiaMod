using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ant : ClickableObject
{
	[Header("Ant")]
	public List<Renderer> rends = new List<Renderer>();

	public Transform minimapMesh;

	public Transform carryPos;

	public Transform statusPos;

	public Animator anim;

	[SerializeField]
	private ParticleSystem[] particleSystems;

	public float antRadius = 2.5f;

	public float walkAnimationFactor = 5f;

	public int carryCapacity = 1;

	public float speedRotate = 500f;

	public float speedGetUp = 1000f;

	public float maxVelocityChange = 1f;

	private float deathJumpDuration = 0.3f;

	public Vector2 deathJumpDisHeightMod = new Vector2(1f, 1f);

	private float tDeathJump = float.MaxValue;

	private float deathJumpHeight;

	private Vector3 deathJumpStart;

	private Vector3 deathJumpEnd;

	private int corpsePollutionGroundId = -10;

	public float durationMineWindUp;

	public float durationMineWindDown;

	[NonSerialized]
	public AntCasteData data;

	protected float speedMove;

	protected float energyTotal;

	[NonSerialized]
	public AntCaste caste;

	[NonSerialized]
	public List<Pickup> carryingPickups = new List<Pickup>();

	[NonSerialized]
	public float birthTime;

	[NonSerialized]
	public Trail currentTrail;

	[NonSerialized]
	public float trailProgress;

	private float progressSpeed;

	private Trail nextTrail;

	private float assumedNextTrailTime;

	[NonSerialized]
	public MoveState moveState;

	[NonSerialized]
	public float energy;

	protected bool isHeadless;

	protected bool keepCommandTrail;

	[NonSerialized]
	public bool waitingAtEnd;

	[NonSerialized]
	public float randomValue;

	private Vector3 lastPosition;

	[NonSerialized]
	public float velocity;

	private Queue<ActionPoint> nextActionPoints = new Queue<ActionPoint>();

	private ActionPoint currentActionPoint;

	private float actionPointProgress;

	private Vector3 actionPointLookTarget;

	private const float ACTION_POINT_DO_RANGE = 0.5f;

	private const float ACTION_POINT_DO_RANGE_CLAMP = 0.45f;

	private const float STOP_BEFORE_END = 4f;

	protected const float STOP_BEFORE_ANT = 2f;

	private int currentActionPointLinkId = -10;

	private const float SEARCH_RADIUS_AFTER_LANDING = 8f;

	private bool isMining;

	private FlightPad flightLandPad;

	private int flightLandPadLinkId;

	private bool isFlying;

	private Vector3 flightTarget;

	private Vector3 flightStart;

	private Vector3 flightHeightStart;

	private Vector3 flightHeightEnd;

	private Vector3 flightLastPos;

	private float flightProgress;

	private FlyStage currentFlyStage;

	private int landPadRelocation;

	private Vector3 launchForce;

	private float timeLaunched;

	private AntLaunchCollider launchTriggerArea;

	private bool dieOnImpact;

	[NonSerialized]
	public List<Trail> returnToTrails;

	[NonSerialized]
	public bool searchForTrailsAfterLanding;

	private int vulnerabilityBits;

	protected StatusEffects statusEffects;

	private List<EffectArea> effectAreas;

	private float radiation;

	protected float radiationDeathTimer;

	[NonSerialized]
	public float deathTimer;

	private bool updateSometimes;

	private float dtSometimes;

	private DeathCause deathCause;

	[SerializeField]
	private ExplosionType deathEffect;

	[SerializeField]
	private Transform deathEffectPos;

	private bool isImmortal;

	private float electrolysedEnergy;

	private float electrolyseTarget;

	private bool electrolysing;

	private Collider[] cols;

	[NonSerialized]
	public Ground currentGround;

	private float groundCheckTimer;

	private new Transform transform;

	public static float TOP_SPEED;

	private Building launchOrigin;

	private string billboardWarning = "";

	private AudioChannel audioAnt;

	private void Awake()
	{
		transform = base.transform;
		if (anim != null)
		{
			anim.cullingMode = AnimatorCullingMode.CullCompletely;
		}
	}

	public void Fill(AntCasteData _data)
	{
		data = _data;
		speedMove = _data.speed;
		energyTotal = _data.energy;
		caste = _data.caste;
		vulnerabilityBits = _data.vulnerabilityBits;
		isImmortal = DebugSettings.standard.ImmortalAnts() || data.IsImmortal() || GameManager.instance.theater;
		statusEffects = new StatusEffects(this);
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (during_load)
		{
			SetMoveState(moveState);
			if (moveState == MoveState.Launched || moveState == MoveState.DeadAndLaunched)
			{
				StartLaunch(launchForce, LaunchCause.LOADED);
			}
		}
		else
		{
			SetMoveState(MoveState.Normal);
			energy = energyTotal;
			birthTime = (float)GameManager.instance.gameTime;
		}
		UpdateMaterial();
		randomValue = UnityEngine.Random.value;
		if (energy < 0f)
		{
			energy = 0f;
		}
		CheckGround();
		for (int i = 0; i < particleSystems.Length; i++)
		{
			GameManager.instance.AddPausableParticles(particleSystems[i]);
		}
	}

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(birthTime);
		save.Write(energy);
		save.Write((int)moveState);
		save.Write(carryingPickups.Count);
		foreach (Pickup carryingPickup in carryingPickups)
		{
			save.Write(carryingPickup.linkId);
		}
		save.Write((!(currentTrail == null)) ? currentTrail.linkId : 0);
		save.Write(trailProgress);
		save.Write((currentActionPoint != null) ? currentActionPoint.connectableObject.linkId : 0);
		save.Write(actionPointProgress);
		save.Write(isFlying);
		if (isFlying)
		{
			save.Write(flightTarget);
			save.Write(flightLandPad != null);
			if (flightLandPad != null)
			{
				save.Write(flightLandPad.linkId);
			}
			save.Write(flightStart);
			save.Write(flightHeightStart);
			save.Write(flightHeightEnd);
			save.Write(flightLastPos);
			save.Write(flightProgress);
			save.Write((int)currentFlyStage);
		}
		bool flag = IsDead() && tDeathJump < deathJumpDuration;
		save.Write(flag);
		if (flag)
		{
			save.Write(tDeathJump);
			save.Write(deathJumpHeight);
			save.Write(deathJumpStart);
			save.Write(deathJumpEnd);
		}
		save.Write(radiation);
		save.Write(deathTimer);
		statusEffects.Write(save);
		if (IsDead())
		{
			save.Write((int)deathCause);
			save.Write(corpsePollutionGroundId);
		}
		save.Write(launchForce);
		save.Write(timeLaunched);
		if (searchForTrailsAfterLanding)
		{
			save.Write(-1);
		}
		else
		{
			save.Write((returnToTrails != null) ? returnToTrails[0].linkId : 0);
		}
		save.Write(radiationDeathTimer);
		save.Write(dieOnImpact);
		save.Write(electrolysedEnergy);
		save.Write(electrolysing);
		save.Write(electrolyseTarget);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		birthTime = ((save.version < 62) ? ((float)GameManager.instance.gameTime) : save.ReadFloat());
		energy = save.ReadFloat();
		moveState = (MoveState)save.ReadInt();
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			int id = save.ReadInt();
			Pickup pickup = GameManager.instance.FindLink<Pickup>(id);
			if (pickup != null)
			{
				pickup.SetStatus(PickupStatus.CARRIED, carryPos);
				carryingPickups.Add(pickup);
			}
		}
		SetCarryAnim(num > 0);
		int num2 = save.ReadInt();
		if (num2 > 0)
		{
			SetCurrentTrail(GameManager.instance.FindLink<Trail>(num2));
		}
		trailProgress = save.ReadFloat();
		if (currentTrail != null && currentTrail.IsCommandTrail() && !keepCommandTrail)
		{
			currentTrail.SetStartPos(transform.position, only_visual: true);
		}
		currentActionPointLinkId = save.ReadInt();
		actionPointProgress = save.ReadFloat();
		isFlying = save.ReadBool();
		if (isFlying)
		{
			StartLoopAudio(AudioLinks.standard.antFlyLoop);
			flightTarget = save.ReadVector3();
			if (save.ReadBool())
			{
				flightLandPadLinkId = save.ReadInt();
			}
			flightStart = save.ReadVector3();
			flightHeightStart = save.ReadVector3();
			flightHeightEnd = save.ReadVector3();
			flightLastPos = save.ReadVector3();
			float progress = save.ReadFloat();
			FlyStage stage = (FlyStage)save.ReadInt();
			SetFlyStage(stage, progress);
		}
		if (save.ReadBool())
		{
			tDeathJump = save.ReadFloat();
			deathJumpHeight = save.ReadFloat();
			deathJumpStart = save.ReadVector3();
			deathJumpEnd = save.ReadVector3();
		}
		if (save.version >= 10)
		{
			radiation = save.ReadFloat();
		}
		if (save.version >= 11)
		{
			deathTimer = save.ReadFloat();
		}
		statusEffects.Read(save);
		if (IsDead())
		{
			SetCorpseLayer();
			if (save.version >= 12)
			{
				deathCause = (DeathCause)save.ReadInt();
			}
			if (save.version >= 29)
			{
				corpsePollutionGroundId = save.ReadInt();
			}
		}
		if (save.version >= 22)
		{
			launchForce = save.ReadVector3();
			timeLaunched = save.ReadFloat();
			if (save.version >= 30)
			{
				returnToTrails = null;
				searchForTrailsAfterLanding = false;
				int num3 = save.ReadInt();
				if (num3 == -1)
				{
					searchForTrailsAfterLanding = true;
				}
				else if (num3 > 0)
				{
					returnToTrails = new List<Trail> { GameManager.instance.FindLink<Trail>(num3) };
				}
			}
		}
		if (save.version >= 25)
		{
			radiationDeathTimer = save.ReadFloat();
		}
		if (save.version >= 26)
		{
			dieOnImpact = save.ReadBool();
		}
		if (save.version >= 34)
		{
			electrolysedEnergy = save.ReadFloat();
			electrolysing = save.ReadBool();
		}
		if (save.version >= 35 && save.version < 38)
		{
			save.ReadBool();
		}
		if (save.version >= 74)
		{
			electrolyseTarget = save.ReadFloat();
		}
	}

	public virtual void LoadLinks()
	{
		if (currentTrail != null)
		{
			int num = currentActionPointLinkId;
			if (num > 0)
			{
				ConnectableObject connectableObject = GameManager.instance.FindLink<ConnectableObject>(num);
				if (connectableObject != null)
				{
					foreach (ActionPoint actionPoint in currentTrail.actionPoints)
					{
						if (actionPoint.connectableObject == connectableObject)
						{
							currentActionPoint = actionPoint;
							break;
						}
					}
				}
			}
			FillAntActionPoints();
			if (currentActionPoint != null && nextActionPoints.TryPeek(out var result) && result == currentActionPoint)
			{
				nextActionPoints.Dequeue();
			}
			if (actionPointProgress == -1f)
			{
				SetActionPointLookTarget();
			}
		}
		int num2 = flightLandPadLinkId;
		if (num2 > 0)
		{
			flightLandPad = GameManager.instance.FindLink<FlightPad>(num2);
		}
		if (data.exchangeTypes.Contains(ExchangeType.MINE) && currentActionPoint != null && currentActionPoint.exchangeType == ExchangeType.MINE && actionPointProgress >= 0f)
		{
			SetMining(_mining: true);
		}
	}

	public void DoUpdateSometimes(float dt_sometimes)
	{
		updateSometimes = true;
		dtSometimes = dt_sometimes;
	}

	public virtual void AntUpdate(float dt)
	{
		switch (moveState)
		{
		case MoveState.Disabled:
		case MoveState.DeadAndDisabled:
			CheckFellOffMap();
			break;
		case MoveState.Dead:
		case MoveState.DeadAndLaunched:
			energy += dt;
			if (energy > GetCorpseRotTime())
			{
				Delete();
			}
			CheckFellOffMap();
			break;
		default:
		{
			if (updateSometimes)
			{
				if (effectAreas != null)
				{
					foreach (EffectArea effectArea in effectAreas)
					{
						effectArea.UpdatePos(transform.position);
					}
				}
				if (!isHeadless)
				{
					UpdateEffectAreas();
				}
				foreach (Pickup carryingPickup in carryingPickups)
				{
					foreach (StatusEffect statusEffect in carryingPickup.data.statusEffects)
					{
						GainStatusEffect(statusEffect);
					}
				}
				if (deathTimer > 0f)
				{
					deathTimer -= dtSometimes;
					if (deathTimer <= 0f && !IsImmortal())
					{
						energy = 0f;
					}
				}
			}
			if (!IsImmortal() && energy >= 0f && !isHeadless)
			{
				energy -= dt * statusEffects.lifeDrainFactor;
			}
			if (!updateSometimes)
			{
				break;
			}
			AddRadiation(statusEffects.radiationChange * dtSometimes);
			if (radiationDeathTimer < GlobalValues.standard.radDeathTime)
			{
				radiationDeathTimer += statusEffects.radDeath * dtSometimes;
			}
			else if (MayDie())
			{
				if (moveState == MoveState.Launched)
				{
					Progress.antsRadExplodedWhileAirborn++;
				}
				Die(DeathCause.RADIATION);
			}
			if (energy < data.oldTime)
			{
				GainStatusEffect(StatusEffect.OLD);
			}
			if (currentTrail != null && currentTrail.trailType == TrailType.ELDER)
			{
				if (HasStatusEffect(StatusEffect.OLD))
				{
					GainStatusEffect(StatusEffect.ELDER_SPED);
				}
				else
				{
					GainStatusEffect(StatusEffect.ELDER_SLOWED);
				}
			}
			electrolyseTarget = 0f;
			float num = 0f;
			foreach (Pickup carryingPickup2 in carryingPickups)
			{
				if (carryingPickup2.data.CanElectrolyse(out var _data))
				{
					electrolyseTarget += _data.energyCost;
					num += GlobalValues.standard.electrolyseDecay;
				}
			}
			if (electrolysedEnergy >= electrolyseTarget)
			{
				List<(Pickup, FactoryRecipeData)> list = null;
				foreach (Pickup carryingPickup3 in carryingPickups)
				{
					if (carryingPickup3.data.CanElectrolyse(out var _data2))
					{
						if (list == null)
						{
							list = new List<(Pickup, FactoryRecipeData)>();
						}
						list.Add((carryingPickup3, _data2));
					}
				}
				if (list != null)
				{
					foreach (var item3 in list)
					{
						Pickup item = item3.Item1;
						FactoryRecipeData item2 = item3.Item2;
						Pickup pickup = GameManager.instance.SpawnPickup(item2.productPickups[0].type, item.transform.position, item.transform.rotation);
						ExchangePickup(ExchangeType.DELETE, item);
						DirectAddPickup(pickup);
						electrolysedEnergy -= item2.energyCost;
						electrolysing = false;
						Progress.AddPickupManufactured(pickup.type);
					}
				}
			}
			else if (!electrolysing)
			{
				electrolysedEnergy = Mathf.Clamp(electrolysedEnergy - num * dtSometimes, 0f, electrolyseTarget);
			}
			statusEffects.Process(dtSometimes);
			CheckFellOffMap();
			if (moveState == MoveState.Launched || isFlying || (currentTrail != null && currentTrail.IsInBuilding(out var owner) && owner is Bridge))
			{
				UpdateGroundCheck();
			}
			break;
		}
		}
		switch (moveState)
		{
		case MoveState.Normal:
		case MoveState.Waiting:
			if (isFlying)
			{
				FlyToLandPad(dt);
			}
			else if (currentActionPoint != null)
			{
				DoActionPoint(dt);
			}
			else if (currentTrail != null)
			{
				MoveOnTrail(dt);
				float num2 = GetSpeed() * statusEffects.speedFactor;
				if (num2 > TOP_SPEED)
				{
					TOP_SPEED = num2;
				}
			}
			else if (billboardWarning != null)
			{
				ClearBillboard();
				UpdateBillboard();
			}
			if (updateSometimes)
			{
				if (lastPosition != Vector3.zero)
				{
					velocity = ((transform.position - lastPosition) / dtSometimes).magnitude;
					if (anim != null)
					{
						anim.SetBool(ClickableObject.paramWalk, velocity != 0f);
						anim.SetFloat(ClickableObject.paramWalkSpeed, velocity * walkAnimationFactor / 100f);
					}
				}
				lastPosition = transform.position;
				if (isMining && currentActionPoint == null)
				{
					SetMining(_mining: false);
				}
			}
			if (energy <= 0f && !IsImmortal() && MayDie())
			{
				Die(DeathCause.OLD_AGE);
			}
			break;
		}
		updateSometimes = false;
	}

	private void CheckFellOffMap()
	{
		if (transform.position.y < -2000f)
		{
			if (!IsDead() && MayDie())
			{
				Die(DeathCause.FELL_OFF_MAP);
			}
			Delete();
		}
	}

	public void GainStatusEffect(StatusEffect effect)
	{
		if (!IsImmuneTo(effect))
		{
			statusEffects.Gain(effect);
		}
	}

	public void ClearStatusEffects()
	{
		statusEffects.Clear();
		UpdateMaterial();
	}

	public void GainEffectArea(StatusEffect effect, float radius)
	{
		if (effectAreas == null)
		{
			effectAreas = new List<EffectArea>();
		}
		EffectArea effectArea = new EffectArea(effect, antRadius + radius);
		effectArea.SetActive(moveState != MoveState.Disabled && moveState != MoveState.Dead && moveState != MoveState.DeadAndLaunched && moveState != MoveState.DeadAndDisabled, force: true);
		effectAreas.Add(effectArea);
	}

	public void LoseEffectArea(StatusEffect effect)
	{
		if (effectAreas == null)
		{
			return;
		}
		for (int num = effectAreas.Count - 1; num >= 0; num--)
		{
			EffectArea effectArea = effectAreas[num];
			if (effectArea.statusEffect == effect)
			{
				effectArea.SetActive(_active: false);
				effectAreas.RemoveAt(num);
			}
		}
		if (effectAreas.Count == 0)
		{
			effectAreas = null;
		}
	}

	private void UpdateEffectAreas()
	{
		Vector3 position = transform.position;
		int num = 0;
		int num2 = EffectArea.effectAreaInfos.Length;
		for (int i = 0; i < num2; i++)
		{
			EffectAreaInfo effectAreaInfo = EffectArea.effectAreaInfos[i];
			float num3 = position.x - effectAreaInfo.x;
			float num4 = position.z - effectAreaInfo.z;
			if (num3 * num3 + num4 * num4 < effectAreaInfo.radiusSq)
			{
				num |= effectAreaInfo.effectBit;
				if (effectAreaInfo.antReaction != null)
				{
					effectAreaInfo.antReaction(this);
				}
			}
		}
		statusEffects.ApplyAreaEffectBits(num & vulnerabilityBits);
	}

	public bool IsImmuneTo(StatusEffect effect)
	{
		return (vulnerabilityBits & (1 << (int)effect)) == 0;
	}

	public bool AddImmunity(int effect_bits)
	{
		int num = vulnerabilityBits & ~effect_bits;
		if (num == vulnerabilityBits)
		{
			return false;
		}
		vulnerabilityBits = num;
		return true;
	}

	public bool HasStatusEffect(StatusEffect _effect)
	{
		return statusEffects.currentEffects.Contains(_effect);
	}

	public void AddRadiation(float d)
	{
		if (IsDead())
		{
			return;
		}
		int num = Mathf.FloorToInt(radiation);
		radiation += d;
		if (radiation < 0f)
		{
			radiation = 0f;
			if (num > 0)
			{
				SetRadiationStatusEffect(0, num);
			}
		}
		else if (radiation >= 4f)
		{
			radiation = 3.99f;
			if (num < 3)
			{
				SetRadiationStatusEffect(3, num);
			}
		}
		else if (num != Mathf.FloorToInt(radiation))
		{
			SetRadiationStatusEffect(Mathf.FloorToInt(radiation), num);
		}
	}

	private void SetRadiationStatusEffect(int r, int r_prev)
	{
		if (r_prev != 0)
		{
			statusEffects.Lose((StatusEffect)(1 + (r_prev - 1)));
		}
		if (r != 0)
		{
			StatusEffect effect = (StatusEffect)(1 + (r - 1));
			if (!IsImmuneTo(effect))
			{
				statusEffects.Gain(effect);
			}
		}
	}

	public void AddElectrolyse(float amount)
	{
		electrolysedEnergy = Mathf.Clamp(electrolysedEnergy + amount, 0f, electrolyseTarget);
		electrolysing = true;
	}

	public void StopElectrolysing()
	{
		electrolysing = false;
	}

	public void CopyElectrolysing(float _energy, float _target)
	{
		electrolysedEnergy = _energy;
		electrolyseTarget = _target;
		if (_energy > 0f)
		{
			electrolysing = true;
		}
	}

	public virtual void Die(DeathCause _cause)
	{
		ClearBillboard();
		UpdateBillboard();
		bool flag = false;
		deathCause = _cause;
		if (statusEffects.deathExplosion != ExplosionType.NONE)
		{
			GameManager.instance.SpawnExplosion(statusEffects.deathExplosion, transform.position.TargetYPosition(transform.position.y + GetHeight() / 2f)).Init();
			flag = true;
		}
		statusEffects.Clear();
		if (deathEffect != ExplosionType.NONE && statusEffects.deathExplosion == ExplosionType.NONE && (deathCause == DeathCause.OLD_AGE || deathCause == DeathCause.DEBUG_DEATH))
		{
			Vector3 position = transform.position;
			if (deathEffectPos != null)
			{
				position = deathEffectPos.position;
			}
			GameManager.instance.SpawnExplosion(deathEffect, position).Init();
		}
		Ant ant = null;
		if (!flag && data.deathSpawn != AntCaste.NONE && (deathCause == DeathCause.OLD_AGE || deathCause == DeathCause.DEBUG_DEATH))
		{
			ant = GameManager.instance.SpawnAnt(data.deathSpawn, transform.position, transform.rotation);
			if (currentTrail != null && currentTrail.trailType == TrailType.COMMAND)
			{
				currentTrail.ChangeOwner(ant);
			}
			ant.SetCurrentTrail(currentTrail);
			if (moveState == MoveState.Launched)
			{
				ant.ContinueLaunch(transform.position, launchForce, timeLaunched, returnToTrails, searchForTrailsAfterLanding);
			}
			ant.CopyElectrolysing(electrolysedEnergy, electrolyseTarget);
			List<TrailGate_Link> list = new List<TrailGate_Link>();
			foreach (TrailGate_Link linkedGate in GameManager.instance.GetLinkedGates(this))
			{
				if (!list.Contains(linkedGate))
				{
					list.Add(linkedGate);
				}
			}
			foreach (TrailGate_Link item in list)
			{
				item.Assign(ant);
			}
			flag = true;
		}
		GetOnNewTrail(null);
		if (ant != null)
		{
			foreach (Pickup item2 in new List<Pickup>(carryingPickups))
			{
				ant.DirectAddPickup(item2);
			}
			carryingPickups.Clear();
		}
		else
		{
			SendPickupsToInventory();
			if (currentFlyStage >= FlyStage.LiftOff && currentFlyStage <= FlyStage.Land)
			{
				DeleteCarryingPickups();
			}
			else
			{
				DropPickupsOnGround();
			}
		}
		energy = 0f;
		if (!flag)
		{
			PlayAudioShort(WorldSfx.AntDie);
		}
		ClearAntAudio();
		if (!flag)
		{
			if (moveState != MoveState.Launched)
			{
				GetLaunchCollider();
				Vector3 eulerAngles = launchTriggerArea.jumpAngle.rotation.eulerAngles;
				eulerAngles.y = UnityEngine.Random.Range(0f, 360f);
				launchTriggerArea.jumpAngle.rotation = Quaternion.Euler(eulerAngles);
				StartLaunch(launchTriggerArea.jumpAngle.forward * 50f, LaunchCause.DEATH);
			}
			else
			{
				SetMoveState(MoveState.DeadAndLaunched);
			}
		}
		else
		{
			SetMoveState(MoveState.Dead);
		}
		UpdateMaterial();
		SetCorpseLayer();
		if (ant != null && Gameplay.instance.IsSelected(this))
		{
			Gameplay.instance.Select(ant);
		}
		Gameplay.instance.ClearIfSelected(this);
		GameManager.instance.UpdateAntCount();
		GameManager.instance.StartCheckLinkGates();
		if (ant == null)
		{
			History.RegisterAntEnd(this, repurposed: false);
		}
		if (flag)
		{
			Delete();
		}
	}

	public override void OnClickDelete()
	{
		if (DebugSettings.standard.DeletableEverything() && MayDie())
		{
			Die(DeathCause.DEBUG_DEATH);
		}
	}

	protected override void DoDelete()
	{
		DropPickupsOnGround();
		GetOnNewTrail(null);
		if (effectAreas != null)
		{
			foreach (EffectArea effectArea in effectAreas)
			{
				effectArea.SetActive(_active: false);
			}
		}
		ClearAntAudio();
		ClearGround();
		for (int i = 0; i < particleSystems.Length; i++)
		{
			GameManager.instance.RemovePausableParticles(particleSystems[i]);
		}
		GameManager.instance.DeleteAnt(this);
		base.DoDelete();
	}

	public bool IsImmortal()
	{
		return isImmortal;
	}

	private bool MayDie()
	{
		if (currentActionPoint != null && currentActionPoint.exchangeType != ExchangeType.MINE && currentActionPoint.exchangeType != ExchangeType.ENTER)
		{
			return false;
		}
		if (moveState == MoveState.Normal || moveState == MoveState.Launched)
		{
			return !isHeadless;
		}
		return false;
	}

	public virtual bool IsDead()
	{
		if (moveState != MoveState.Dead && moveState != MoveState.DeadAndLaunched)
		{
			return moveState == MoveState.DeadAndDisabled;
		}
		return true;
	}

	protected virtual float GetCorpseRotTime()
	{
		return GlobalValues.standard.corpseRotTime;
	}

	public float GetRemainingLife()
	{
		if (IsImmortal())
		{
			return 1f;
		}
		return Mathf.Clamp01(energy / energyTotal);
	}

	public virtual bool CanGetCommand()
	{
		if (IsDead() || !Progress.HasUnlocked(TrailType.COMMAND) || isFlying)
		{
			return false;
		}
		if (moveState != MoveState.Normal && moveState != MoveState.Waiting)
		{
			return false;
		}
		if (currentTrail != null)
		{
			if (currentTrail.IsBuilding())
			{
				return waitingAtEnd;
			}
			return true;
		}
		return true;
	}

	public void SetTotalEnergy(float e)
	{
		energyTotal = (energy = e);
	}

	public void SetColliders(bool target)
	{
		if (cols == null)
		{
			cols = GetComponentsInChildren<Collider>();
		}
		Collider[] array = cols;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = target;
		}
	}

	public bool AreCollidersDisabled()
	{
		if (cols == null || cols.Length == 0)
		{
			return false;
		}
		return !cols[0].enabled;
	}

	private void SetCorpseLayer()
	{
		base.gameObject.SetLayerRecursive(28);
	}

	public void SetMoveState(MoveState state)
	{
		moveState = state;
		this.SetObActive(state != MoveState.Disabled && state != MoveState.DeadAndDisabled);
		if (effectAreas != null)
		{
			foreach (EffectArea effectArea in effectAreas)
			{
				effectArea.SetActive(state != MoveState.Disabled && state != MoveState.Dead && state != MoveState.DeadAndLaunched && state != MoveState.DeadAndDisabled);
			}
		}
		if (anim != null)
		{
			anim.SetBool(ClickableObject.paramDie, state == MoveState.Dead || state == MoveState.DeadAndLaunched);
			anim.SetBool(ClickableObject.paramLaunched, state == MoveState.Launched);
			if (state != MoveState.Normal)
			{
				anim.SetBool(ClickableObject.paramWalk, value: false);
			}
		}
		if (minimapMesh == null)
		{
			Debug.LogWarning(base.name + ": Missing minimap mesh!");
		}
		else
		{
			minimapMesh.SetObActive(state != MoveState.Dead && state != MoveState.DeadAndLaunched && state != MoveState.Disabled && state != MoveState.DeadAndDisabled);
		}
	}

	protected virtual void MoveOnTrail(float dt)
	{
		float progress = trailProgress;
		if (moveState != MoveState.Waiting)
		{
			progress += dt * progressSpeed * statusEffects.speedFactor;
		}
		string warning;
		bool flag = CanContinueOnTrail(ref progress, out warning);
		trailProgress = progress;
		if ((flag || waitingAtEnd) && !statusEffects.blockActionPoints)
		{
			ActionPoint result;
			while (nextActionPoints.TryPeek(out result) && (waitingAtEnd || trailProgress > result.trailProgress - 0.5f / currentTrail.length))
			{
				nextActionPoints.Dequeue();
				if (CanDoActionPoint(result, out var _))
				{
					if (!waitingAtEnd)
					{
						trailProgress = result.trailProgress;
					}
					SetActionPoint(result);
					return;
				}
			}
		}
		if (flag)
		{
			ClearBillboard();
			UpdateBillboard();
			if (trailProgress > 1f)
			{
				Trail trail = currentTrail;
				string warning2;
				Trail trail2 = ChooseNextTrail(final: true, out warning2);
				if (trail2 == null)
				{
					ResetNextTrail();
					trailProgress = 1f;
					return;
				}
				float num = (trailProgress - 1f) * trail.length;
				if (num > 0.45f)
				{
					num = 0.45f;
				}
				SetCurrentTrail(trail2, num / trail2.length);
				trail2.DoEnterGate(this);
				if (trail.IsInBuilding(out var owner) && !trail2.IsInBuilding() && owner is Bridge)
				{
					CheckGround();
				}
			}
		}
		else if (warning != "" && billboardWarning != warning)
		{
			billboardWarning = warning;
			UpdateBillboard();
		}
		if (!(currentTrail == null))
		{
			Vector3 posStart = currentTrail.posStart;
			Vector3 posEnd = currentTrail.posEnd;
			transform.position = posStart + trailProgress * (posEnd - posStart);
			if (Vector3.Dot(transform.forward, currentTrail.direction) < 0.999f)
			{
				RotateTowards(currentTrail.direction, dt);
			}
			if (currentTrail.trailType == TrailType.COMMAND && flag && !keepCommandTrail)
			{
				currentTrail.SetStartPos(transform.position, only_visual: true);
			}
		}
	}

	public void ResetNextTrail()
	{
		assumedNextTrailTime = 0f;
		nextTrail = null;
	}

	protected virtual bool CanContinueOnTrail(ref float progress, out string warning)
	{
		warning = "";
		float num = 1f / currentTrail.length;
		float num2 = 4f * num;
		if (IsDead())
		{
			progress = trailProgress;
			return false;
		}
		if (nextTrail == null)
		{
			TrailType trailType = currentTrail.trailType;
			if (progress > 1f - num2 && assumedNextTrailTime < Time.realtimeSinceStartup - 1f && (trailType != TrailType.COMMAND || assumedNextTrailTime == 0f))
			{
				nextTrail = ChooseNextTrail(final: false, out warning);
				assumedNextTrailTime = Time.realtimeSinceStartup;
			}
			if (nextTrail == null)
			{
				float num3 = ((trailType == TrailType.COMMAND || trailType == TrailType.MINING || trailType.IsBuildingTrail()) ? 1f : (1f - num2));
				if (progress > num3)
				{
					progress = num3;
					if (isHeadless)
					{
						SetCurrentTrail(null);
					}
					else
					{
						if (trailType == TrailType.COMMAND && !Gameplay.instance.IsDrawingCommandTrail(this))
						{
							SetOnNearbyTrail(1f, currentTrail.posEnd, should_wait_at_gate: true);
							progress = trailProgress;
							return false;
						}
						waitingAtEnd = true;
					}
					return false;
				}
			}
		}
		waitingAtEnd = false;
		foreach (Ant currentAnt in currentTrail.currentAnts)
		{
			if (!(currentAnt == this) && !(currentAnt.trailProgress < progress) && currentAnt.trailProgress < progress + (GetAntGap(currentAnt) + antRadius + currentAnt.antRadius) * num)
			{
				progress = trailProgress;
				return false;
			}
		}
		if (nextTrail != null && progress > 1f - (2f + antRadius) * num)
		{
			foreach (Ant currentAnt2 in nextTrail.currentAnts)
			{
				float num4 = (GetAntGap(currentAnt2) + antRadius + currentAnt2.antRadius - (1f - progress) * currentTrail.length) / nextTrail.length;
				if (currentAnt2.trailProgress < num4)
				{
					progress = trailProgress;
					return false;
				}
			}
		}
		return true;
	}

	protected virtual float GetAntGap(Ant ant)
	{
		return 2f;
	}

	protected virtual void SetOnNearbyTrail(float range, Vector3 check_pos, bool should_wait_at_gate)
	{
		bool flag = false;
		foreach (Trail item in Toolkit.EFindTrailsNear(check_pos, range))
		{
			if (!item.IsUsableFor(this))
			{
				continue;
			}
			float progressNear = item.GetProgressNear(check_pos);
			if (item.IsGate() && (progressNear < 0.001f || item.IsBuilding()) && !item.CheckIfTrailGateSatisfied(this, final: true, out var _))
			{
				flag = should_wait_at_gate;
				continue;
			}
			if (item.trailType == TrailType.DIVIDER && progressNear < 0.001f)
			{
				item.splitStart.DividerChoose(out var next_trail, update_count: true);
				SetCurrentTrail(next_trail, progressNear);
			}
			else
			{
				SetCurrentTrail(item, progressNear);
			}
			return;
		}
		if (flag)
		{
			waitingAtEnd = true;
		}
		else
		{
			SetCurrentTrail(null);
		}
	}

	public void GetOnNewTrail(Trail trail)
	{
		ClearActionPoints();
		if (trail != null && trail.IsCommandTrail())
		{
			trail.NewStartSplit(transform.position);
		}
		SetCurrentTrail(trail);
	}

	public virtual void SetCurrentTrail(Trail _trail, float progress = float.MinValue)
	{
		if (_trail == currentTrail)
		{
			return;
		}
		Trail trail = null;
		if (currentTrail != null)
		{
			currentTrail.ExitTrail(this);
			if (currentTrail.trailType == TrailType.COMMAND && currentTrail.owner == this)
			{
				trail = currentTrail;
			}
		}
		if (IsDead() && _trail != null)
		{
			Debug.LogWarning(base.name + " tried to get on trail while dead, shouldn't happen");
			_trail = null;
		}
		currentTrail = _trail;
		ResetNextTrail();
		if (_trail != null)
		{
			_trail.EnterTrail(this);
			trailProgress = ((progress == float.MinValue) ? _trail.GetProgressNear(transform.position) : progress);
			progressSpeed = GetSpeed() / _trail.length;
			FillAntActionPoints();
		}
		else
		{
			ClearActionPoints();
		}
		if (trail != null)
		{
			trail.DeleteCommandTrailTail();
		}
	}

	public void CommandTrailUpdated()
	{
		currentTrail.SetStartPos(transform.position);
		progressSpeed = GetSpeed() / currentTrail.length;
		trailProgress = 0f;
	}

	public void RefreshAntActionPoints()
	{
		FillAntActionPoints();
	}

	public void ClearActionPoints()
	{
		nextActionPoints.Clear();
		currentActionPoint = null;
	}

	private void FillAntActionPoints()
	{
		nextActionPoints.Clear();
		float num = 0.5f / currentTrail.length;
		currentTrail.actionPoints.Sort((ActionPoint a, ActionPoint b) => a.trailProgress.CompareTo(b.trailProgress));
		foreach (ActionPoint actionPoint in currentTrail.actionPoints)
		{
			if (actionPoint.trailProgress > trailProgress - num)
			{
				nextActionPoints.Enqueue(actionPoint);
			}
		}
		Split splitEnd = currentTrail.splitEnd;
		foreach (Trail item in currentTrail.splitEnd.ENonLogicTrails(currentTrail))
		{
			if (!(item.splitEnd == splitEnd))
			{
				continue;
			}
			float num2 = 1f - 1.2f / item.length;
			foreach (ActionPoint actionPoint2 in item.actionPoints)
			{
				if (actionPoint2.trailProgress > num2)
				{
					nextActionPoints.Enqueue(actionPoint2);
				}
			}
		}
	}

	public void RemoveActionPoint(ActionPoint delete_ap)
	{
		nextActionPoints.Remove(delete_ap);
	}

	protected virtual Trail ChooseNextTrail(bool final, out string warning)
	{
		warning = "";
		List<Trail> list = new List<Trail>();
		List<Trail> list2 = new List<Trail>();
		List<Trail> list3 = new List<Trail>();
		Split splitEnd = currentTrail.splitEnd;
		foreach (Trail connectedTrail in splitEnd.connectedTrails)
		{
			if (connectedTrail.splitStart != splitEnd)
			{
				continue;
			}
			switch (connectedTrail.trailType)
			{
			case TrailType.GATE_SENSORS:
			case TrailType.GATE_COUNTER:
			case TrailType.GATE_LIFE:
			case TrailType.GATE_CARRY:
			case TrailType.GATE_CASTE:
			case TrailType.GATE_OLD:
			case TrailType.GATE_SPEED:
			case TrailType.GATE_TIMER:
			case TrailType.GATE_STOCKPILE:
			case TrailType.GATE_LINK:
				list2.Add(connectedTrail);
				break;
			case TrailType.IN_BUILDING_GATE:
				list3.Add(connectedTrail);
				break;
			case TrailType.GATE_COUNTER_END:
				if (connectedTrail.IsStartOfChain())
				{
					list2.Add(connectedTrail);
				}
				else
				{
					list.Add(connectedTrail);
				}
				break;
			default:
				list.Add(connectedTrail);
				break;
			case TrailType.DIVIDER:
				break;
			}
		}
		List<Trail> list4 = new List<Trail>();
		if (list4.Count == 0)
		{
			foreach (Trail item in list2)
			{
				if (item.IsUsableFor(this) && item.CheckIfTrailGateSatisfied(this, final, out warning))
				{
					list4.Add(item);
				}
			}
		}
		if (list4.Count == 0)
		{
			foreach (Trail item2 in list3)
			{
				if (item2.IsUsableFor(this) && item2.CheckIfTrailGateSatisfied(this, final, out warning))
				{
					list4.Add(item2);
				}
			}
		}
		if (list4.Count == 0 && currentTrail.splitEnd.DividerChoose(out var next_trail, final))
		{
			list4.Add(next_trail);
		}
		if (list4.Count == 0)
		{
			foreach (Trail item3 in list)
			{
				if (item3.IsUsableFor(this) && item3 != currentTrail && item3.CheckIfTrailGateSatisfied(this, final, out warning))
				{
					list4.Add(item3);
				}
			}
		}
		if (list4.Count == 0)
		{
			return null;
		}
		warning = "";
		if (list4.Count == 1)
		{
			return list4[0];
		}
		Trail result = null;
		float num = float.PositiveInfinity;
		Vector3 direction = currentTrail.direction;
		for (int i = 0; i < list4.Count; i++)
		{
			float num2 = Mathf.Abs(Vector3.Angle(direction, list4[i].direction));
			if (num2 < num)
			{
				num = num2;
				result = list4[i];
			}
		}
		return result;
	}

	public virtual float GetSpeed()
	{
		if (IsDead())
		{
			return 0f;
		}
		return speedMove;
	}

	public float GetSpeedFactor()
	{
		return statusEffects.speedFactor;
	}

	protected float MoveTowards(Vector3 targetPos, float dt)
	{
		Vector3 vector = targetPos.FloorPosition(transform.position) - transform.position;
		float magnitude = vector.magnitude;
		if (magnitude == 0f)
		{
			return 0f;
		}
		float num = GetSpeed() * statusEffects.speedFactor * dt;
		if (num > magnitude)
		{
			num = magnitude;
		}
		transform.position += vector / magnitude * num;
		RotateTowards(vector, dt);
		return num;
	}

	protected virtual void RotateTowards(Vector3 dir, float dt)
	{
		if (dir != Vector3.zero)
		{
			RotateTowards(Quaternion.LookRotation(dir), dt);
		}
	}

	protected void RotateTowards(Quaternion target_rot, float dt)
	{
		transform.rotation = Quaternion.Lerp(transform.rotation, target_rot, speedRotate * dt * statusEffects.speedFactor);
	}

	private void SetActionPoint(ActionPoint action_point)
	{
		currentActionPoint = action_point;
		actionPointProgress = -1f;
		SetActionPointLookTarget();
	}

	private void SetActionPointLookTarget()
	{
		if (currentActionPoint == null)
		{
			return;
		}
		if (currentActionPoint.connectableObject is PickupContainer pickupContainer)
		{
			switch (currentActionPoint.exchangeType)
			{
			case ExchangeType.BUILDING_IN:
			case ExchangeType.BUILDING_PROCESS:
			case ExchangeType.BUILD:
				actionPointLookTarget = pickupContainer.GetInsertPos();
				break;
			case ExchangeType.BUILDING_OUT:
			case ExchangeType.FORAGE:
			case ExchangeType.PLANT_CUT:
				actionPointLookTarget = pickupContainer.GetExtractPos();
				break;
			case ExchangeType.INSERT:
			case ExchangeType.EXTRACT:
			case ExchangeType.MINE:
			case ExchangeType.EXTRACT_INSTANT:
				actionPointLookTarget = currentActionPoint.actionTrail.posEnd.TransformYPosition(transform);
				break;
			default:
				actionPointLookTarget = pickupContainer.transform.position;
				break;
			}
		}
		else
		{
			actionPointLookTarget = currentActionPoint.actionTrail.posEnd.TransformYPosition(transform);
		}
	}

	private void DoActionPoint(float dt)
	{
		if (currentTrail == null)
		{
			ClearActionPoints();
		}
		else if (actionPointProgress == -1f)
		{
			ConnectableObject connectedObject = currentActionPoint.GetConnectedObject();
			if (connectedObject == null)
			{
				actionPointProgress = -2f;
				return;
			}
			bool flag = false;
			if (currentActionPoint.exchangeType == ExchangeType.ENTER)
			{
				flag = true;
			}
			else
			{
				Vector3 vector = (actionPointLookTarget - transform.position).SetY(0f);
				Quaternion quaternion = ((vector == Vector3.zero) ? transform.rotation : Quaternion.LookRotation(vector));
				if (Quaternion.Angle(transform.rotation, quaternion) > 5f)
				{
					RotateTowards(quaternion, dt);
				}
				else
				{
					transform.rotation = quaternion;
					flag = true;
				}
			}
			if (!flag)
			{
				return;
			}
			if (!CanDoActionPoint(currentActionPoint, out var need_to_wait))
			{
				actionPointProgress = 0f;
				return;
			}
			if (need_to_wait)
			{
				actionPointProgress = -1f;
				return;
			}
			actionPointProgress = 0f;
			switch (currentActionPoint.exchangeType)
			{
			case ExchangeType.BUILDING_PROCESS:
			{
				if (CanDoExchange(currentActionPoint.GetConnectedObject(), ExchangeType.BUILDING_IN, currentActionPoint.GetExchangePoint(), out var _))
				{
					actionPointProgress = StartExchangePickup(currentActionPoint.GetConnectedObject(), ExchangeType.BUILDING_IN);
				}
				break;
			}
			case ExchangeType.ENTER:
			{
				Ant ant = ((this is CargoAnt cargoAnt) ? cargoAnt.centipedeHead : this);
				Building building = (Building)connectedObject;
				if (building.TryUseBuilding(currentActionPoint.GetEntranceN(), ant))
				{
					actionPointProgress = building.UseBuilding(currentActionPoint.GetEntranceN(), ant, out var ant_entered);
					if (ant_entered)
					{
						currentActionPoint = null;
					}
				}
				else
				{
					actionPointProgress = -1f;
				}
				break;
			}
			case ExchangeType.MINE:
				if (connectedObject is BiomeObject biomeObject)
				{
					SetMining(_mining: true);
					actionPointProgress = durationMineWindUp + biomeObject.GetMineDuration(data.mineSpeed);
				}
				else
				{
					actionPointProgress = -2f;
				}
				break;
			default:
				actionPointProgress = Mathf.Max(MinExchangeTime(), StartExchangePickup(currentActionPoint.connectableObject, currentActionPoint.exchangeType));
				break;
			}
		}
		else if (actionPointProgress >= 0f)
		{
			if (currentActionPoint == null || currentActionPoint.GetConnectedObject() == null)
			{
				actionPointProgress = -2f;
			}
			bool let_ant_wait = false;
			actionPointProgress = Mathf.Clamp(actionPointProgress - dt, 0f, float.MaxValue);
			if (actionPointProgress != 0f)
			{
				return;
			}
			switch (currentActionPoint.exchangeType)
			{
			case ExchangeType.BUILDING_PROCESS:
				if (currentActionPoint.GetConnectedObject() is PickupContainer pickupContainer && !IsFull() && pickupContainer.CanExtract(ExchangeType.BUILDING_PROCESS, ref let_ant_wait, show_billboard: true))
				{
					actionPointProgress = StartExchangePickup(currentActionPoint.connectableObject, ExchangeType.BUILDING_OUT);
					let_ant_wait = true;
				}
				break;
			case ExchangeType.MINE:
				if (currentActionPoint.GetConnectedObject() is BiomeObject biomeObject2)
				{
					StartExchangePickup(biomeObject2, ExchangeType.MINE);
					if (data.mineForever)
					{
						actionPointProgress = biomeObject2.GetMineDuration(data.mineSpeed);
						let_ant_wait = true;
					}
				}
				break;
			}
			if (!let_ant_wait)
			{
				actionPointProgress = -2f;
			}
		}
		else if (actionPointProgress == -2f)
		{
			SetMining(_mining: false);
			bool flag2 = false;
			Vector3 vector2 = ((currentTrail == null) ? Vector3.zero : currentTrail.direction);
			Quaternion quaternion2 = ((vector2 == Vector3.zero) ? transform.rotation : Quaternion.LookRotation(vector2));
			if (Quaternion.Angle(transform.rotation, quaternion2) > 5f)
			{
				RotateTowards(quaternion2, dt);
			}
			else
			{
				flag2 = true;
				transform.rotation = quaternion2;
			}
			if (flag2)
			{
				currentActionPoint = null;
			}
		}
	}

	protected virtual float MinExchangeTime()
	{
		return 0.3f;
	}

	private void SetMining(bool _mining)
	{
		if (isMining != _mining)
		{
			isMining = _mining;
			if (anim != null)
			{
				anim.SetBool(ClickableObject.paramMine, isMining);
			}
			if (isMining)
			{
				StartLoopAudio(AudioLinks.standard.antMineLoop, AudioLinks.standard.antMineLoopDelay);
			}
			else
			{
				StopAudio();
			}
		}
	}

	public void StartFlying(Vector3 target_pos, FlightPad land_pad)
	{
		SetCurrentTrail(null);
		isFlying = true;
		flightStart = transform.position;
		flightTarget = target_pos;
		if (land_pad != null)
		{
			flightLandPad = land_pad;
			landPadRelocation = flightLandPad.relocation;
		}
		Vector3 vector = Toolkit.LookVectorNormalized(flightStart, flightTarget);
		if (Vector3.Distance(flightStart, flightTarget) < GlobalValues.standard.flightWindUpDownDistance * 2f)
		{
			Vector3 vector2 = (flightStart + flightTarget) / 2f;
			vector2.y += GlobalValues.standard.flightHeight;
			flightHeightStart = (flightHeightEnd = vector2 + UnityEngine.Random.insideUnitSphere * GlobalValues.standard.flightRandomRadius);
		}
		else
		{
			flightHeightStart = flightStart + vector * GlobalValues.standard.flightWindUpDownDistance + UnityEngine.Random.insideUnitSphere * GlobalValues.standard.flightRandomRadius;
			flightHeightStart.y += GlobalValues.standard.flightHeight;
			flightHeightEnd = flightTarget - vector * GlobalValues.standard.flightWindUpDownDistance + UnityEngine.Random.insideUnitSphere * GlobalValues.standard.flightRandomRadius;
			flightHeightEnd.y += GlobalValues.standard.flightHeight;
		}
		SetFlyStage(FlyStage.LiftOff);
	}

	private void FlyToLandPad(float dt)
	{
		flightProgress += dt * progressSpeed * statusEffects.speedFactor * data.flightSpeed;
		Vector3 dir = Vector3.zero;
		if (DebugSettings.standard.showFlightLines)
		{
			for (int i = 0; i < 3; i++)
			{
				Vector3 zero = Vector3.zero;
				Vector3 zero2 = Vector3.zero;
				AnimationCurve y_curve = null;
				switch (i)
				{
				case 0:
					zero = flightStart;
					zero2 = flightHeightStart;
					y_curve = GlobalValues.standard.curveSIn;
					break;
				case 1:
					zero = flightHeightStart;
					zero2 = flightHeightEnd;
					break;
				case 2:
					zero = flightHeightEnd;
					zero2 = flightTarget;
					y_curve = GlobalValues.standard.curveSOut;
					break;
				}
				float num = Vector3.Distance(flightStart, flightHeightStart) * 10f;
				for (int j = 0; (float)j < num; j++)
				{
					if (!((float)j >= num - 1f))
					{
						float progress = (float)j / num;
						Vector3 pointInFlight = GetPointInFlight(zero, zero2, progress, y_curve);
						progress = (((float)j >= num - 2f) ? 1f : ((float)(j + 1) / num));
						Vector3 pointInFlight2 = GetPointInFlight(zero, zero2, progress, y_curve);
						Debug.DrawLine(pointInFlight, pointInFlight2, Color.white);
					}
				}
			}
		}
		if (flightLandPad != null && landPadRelocation < flightLandPad.relocation)
		{
			landPadRelocation = flightLandPad.relocation;
			OnLandPadRelocation();
		}
		switch (currentFlyStage)
		{
		case FlyStage.LiftOff:
			dir = (flightHeightStart - flightStart).SetY(0f);
			if (anim != null)
			{
				anim.SetBool(ClickableObject.paramFly, value: true);
			}
			if (flightProgress > 1f)
			{
				SetFlyStage(FlyStage.GainHeight);
			}
			break;
		case FlyStage.GainHeight:
			transform.position = GetPointInFlight(flightStart, flightHeightStart, flightProgress, GlobalValues.standard.curveSIn);
			dir = transform.position - flightLastPos;
			flightLastPos = transform.position;
			if (anim != null)
			{
				anim.SetBool(ClickableObject.paramFly, value: true);
			}
			if (flightProgress > 1f)
			{
				transform.position = flightHeightStart;
				if (flightHeightStart == flightHeightEnd)
				{
					SetFlyStage(FlyStage.LoseHeight);
				}
				else
				{
					SetFlyStage(FlyStage.FlyTowards);
				}
			}
			break;
		case FlyStage.FlyTowards:
			transform.position = GetPointInFlight(flightHeightStart, flightHeightEnd, flightProgress);
			dir = transform.position - flightLastPos;
			flightLastPos = transform.position;
			if (anim != null)
			{
				anim.SetBool(ClickableObject.paramFly, value: true);
			}
			if (flightProgress > 1f)
			{
				transform.position = flightHeightEnd;
				SetFlyStage(FlyStage.LoseHeight);
			}
			break;
		case FlyStage.LoseHeight:
			transform.position = GetPointInFlight(flightHeightEnd, flightTarget, flightProgress, GlobalValues.standard.curveSOut);
			dir = transform.position - flightLastPos;
			flightLastPos = transform.position;
			if (anim != null)
			{
				anim.SetBool(ClickableObject.paramFly, value: true);
			}
			if (flightProgress > 1f)
			{
				transform.position = flightTarget;
				SetFlyStage(FlyStage.Land);
			}
			break;
		case FlyStage.Land:
			dir = (flightTarget - flightHeightEnd).SetY(0f);
			if (anim != null)
			{
				anim.SetBool(ClickableObject.paramFly, value: false);
			}
			if (flightProgress > 1f)
			{
				isFlying = false;
				if (flightLandPad != null)
				{
					flightLandPad.LandOnPad(this);
					flightLandPad = null;
					CheckGround();
				}
			}
			break;
		}
		RotateTowards(dir, dt);
	}

	private Vector3 GetPointInFlight(Vector3 start, Vector3 end, float progress, AnimationCurve y_curve = null)
	{
		Vector3 result = start * (1f - progress) + end * progress;
		if (y_curve != null)
		{
			result.y = start.y * y_curve.Evaluate(1f - progress) + end.y * y_curve.Evaluate(progress);
		}
		return result;
	}

	private void SetFlyStage(FlyStage _stage, float _progress = 0f)
	{
		currentFlyStage = _stage;
		flightProgress = _progress;
		switch (currentFlyStage)
		{
		case FlyStage.LiftOff:
			StartLoopAudio(AudioLinks.standard.antFlyLoop);
			progressSpeed = 1f;
			break;
		case FlyStage.GainHeight:
			progressSpeed = GetFlySpeed() / Vector3.Distance(flightStart, flightHeightStart);
			break;
		case FlyStage.FlyTowards:
			progressSpeed = GetFlySpeed() / Vector3.Distance(flightHeightStart, flightHeightEnd);
			break;
		case FlyStage.LoseHeight:
			progressSpeed = GetFlySpeed() / Vector3.Distance(flightHeightEnd, flightTarget);
			break;
		case FlyStage.Land:
			StopAudio();
			progressSpeed = 1f;
			break;
		}
	}

	private void OnLandPadRelocation()
	{
		Vector3 position = flightLandPad.landPoint.position;
		if (currentFlyStage >= FlyStage.LoseHeight && (flightTarget.XZ() - position.XZ()).magnitude > 20f)
		{
			flightLandPad = null;
			return;
		}
		switch (currentFlyStage)
		{
		case FlyStage.FlyTowards:
			flightStart = (flightHeightStart = transform.position);
			flightProgress = 0f;
			progressSpeed = GetFlySpeed() / Vector3.Distance(flightHeightStart, flightHeightEnd);
			break;
		}
		flightTarget = position;
		Vector3 vector = Toolkit.LookVectorNormalized(flightStart, flightTarget);
		if (currentFlyStage < FlyStage.LoseHeight)
		{
			flightHeightEnd = flightTarget - vector * GlobalValues.standard.flightWindUpDownDistance;
			flightHeightEnd.y += GlobalValues.standard.flightHeight;
		}
	}

	private float GetFlySpeed()
	{
		return 20f;
	}

	public void StartLaunch(Vector3 force, LaunchCause launch_cause)
	{
		Trail trail = currentTrail;
		SetCurrentTrail(null);
		SetMining(_mining: false);
		GetLaunchCollider();
		launchTriggerArea.SetObActive(active: true);
		launchTriggerArea.ResetOverlap();
		launchForce = force;
		dieOnImpact = statusEffects.deathExplosion != ExplosionType.NONE;
		if (launch_cause == LaunchCause.LOADED)
		{
			return;
		}
		timeLaunched = 0f;
		returnToTrails = null;
		searchForTrailsAfterLanding = false;
		switch (launch_cause)
		{
		case LaunchCause.EXPLOSION:
			if (trail != null && !trail.IsCommandTrail())
			{
				returnToTrails = new List<Trail> { trail };
			}
			break;
		case LaunchCause.LAUNCHER:
			searchForTrailsAfterLanding = true;
			break;
		}
		SetMoveState((moveState == MoveState.Dead || launch_cause == LaunchCause.DEATH) ? MoveState.DeadAndLaunched : MoveState.Launched);
	}

	public void ContinueLaunch(Vector3 pos, Vector3 force, float time_launched, List<Trail> return_to_trails, bool search_trails_after_landing)
	{
		transform.position = pos;
		StartLaunch(force, LaunchCause.LOADED);
		timeLaunched = time_launched;
		returnToTrails = return_to_trails;
		searchForTrailsAfterLanding = search_trails_after_landing;
		SetMoveState(MoveState.Launched);
	}

	public void UpdateLaunch(Vector3 force)
	{
		launchTriggerArea.ResetOverlap();
		launchForce = force;
		timeLaunched = 0f;
	}

	private void GetLaunchCollider()
	{
		if (launchTriggerArea == null)
		{
			launchTriggerArea = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(AntLaunchCollider)), transform).GetComponent<AntLaunchCollider>();
		}
	}

	public void StopLaunch()
	{
		launchTriggerArea.SetObActive(active: false);
		Vector3 position = transform.position;
		position.y = 0f;
		transform.position = position;
		if (moveState == MoveState.DeadAndLaunched)
		{
			SetMoveState(MoveState.Dead);
		}
		else
		{
			SetMoveState(MoveState.Normal);
		}
		CheckGround();
		if (launchOrigin != null && currentGround != null)
		{
			if (currentGround != launchOrigin.ground)
			{
				Progress.AddBiomeHitFromLaunch(currentGround.biome.biomeType);
			}
			launchOrigin = null;
		}
	}

	public void LaunchingFixedUpdate(float xdt)
	{
		if (timeLaunched > 0.05f)
		{
			bool flag = false;
			bool flag2 = false;
			List<Collider> list = new List<Collider>();
			foreach (Collider item in launchTriggerArea.EOverlapping())
			{
				switch (item.gameObject.layer)
				{
				case 14:
					flag = true;
					list.Add(item);
					break;
				case 24:
				{
					Plant componentInParent = item.gameObject.GetComponentInParent<Plant>();
					if (componentInParent == null || !componentInParent.data.trailsPassThrough)
					{
						flag2 = true;
						list.Add(item);
					}
					break;
				}
				default:
					flag2 = true;
					list.Add(item);
					break;
				case 8:
				case 10:
				case 15:
				case 17:
				case 22:
				case 25:
				case 26:
				case 28:
					break;
				}
			}
			if (dieOnImpact && (flag || flag2))
			{
				StopLaunch();
				Die((statusEffects.deathExplosion == ExplosionType.RADIATION_DEATH) ? DeathCause.RADIATION : DeathCause.IMPACT);
				return;
			}
			if (flag && !flag2)
			{
				StopLaunch();
				if (searchForTrailsAfterLanding)
				{
					searchForTrailsAfterLanding = false;
					returnToTrails = null;
					foreach (Trail item2 in Toolkit.EFindTrailsNear(transform.position, 8f))
					{
						if (returnToTrails == null)
						{
							returnToTrails = new List<Trail>();
						}
						returnToTrails.Add(item2);
					}
				}
				TryToReturn();
				return;
			}
			if (list.Count > 0)
			{
				Vector3 zero = Vector3.zero;
				foreach (Collider item3 in list)
				{
					zero += item3.ClosestPoint(launchTriggerArea.centerPoint.position);
				}
				zero /= (float)list.Count;
				Vector3 inNormal = Toolkit.LookVectorNormalized(zero, launchTriggerArea.centerPoint.position);
				Vector3 vector = Vector3.Reflect(launchForce.normalized, inNormal);
				float num = Mathf.Max(launchForce.magnitude * 0.75f, (moveState == MoveState.DeadAndLaunched) ? 60f : 40f);
				UpdateLaunch(vector * num);
				return;
			}
		}
		float num2 = GlobalValues.standard.gravity;
		if (moveState == MoveState.DeadAndLaunched)
		{
			num2 *= 3f;
		}
		launchForce -= num2 * xdt * Vector3.up;
		Vector3 vector2 = transform.position + launchForce * xdt;
		Vector3 vector3 = Toolkit.LookVector(transform.position, vector2.TargetYPosition(transform.position.y));
		if (vector3 != Vector3.zero)
		{
			transform.rotation = Quaternion.LookRotation(vector3, Vector3.up);
		}
		transform.position = vector2;
		timeLaunched += xdt;
		launchTriggerArea.ResetOverlap();
	}

	public void SetLaunchOrigin(Building origin)
	{
		launchOrigin = origin;
	}

	public bool CanDoActionPoint(ActionPoint ap, out bool need_to_wait)
	{
		need_to_wait = false;
		if (ap == null || !ap.activated)
		{
			return false;
		}
		return CanDoExchange(ap.GetConnectedObject(), ap.exchangeType, ap.GetExchangePoint(), out need_to_wait);
	}

	public virtual bool CanDoExchange(ConnectableObject target, ExchangeType exchange_type, ExchangePoint point, out bool after_waiting)
	{
		after_waiting = false;
		if (target == null)
		{
			return false;
		}
		if (!exchange_type.EveryAntCanDo() && !data.exchangeTypes.Contains(exchange_type))
		{
			return false;
		}
		if (!Progress.CanDoExchange(exchange_type))
		{
			return false;
		}
		PickupContainer pickupContainer = ((target is PickupContainer pickupContainer2) ? pickupContainer2 : null);
		Pickup pickup = ((target is Pickup pickup2) ? pickup2 : null);
		switch (exchange_type)
		{
		case ExchangeType.BUILDING_IN:
		{
			bool let_ant_wait2 = false;
			foreach (Pickup carryingPickup in carryingPickups)
			{
				if (pickupContainer.CanInsert(carryingPickup.type, exchange_type, point, ref let_ant_wait2, show_billboard: true))
				{
					return true;
				}
			}
			if (let_ant_wait2)
			{
				after_waiting = true;
				return true;
			}
			return false;
		}
		case ExchangeType.BUILDING_OUT:
		case ExchangeType.PICKUP:
		case ExchangeType.FORAGE:
		case ExchangeType.PLANT_CUT:
		case ExchangeType.MINE:
		case ExchangeType.PICKUP_CORPSE:
			if (IsFull())
			{
				return false;
			}
			if (pickupContainer != null)
			{
				bool let_ant_wait3 = false;
				if (pickupContainer.CanExtract(exchange_type, ref let_ant_wait3, show_billboard: true))
				{
					return true;
				}
				if (let_ant_wait3)
				{
					after_waiting = true;
					return true;
				}
			}
			else if (pickup != null)
			{
				return true;
			}
			return false;
		case ExchangeType.BUILDING_PROCESS:
		{
			bool let_ant_wait = false;
			foreach (Pickup carryingPickup2 in carryingPickups)
			{
				if (pickupContainer.CanInsert(carryingPickup2.type, ExchangeType.BUILDING_IN, point, ref let_ant_wait, show_billboard: true))
				{
					return true;
				}
			}
			if (pickupContainer.CanExtract(ExchangeType.BUILDING_OUT, ref let_ant_wait, show_billboard: true))
			{
				return !IsFull();
			}
			if (let_ant_wait)
			{
				after_waiting = true;
				return true;
			}
			return false;
		}
		case ExchangeType.ENTER:
			return true;
		case ExchangeType.EXIT:
			return false;
		default:
			Debug.LogWarning("Don't know how to handle exchange type " + exchange_type);
			return false;
		}
	}

	public float StartExchangePickup(ConnectableObject target, ExchangeType exchange)
	{
		PickupContainer pickupContainer = ((target is PickupContainer pickupContainer2) ? pickupContainer2 : null);
		Pickup pickup = ((target is Pickup pickup2) ? pickup2 : null);
		float num = 0f;
		switch (exchange)
		{
		case ExchangeType.BUILDING_IN:
		{
			if (!(pickupContainer != null))
			{
				break;
			}
			List<Pickup> list = new List<Pickup>(carryingPickups);
			for (int num2 = list.Count - 1; num2 >= 0; num2--)
			{
				if (list[num2] == null)
				{
					Debug.LogError("Tried inserting pickup null into building, shouldn't happen");
					carryingPickups.RemoveAt(num2);
					SetCarryAnim(carryingPickups.Count > 0);
				}
				else
				{
					bool let_ant_wait = false;
					if (pickupContainer.CanInsert(list[num2].type, exchange, null, ref let_ant_wait, show_billboard: true))
					{
						num = Mathf.Max(num, ExchangePickup(ExchangeType.INSERT, list[num2], pickupContainer));
					}
				}
			}
			break;
		}
		case ExchangeType.BUILDING_OUT:
		case ExchangeType.FORAGE:
		case ExchangeType.PLANT_CUT:
			if (pickupContainer != null)
			{
				List<PickupType> extractablePickups2 = pickupContainer.GetExtractablePickups(exchange);
				if (extractablePickups2.Count > 0)
				{
					pickup = pickupContainer.ExtractPickup(extractablePickups2[UnityEngine.Random.Range(0, extractablePickups2.Count)]);
					if (pickup != null)
					{
						PlayAudioShort(exchange switch
						{
							ExchangeType.FORAGE => WorldSfx.AntForage, 
							ExchangeType.PLANT_CUT => WorldSfx.AntPlantCut, 
							_ => AudioManager.GetPickupSfx(pickup.type), 
						});
						num = Mathf.Max(num, ExchangePickup(ExchangeType.EXTRACT, pickup));
					}
				}
			}
			else if (pickup != null)
			{
				PlayAudioShort(AudioManager.GetPickupSfx(pickup.type));
				num += ExchangePickup(ExchangeType.EXTRACT, pickup);
			}
			break;
		case ExchangeType.MINE:
			if (pickupContainer != null)
			{
				List<PickupType> extractablePickups3 = pickupContainer.GetExtractablePickups(exchange);
				if (extractablePickups3.Count <= 0)
				{
					break;
				}
				pickup = pickupContainer.ExtractPickup(extractablePickups3[UnityEngine.Random.Range(0, extractablePickups3.Count)]);
				if (!(pickup != null))
				{
					break;
				}
				if (data.mineForever)
				{
					pickup.transform.SetPositionAndRotation(carryPos.transform.position, carryPos.transform.rotation);
					PlayAudioShort(WorldSfx.AntMineRetrieve);
					num = Mathf.Max(num, ExchangePickup(ExchangeType.DROP_BEHIND, pickup));
				}
				else
				{
					PlayAudioShort(WorldSfx.AntMineRetrieve);
					num = Mathf.Max(num, ExchangePickup(ExchangeType.EXTRACT_INSTANT, pickup));
					if (num == 0f)
					{
						num = 0.001f;
					}
				}
				Progress.AddPickupMined(pickup.data.type);
			}
			else
			{
				Debug.Log("pc = null");
			}
			break;
		case ExchangeType.PICKUP:
		case ExchangeType.PICKUP_CORPSE:
			if (pickup != null)
			{
				PlayAudioShort(AudioManager.GetPickupSfx(pickup.type));
				num += ExchangePickup(ExchangeType.EXTRACT, pickup);
			}
			else
			{
				if (!(pickupContainer != null))
				{
					break;
				}
				List<PickupType> extractablePickups = pickupContainer.GetExtractablePickups(exchange);
				if (extractablePickups.Count > 0)
				{
					pickup = pickupContainer.ExtractPickup(extractablePickups[UnityEngine.Random.Range(0, extractablePickups.Count)]);
					if (pickup != null)
					{
						PlayAudioShort(AudioManager.GetPickupSfx(pickup.type));
						num += ExchangePickup(ExchangeType.EXTRACT, pickup);
					}
				}
			}
			break;
		default:
			Debug.LogWarning("Don't know how to handle exchange type " + exchange);
			break;
		}
		if (num == 0f)
		{
			Debug.LogWarning("Tried starting pickup exchange " + exchange.ToString() + " and returned 0, probably shouldn't happen", base.gameObject);
		}
		return num;
	}

	private void SendPickupsToInventory()
	{
		Ground ground = Toolkit.GetGround(transform.position);
		foreach (Pickup item in new List<Pickup>(carryingPickups))
		{
			if (GameManager.instance.TryExchangePickupToInventory(ground, transform.position, item, null, teleport: true, exclude_empty_stockpiles: true))
			{
				carryingPickups.Remove(item);
			}
		}
	}

	public void DropPickupsOnGround()
	{
		foreach (Pickup item in new List<Pickup>(carryingPickups))
		{
			ExchangePickup(ExchangeType.DROP, item);
		}
	}

	public void DeleteCarryingPickups()
	{
		foreach (Pickup item in new List<Pickup>(carryingPickups))
		{
			ExchangePickup(ExchangeType.DELETE, item);
		}
	}

	public float ExchangePickup(ExchangeType exchange_type, Pickup _pickup, PickupContainer _pc = null)
	{
		if (DebugSettings.standard.logPickupExchange)
		{
			Debug.Log("Exchanging pickup" + Time.time);
		}
		if (_pickup == null)
		{
			Debug.LogWarning("Ant pickup exchange: Pickup is null, shouldn't happen");
			return 0f;
		}
		float result = 0f;
		switch (exchange_type)
		{
		case ExchangeType.EXTRACT:
			carryingPickups.Add(_pickup);
			result = _pickup.Exchange(this, ExchangeAnimationType.STRAIGHT);
			break;
		case ExchangeType.EXTRACT_INSTANT:
			carryingPickups.Add(_pickup);
			result = _pickup.Exchange(this, ExchangeAnimationType.TELEPORT);
			break;
		case ExchangeType.INSERT:
		{
			if (_pc == null)
			{
				Debug.LogError("Pickup insert: Container is null, shouldn't happen");
				return 0f;
			}
			PlayAudioShort(AudioManager.GetDropSfx(_pickup.type));
			carryingPickups.Remove(_pickup);
			SetCarryAnim(carryingPickups.Count > 0);
			result = _pickup.Exchange(_pc, _pc.GetInsertPos(_pickup), ExchangeAnimationType.ARC);
			for (int j = 0; j < carryingPickups.Count; j++)
			{
				MovePickupInStack(carryingPickups[j], GetPickupLocalPosInStack(j));
			}
			break;
		}
		case ExchangeType.DROP:
		{
			PlayAudioShort(AudioManager.GetDropSfx(_pickup.type));
			if (carryingPickups.Contains(_pickup))
			{
				carryingPickups.Remove(_pickup);
			}
			SetCarryAnim(carryingPickups.Count > 0);
			Vector3 vector = transform.right * UnityEngine.Random.Range(-3f, 3f);
			Vector3 target_pos = (_pickup.transform.position + transform.forward * (_pickup.GetRadius() * 2f) + vector).ZeroPosition();
			_pickup.transform.rotation = Toolkit.RandomYRotation();
			result = _pickup.Exchange(target_pos, (GameManager.instance.GetStatus() == GameStatus.PAUSED) ? ExchangeAnimationType.ARC_UNSCALED : ExchangeAnimationType.ARC);
			for (int i = 0; i < carryingPickups.Count; i++)
			{
				MovePickupInStack(carryingPickups[i], GetPickupLocalPosInStack(i));
			}
			break;
		}
		case ExchangeType.DROP_BEHIND:
		{
			PlayAudioShort(AudioManager.GetDropSfx(_pickup.type));
			if (carryingPickups.Contains(_pickup))
			{
				carryingPickups.Remove(_pickup);
			}
			SetCarryAnim(carryingPickups.Count > 0);
			Vector2 vector2 = UnityEngine.Random.insideUnitCircle * 3f;
			Vector3 vector = new Vector3(vector2.x, 0f, vector2.y);
			Vector3 target_pos = (transform.position - transform.forward * (GetRadius() + 4f) + vector).ZeroPosition();
			_pickup.transform.rotation = Toolkit.RandomYRotation();
			result = _pickup.Exchange(target_pos, ExchangeAnimationType.ARC);
			for (int k = 0; k < carryingPickups.Count; k++)
			{
				MovePickupInStack(carryingPickups[k], GetPickupLocalPosInStack(k));
			}
			break;
		}
		case ExchangeType.DELETE:
			SetCarryAnim(carryingPickups.Count - 1 > 0);
			if (carryingPickups.Contains(_pickup))
			{
				carryingPickups.Remove(_pickup);
			}
			_pickup.Delete();
			break;
		default:
			Debug.LogWarning("Don't know how to handle exchange type " + exchange_type);
			break;
		}
		electrolysedEnergy = 0f;
		return result;
	}

	public virtual void OnPickupArrival(Pickup pickup)
	{
		pickup.SetStatus(PickupStatus.CARRIED, carryPos);
		pickup.transform.localPosition = Vector3.zero;
		pickup.transform.localRotation = Quaternion.identity;
		SetCarryAnim(carryingPickups.Count > 0);
		Progress.AddSeenPickup(pickup.data.type);
	}

	public void DirectAddPickup(Pickup pickup)
	{
		carryingPickups.Add(pickup);
		pickup.SetStatus(PickupStatus.CARRIED, carryPos);
		pickup.transform.localPosition = Vector3.zero;
		SetCarryAnim(target: true);
		Progress.AddSeenPickup(pickup.data.type);
	}

	public Pickup DirectRetrievePickup(PickupType pickup_type)
	{
		for (int i = 0; i < carryingPickups.Count; i++)
		{
			Pickup pickup = carryingPickups[i];
			if (pickup.type == pickup_type)
			{
				carryingPickups.RemoveAt(i);
				SetCarryAnim(carryingPickups.Count > 0);
				return pickup;
			}
		}
		return null;
	}

	public bool IsFull()
	{
		return carryingPickups.Count >= carryCapacity;
	}

	private void MovePickupInStack(Pickup _pickup, Vector3 local_target_pos)
	{
		_pickup.LocalMove(local_target_pos);
	}

	private Vector3 GetPickupLocalPosInStack(int h)
	{
		Vector3 localPosition = carryPos.localPosition;
		for (int i = 0; i < h; i++)
		{
			localPosition.y += carryingPickups[i].height;
		}
		return localPosition;
	}

	public int GetCarryingPickupsCount()
	{
		return carryingPickups.Count;
	}

	public IEnumerable<PickupType> ECarryingPickupTypes()
	{
		foreach (Pickup carryingPickup in carryingPickups)
		{
			yield return carryingPickup.type;
		}
	}

	public void SetCarryAnim(bool target)
	{
		if (anim != null)
		{
			anim.SetBool(ClickableObject.paramCarry, target);
		}
	}

	public IEnumerator CSetCarryAnim(bool target)
	{
		SetCarryAnim(target);
		yield return new WaitForSeconds(0.25f);
	}

	public virtual bool CanEatPickup(PickupType _type)
	{
		return PickupData.Get(_type).IsEdible();
	}

	public virtual void EatPickup(Pickup p)
	{
		ExchangePickup(ExchangeType.DELETE, p);
		if (moveState != MoveState.Dead && moveState != MoveState.DeadAndLaunched && moveState != MoveState.DeadAndDisabled)
		{
			energy = Mathf.Clamp(energy + p.data.energyAmount * 100f, 0f, energyTotal);
			PlayAudioShort(WorldSfx.QueenEat);
		}
	}

	private void UpdateGroundCheck()
	{
		float deltaTime = Time.deltaTime;
		groundCheckTimer -= deltaTime;
		if (groundCheckTimer < 0f)
		{
			groundCheckTimer = 0.5f;
			CheckGround();
		}
	}

	private void CheckGround()
	{
		Ground ground = Toolkit.GetGround(transform.position);
		if (ground != currentGround)
		{
			if (currentGround != null)
			{
				currentGround.RemoveAnt(this);
			}
			if (ground != null)
			{
				ground.AddAnt(this);
			}
			currentGround = ground;
		}
	}

	private void ClearGround()
	{
		if (currentGround != null)
		{
			currentGround.RemoveAnt(this);
			currentGround = null;
		}
	}

	public override float GetRadius()
	{
		return antRadius;
	}

	public void UpdateMaterial()
	{
		if (IsDead())
		{
			SetMaterial(StatusEffect.DEAD);
		}
		else if (statusEffects.materialEffect != StatusEffect.NONE)
		{
			SetMaterial(statusEffects.materialEffect);
		}
		else
		{
			SetStartMaterial();
		}
	}

	public void SetStartMaterial()
	{
		SetMaterials(AssetLinks.standard.GetAntMaterial(data.caste, StatusEffect.NONE));
	}

	public void SetMaterial(StatusEffect _effect)
	{
		SetMaterials(AssetLinks.standard.GetAntMaterial(data.caste, _effect));
	}

	public void SetMaterials(List<Material> mats)
	{
		foreach (Renderer rend in rends)
		{
			Material[] array = new Material[rend.sharedMaterials.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (i < 2)
				{
					array[i] = mats[i];
				}
				else
				{
					array[i] = rend.sharedMaterials[i];
				}
			}
			rend.sharedMaterials = array;
		}
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		if (is_selected && !was_selected && !CamController.instance.GetFollowTarget() == (bool)transform)
		{
			CamController.instance.ToggleFollow(null);
		}
	}

	public override Vector3 GetAssignLinePos(AssignType assign_type)
	{
		switch (assign_type)
		{
		case AssignType.GATE:
			return transform.position.TargetYPosition(transform.position.y + GetHeight() / 2f);
		case AssignType.LINK:
			if (statusPos != null)
			{
				return statusPos.position;
			}
			break;
		}
		return base.GetAssignLinePos(assign_type);
	}

	public override void SetHoverUI(UIHoverClickOb ui_hover)
	{
		ui_hover.SetTitle(data.GetTitleFull());
		if (IsDead())
		{
			ui_hover.SetInfo();
			return;
		}
		if (currentActionPoint != null && currentActionPoint.exchangeType == ExchangeType.MINE && actionPointProgress >= 0f)
		{
			ui_hover.SetInfo();
		}
		ui_hover.SetCapabilities(Loc.GetUI("ANT_CANDO"));
		foreach (TrailType item in TrailData.ExchangeTypesToTrailTypes(data.exchangeTypes))
		{
			ui_hover.AddCapability(item);
		}
		if (this is AntInventor)
		{
			ui_hover.AddCapability(Loc.GetUI("ANT_CANDO_INVENTING"), Color.clear);
		}
		if (data.flying)
		{
			ui_hover.AddCapability(Loc.GetUI("ANT_CANDO_FLYING"), Color.clear);
		}
		if (data.isGyne)
		{
			ui_hover.AddCapability(Loc.GetUI("ANT_CANDO_REPRODUCING"), Color.clear);
		}
		ui_hover.SetHealth(Loc.GetUI("ANT_LIFESPAN"));
		ui_hover.SetButtonWithText(delegate
		{
			DropPickupsOnGround();
		}, clear_on_click: false);
		ui_hover.SetButtonWithTextHotkey(InputAction.DropPickup);
		if (electrolyseTarget > 0f)
		{
			ui_hover.SetEnergy(Loc.GetUI("ANT_ELECTROLYSING"));
		}
		ui_hover.SetBottomButtons(DebugSettings.standard.DeletableEverything() ? new Action(OnClickDelete) : null, null, delegate
		{
			CamController.instance.ToggleFollow(transform);
			Gameplay.instance.Select(null);
		});
	}

	public override void UpdateHoverUI(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI(ui_hover);
		AntUpdateHoverUI(ui_hover);
	}

	protected virtual void AntUpdateHoverUI(UIHoverClickOb ui_hover)
	{
		if (IsDead())
		{
			ui_hover.UpdateEffects(new List<StatusEffect> { StatusEffect.DEAD });
			switch (deathCause)
			{
			case DeathCause.OLD_AGE:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_OLDAGE"));
				break;
			case DeathCause.FELL_OFF_MAP:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_FELLOFFMAP"));
				break;
			case DeathCause.TOXIC_WASTE:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_TOXICWASTE"));
				break;
			case DeathCause.INVENTOR:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_INVENTOR"));
				break;
			case DeathCause.BUG:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_BUG"));
				break;
			case DeathCause.DEBUG_DEATH:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_DEBUGDEATH"));
				break;
			case DeathCause.CRUSHER:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_CRUSHER"));
				break;
			case DeathCause.RADIATION:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_RADIATION"));
				break;
			case DeathCause.IMPACT:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_IMPACT"));
				break;
			case DeathCause.RADIATION_EXPLODED:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_RADIATION_EXPLOSION"));
				break;
			default:
				ui_hover.UpdateInfo(Loc.GetUI("ANT_DEAD_NONE"));
				break;
			}
			return;
		}
		if (currentActionPoint != null && currentActionPoint.exchangeType == ExchangeType.MINE && actionPointProgress >= 0f)
		{
			ui_hover.UpdateInfo(Loc.GetUI("ANT_MINING") + " " + actionPointProgress.Unit(PhysUnit.TIME_MINUTES));
		}
		else
		{
			ui_hover.UpdateInfo("");
		}
		string amount = (IsImmortal() ? "∞" : (energy / statusEffects.lifeDrainFactor).Unit(PhysUnit.TIME_MINUTES));
		ui_hover.UpdateHealth(amount, GetRemainingLife());
		if (radiationDeathTimer > 0f)
		{
			ui_hover.UpdateRadDeath(Mathf.Clamp01(radiationDeathTimer / GlobalValues.standard.radDeathTime));
		}
		if (electrolyseTarget > 0f)
		{
			ui_hover.UpdateEnergy("", electrolysedEnergy / electrolyseTarget);
		}
		ui_hover.UpdateButtonWithText(Loc.GetUI("ANT_DROP"), carryingPickups.Count > 0);
		ui_hover.UpdateEffects(statusEffects.currentEffects);
		if (!statusEffects.currentEffects.Contains(StatusEffect.RADIATION))
		{
			return;
		}
		int num = 0;
		foreach (StatusEffect currentEffect in statusEffects.currentEffects)
		{
			if (currentEffect == StatusEffect.RADIATION)
			{
				num++;
			}
		}
		StatusEffectData statusEffectData = StatusEffectData.Get(StatusEffect.RADIATION);
		if (num == 1)
		{
			ui_hover.UpdateEffectTitle(StatusEffect.RADIATION, statusEffectData.GetTitle());
		}
		else
		{
			ui_hover.UpdateEffectTitle(StatusEffect.RADIATION, statusEffectData.GetTitleMultiple(num));
		}
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.ANT;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		ui_click.SetTitle(data.GetTitleFull());
		UIClickLayout_Ant uIClickLayout_Ant = (UIClickLayout_Ant)ui_click;
		uIClickLayout_Ant.SetEffects();
		uIClickLayout_Ant.SetHealth(Loc.GetUI("ANT_LIFESPAN"));
		if (IsDead())
		{
			return;
		}
		uIClickLayout_Ant.SetCapabilities(Loc.GetUI("ANT_CANDO"));
		foreach (TrailType item in TrailData.ExchangeTypesToTrailTypes(data.exchangeTypes))
		{
			uIClickLayout_Ant.AddCapability(item);
		}
		if (this is AntInventor)
		{
			uIClickLayout_Ant.AddCapability(Loc.GetUI("ANT_CANDO_INVENTING"), Color.clear);
		}
		if (data.flying)
		{
			uIClickLayout_Ant.AddCapability(Loc.GetUI("ANT_CANDO_FLYING"), Color.clear);
		}
		if (data.isGyne)
		{
			uIClickLayout_Ant.AddCapability(Loc.GetUI("ANT_CANDO_REPRODUCING"), Color.clear);
		}
		uIClickLayout_Ant.SetButton(UIClickButtonType.Delete, (DebugSettings.standard.DeletableEverything() && !IsDead()) ? new Action(OnClickDelete) : null, InputAction.Delete);
		uIClickLayout_Ant.SetButton(UIClickButtonType.Follow, delegate
		{
			Gameplay.instance.SetActivity(Activity.NONE);
			CamController.instance.ToggleFollow(transform);
		}, InputAction.FollowAnt);
		uIClickLayout_Ant.SetButton(UIClickButtonType.Drop, DropPickupsOnGround, InputAction.DropPickup);
		uIClickLayout_Ant.SetButton(UIClickButtonType.Clear, delegate
		{
			GameManager.instance.RemoveAntFromLinkGates(this);
		}, InputAction.None);
	}

	public override void UpdateClickUi(UIClickLayout ui_click)
	{
		base.UpdateClickUi(ui_click);
		UpdateClickUI_Ant(ui_click);
	}

	protected virtual void UpdateClickUI_Ant(UIClickLayout ui_click)
	{
		UIClickLayout_Ant uIClickLayout_Ant = (UIClickLayout_Ant)ui_click;
		if (IsDead())
		{
			uIClickLayout_Ant.UpdateHealth(0f.Unit(PhysUnit.TIME_MINUTES), 0f);
			uIClickLayout_Ant.UpdateEffects(new List<StatusEffect> { StatusEffect.DEAD });
			uIClickLayout_Ant.HideCapabilities();
			uIClickLayout_Ant.SetCarrying(PickupType.NONE);
			uIClickLayout_Ant.UpdateButton(UIClickButtonType.Delete, enabled: false);
			uIClickLayout_Ant.UpdateButton(UIClickButtonType.Follow, enabled: false);
			uIClickLayout_Ant.UpdateButton(UIClickButtonType.Drop, enabled: false);
			uIClickLayout_Ant.UpdateButton(UIClickButtonType.Clear, enabled: false);
		}
		else
		{
			string amount = (IsImmortal() ? "∞" : Mathf.Clamp(energy / statusEffects.lifeDrainFactor, 0f, float.MaxValue).Unit(PhysUnit.TIME_MINUTES));
			uIClickLayout_Ant.UpdateHealth(amount, GetRemainingLife());
			if (radiationDeathTimer > 0f)
			{
				uIClickLayout_Ant.UpdateRadDeath(Mathf.Clamp01(radiationDeathTimer / GlobalValues.standard.radDeathTime));
			}
			uIClickLayout_Ant.UpdateButton(UIClickButtonType.Drop, carryingPickups.Count > 0, Loc.GetUI("ANT_DROP"));
			if (carryingPickups.Count > 0)
			{
				uIClickLayout_Ant.SetCarrying(carryingPickups[0].type);
			}
			else
			{
				uIClickLayout_Ant.SetCarrying(PickupType.NONE);
			}
			int num = 0;
			foreach (TrailGate_Link linkedGate in GameManager.instance.GetLinkedGates(this))
			{
				_ = linkedGate;
				num++;
			}
			if (num == 0)
			{
				uIClickLayout_Ant.SetLinkInfo("");
			}
			else
			{
				uIClickLayout_Ant.UpdateButton(UIClickButtonType.Clear, num > 0, Loc.GetUI("ANT_CLEAR_LINKS"));
				uIClickLayout_Ant.SetLinkInfo(Loc.GetUI("ANT_LINKED_GATES", num.ToString()));
			}
			uIClickLayout_Ant.UpdateEffects(statusEffects.currentEffects);
			if (statusEffects.currentEffects.Contains(StatusEffect.RADIATION))
			{
				int num2 = 0;
				foreach (StatusEffect currentEffect in statusEffects.currentEffects)
				{
					if (currentEffect == StatusEffect.RADIATION)
					{
						num2++;
					}
				}
				StatusEffectData statusEffectData = StatusEffectData.Get(StatusEffect.RADIATION);
				if (num2 == 1)
				{
					uIClickLayout_Ant.UpdateEffectTitle(StatusEffect.RADIATION, statusEffectData.GetTitle());
				}
				else
				{
					uIClickLayout_Ant.UpdateEffectTitle(StatusEffect.RADIATION, statusEffectData.GetTitleMultiple(num2));
				}
			}
		}
		if (electrolyseTarget > 0f && !IsDead())
		{
			uIClickLayout_Ant.SetEnergy(target: true, Loc.GetUI("ANT_ELECTROLYSING"));
			uIClickLayout_Ant.UpdateEnergy("", electrolysedEnergy / electrolyseTarget);
		}
		else
		{
			uIClickLayout_Ant.SetEnergy(target: false);
		}
		if (GetAntInfo(out var s))
		{
			uIClickLayout_Ant.SetInfo(s);
		}
		else
		{
			uIClickLayout_Ant.SetInfo("");
		}
	}

	protected virtual bool GetAntInfo(out string s)
	{
		s = "";
		if (IsDead())
		{
			switch (deathCause)
			{
			case DeathCause.OLD_AGE:
				s = Loc.GetUI("ANT_DEAD_OLDAGE");
				break;
			case DeathCause.FELL_OFF_MAP:
				s = Loc.GetUI("ANT_DEAD_FELLOFFMAP");
				break;
			case DeathCause.TOXIC_WASTE:
				s = Loc.GetUI("ANT_DEAD_TOXICWASTE");
				break;
			case DeathCause.INVENTOR:
				s = Loc.GetUI("ANT_DEAD_INVENTOR");
				break;
			case DeathCause.BUG:
				s = Loc.GetUI("ANT_DEAD_BUG");
				break;
			case DeathCause.DEBUG_DEATH:
				s = Loc.GetUI("ANT_DEAD_DEBUGDEATH");
				break;
			case DeathCause.CRUSHER:
				s = Loc.GetUI("ANT_DEAD_CRUSHER");
				break;
			case DeathCause.RADIATION:
				s = Loc.GetUI("ANT_DEAD_RADIATION");
				break;
			case DeathCause.IMPACT:
				s = Loc.GetUI("ANT_DEAD_IMPACT");
				break;
			case DeathCause.RADIATION_EXPLODED:
				s = Loc.GetUI("ANT_DEAD_RADIATION_EXPLOSION");
				break;
			default:
				s = Loc.GetUI("ANT_DEAD_NONE");
				break;
			}
		}
		else if (currentActionPoint != null && currentActionPoint.exchangeType == ExchangeType.MINE && actionPointProgress >= 0f)
		{
			s = Loc.GetUI("ANT_MINING") + " " + actionPointProgress.Unit(PhysUnit.TIME_MINUTES);
		}
		else
		{
			s = Loc.GetUI("ANT_CURRENT_SPEED", (Mathf.Round(GetSpeed() * GetSpeedFactor() * 10f) / 10f).ToString());
		}
		return s != "";
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		BillboardType currentBillboard = base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
		if (currentBillboard != BillboardType.NONE)
		{
			return currentBillboard;
		}
		if (billboardWarning != "")
		{
			code_desc = billboardWarning;
			col = Color.red;
			return BillboardType.CROSS_SMALL;
		}
		code_desc = "";
		col = Color.white;
		return BillboardType.NONE;
	}

	protected override void ClearBillboard()
	{
		billboardWarning = "";
	}

	public void TryToReturn()
	{
		if (returnToTrails == null)
		{
			return;
		}
		Trail trail = null;
		Vector3 vector = Vector3.zero;
		float num = float.MaxValue;
		Vector3 position = transform.position;
		foreach (Trail returnToTrail in returnToTrails)
		{
			Vector3 nearestPointOnTrail = returnToTrail.GetNearestPointOnTrail(position);
			float sqrMagnitude = (nearestPointOnTrail - position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				vector = nearestPointOnTrail;
				trail = returnToTrail;
			}
		}
		if (trail != null)
		{
			List<ClickableObject> obstructing_objects = new List<ClickableObject>();
			if (!Trail.IsObstructed(position, vector, ref obstructing_objects, escape: true))
			{
				Trail trail2 = GameManager.instance.NewTrail(TrailType.COMMAND, null, this);
				if (num < 0.01f)
				{
					position -= 0.1f * trail.direction;
				}
				trail2.NewStartSplit(position);
				trail2.NewEndSplit(vector);
				trail2.PlaceTrail(TrailStatus.PLACED);
				SetCurrentTrail(trail2, 0f);
			}
		}
		returnToTrails = null;
	}

	private void EnsureChannel()
	{
		if (audioAnt == null)
		{
			audioAnt = AudioManager.GetAntChannel();
			audioAnt.Lock();
			audioAnt.Attach(transform);
			audioAnt.InitCulled();
		}
	}

	protected void PlayAudio(AudioLink audio)
	{
		EnsureChannel();
		audioAnt.Play(audio);
	}

	protected void PlayAudioShort(WorldSfx sfx)
	{
		AudioClip clip = AudioLinks.standard.GetClip(sfx);
		if (clip != null)
		{
			AudioManager.PlayWorldShort(transform.position, clip);
		}
	}

	protected void StartLoopAudio(AudioLink audio)
	{
		if (audio.IsSet())
		{
			EnsureChannel();
			audioAnt.Play(audio, looped: true);
		}
	}

	protected void StartLoopAudio(AudioLink audio, float delay)
	{
		if (audio.IsSet())
		{
			EnsureChannel();
			audioAnt.PlayDelayed(audio, looped: true, delay);
		}
	}

	protected void StopAudio()
	{
		if (audioAnt != null)
		{
			audioAnt.Stop();
		}
	}

	private void ClearAntAudio()
	{
		if (audioAnt != null)
		{
			audioAnt.Free();
			audioAnt = null;
		}
	}
}
