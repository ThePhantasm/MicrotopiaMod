using System;
using UnityEngine;

public class AntLauncher : Building
{
	[Header("Ant Launcher")]
	public Transform rotationPoint;

	public Transform anglePoint;

	public Transform launchPoint;

	public float randomness = 5f;

	public float rotSpeed = 5f;

	public float shootDuration;

	public Vector2 rangeRotation;

	public Vector2 rangeAngle;

	public Vector2 rangePower;

	[NonSerialized]
	public float rotation = 0.5f;

	[NonSerialized]
	public float angle = 0.5f;

	[NonSerialized]
	public float power = 0.5f;

	private Transform traject;

	private Quaternion targetRot;

	private Quaternion targetAngle;

	private Ant loadedAnt;

	private int loadedAntId = -1;

	private float shootTime;

	private bool rotating;

	private bool rotatingAudio;

	[SerializeField]
	private AudioLink audioShoot;

	[SerializeField]
	private AudioLink audioRotateLoop;

	public override void Write(Save save)
	{
		base.Write(save);
		WriteConfig(save);
		save.Write(loadedAnt != null);
		if (loadedAnt != null)
		{
			save.Write(loadedAnt.linkId);
			save.Write(shootTime);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		if (save.version >= 23)
		{
			ReadConfig(save);
		}
		if (save.version >= 24 && save.ReadBool())
		{
			loadedAntId = save.ReadInt();
			shootTime = save.ReadFloat();
		}
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		save.Write(rotation);
		save.Write(angle);
		save.Write(power);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		rotation = save.ReadFloat();
		angle = save.ReadFloat();
		power = save.ReadFloat();
		if (save.GetSaveType() != SaveType.GameSave)
		{
			UpdateTrajectory(on_init: true);
		}
	}

	public override void LoadLinkBuildings()
	{
		base.LoadLinkBuildings();
		if (loadedAntId != -1)
		{
			loadedAnt = GameManager.instance.FindLink<Ant>(loadedAntId);
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		UpdateTrajectory(on_init: true);
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (!runWorld)
		{
			return;
		}
		if (rotating)
		{
			rotationPoint.localRotation = Quaternion.RotateTowards(rotationPoint.localRotation, targetRot, rotSpeed * dt);
			anglePoint.localRotation = Quaternion.RotateTowards(anglePoint.localRotation, targetAngle, rotSpeed * dt);
			if (Quaternion.Angle(rotationPoint.localRotation, targetRot) < 0.01f && Quaternion.Angle(anglePoint.localRotation, targetAngle) < 0.01f)
			{
				rotationPoint.localRotation = targetRot;
				anglePoint.localRotation = targetAngle;
				rotating = false;
				if (rotatingAudio)
				{
					StopAudio();
					rotatingAudio = false;
				}
			}
		}
		if (loadedAnt != null)
		{
			shootTime += dt;
			if (shootTime >= shootDuration)
			{
				LaunchAnt();
				loadedAnt.SetLaunchOrigin(this);
				loadedAnt = null;
			}
		}
	}

	public override void Demolish()
	{
		base.Demolish();
		if (loadedAnt != null)
		{
			DropAntOnGround(loadedAnt);
			loadedAnt = null;
		}
	}

	public override bool TryUseBuilding(int _entrance, Ant _ant)
	{
		if (loadedAnt != null)
		{
			return false;
		}
		return base.TryUseBuilding(_entrance, _ant);
	}

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		if (shootDuration == 0f)
		{
			LaunchAnt();
		}
		else
		{
			_ant.transform.SetPositionAndRotation(launchPoint.position, launchPoint.rotation);
			_ant.transform.parent = launchPoint;
			_ant.SetMoveState(MoveState.Carried);
			loadedAnt = _ant;
			shootTime = 0f;
		}
		anim.SetTrigger("Launch");
		ant_entered = true;
		return 0f;
	}

	private void LaunchAnt()
	{
		if (traject == null)
		{
			traject = new GameObject().transform;
			traject.position = launchPoint.position;
			traject.parent = launchPoint.parent;
		}
		traject.SetPositionAndRotation(launchPoint.position, launchPoint.rotation);
		Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
		Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, randomness), onUnitSphere);
		traject.rotation = quaternion * traject.rotation;
		loadedAnt.transform.parent = null;
		loadedAnt.transform.position = traject.position;
		loadedAnt.StartLaunch(traject.forward * Mathf.Lerp(rangePower.x, rangePower.y, power), LaunchCause.LAUNCHER);
		rotatingAudio = false;
		PlayAudio(audioShoot);
	}

	public void UpdateTrajectory(bool on_init = false)
	{
		targetRot = Quaternion.Euler(rotationPoint.localRotation.x, rotationPoint.localRotation.y, Mathf.Lerp(rangeRotation.x, rangeRotation.y, rotation));
		targetAngle = Quaternion.Euler(Mathf.Lerp(rangeAngle.x, rangeAngle.y, angle), anglePoint.localRotation.y, anglePoint.localRotation.z);
		if (!on_init)
		{
			if (!rotatingAudio)
			{
				StartLoopAudio(audioRotateLoop);
				rotatingAudio = true;
			}
			rotating = true;
		}
		else
		{
			rotationPoint.localRotation = targetRot;
			anglePoint.localRotation = targetAngle;
		}
	}

	public override bool CheckIfGateIsSatisfied(Ant ant, Trail trail, out string warning)
	{
		warning = "";
		if (ant.GetCarryingPickupsCount() > 0 || ant.caste == AntCaste.GYNE)
		{
			return false;
		}
		if (loadedAnt != null || AnyAntsOnBuildingTrails(trail))
		{
			return false;
		}
		return base.CheckIfGateIsSatisfied(ant, trail, out warning);
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetCannon(this);
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.CANNON;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		((UIClickLayout_Cannon)ui_building).SetCannon(this);
	}

	public override bool CanCopySettings()
	{
		return true;
	}
}
