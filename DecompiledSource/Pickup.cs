using System;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : ConnectableObject
{
	[Header("Pickup")]
	public List<GameObject> meshes = new List<GameObject>();

	public Transform meshParent;

	private Collider[] cols;

	public float height = 1f;

	public RotationMode rotationMode;

	[NonSerialized]
	public PickupType type;

	public PickupData data;

	[Space(10f)]
	private PickupStatus status;

	private float decay;

	private PickupStatus storedStatus;

	private Ant exchangingTargetAnt;

	private PickupContainer exchangingTargetContainer;

	private int exchangingTargetAntId;

	private int exchangingTargetContainerId;

	private Vector3 loadedLocalPos;

	private Quaternion loadedLocalRot;

	private ExchangeAnimation exchangeAnim = new ExchangeAnimation();

	private AudioChannel audioPickup;

	private int meshIndex = -1;

	private Building exchangingOriginBuilding;

	private ExchangePoint exchangedFromPoint;

	[SerializeField]
	private Animator anim;

	[SerializeField]
	private MeshRenderer eyeBlinkRenderer;

	[SerializeField]
	private MinMax eyeBlinkSpeed;

	private static float[] blinkValues;

	[NonSerialized]
	public int pileSelection;

	private Ground ground;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(base.transform.localPosition);
		save.WriteYRot(base.transform.localRotation);
		if (status == PickupStatus.EXCHANGING || status == PickupStatus.MOVING_LOCALLY)
		{
			save.Write(b: true);
			save.Write((int)status);
			exchangeAnim.Write(save);
			save.Write((!(exchangingTargetAnt == null)) ? exchangingTargetAnt.linkId : 0);
			save.Write((!(exchangingTargetContainer == null)) ? exchangingTargetContainer.linkId : 0);
		}
		else
		{
			save.Write(b: false);
			save.Write(decay);
		}
		save.Write(meshIndex);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		loadedLocalPos = save.ReadVector3();
		loadedLocalRot = save.ReadYRot();
		if (save.ReadBool())
		{
			storedStatus = (PickupStatus)save.ReadInt();
			exchangeAnim.Read(save);
			exchangingTargetAntId = save.ReadInt();
			exchangingTargetContainerId = save.ReadInt();
			decay = -1f;
		}
		else if (save.version < 51)
		{
			decay = -1f;
		}
		else
		{
			decay = save.ReadFloat();
		}
		meshIndex = save.ReadInt();
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (eyeBlinkRenderer != null)
		{
			if (blinkValues == null)
			{
				blinkValues = new float[6];
				for (int i = 0; i < blinkValues.Length; i++)
				{
					blinkValues[i] = UnityEngine.Random.value;
				}
			}
			int num = UnityEngine.Random.Range(0, blinkValues.Length);
			eyeBlinkRenderer.sharedMaterial = MaterialLibrary.GetBlinkMaterial(eyeBlinkRenderer.sharedMaterial, num, eyeBlinkSpeed.Lerp(blinkValues[num]));
		}
		if (anim != null)
		{
			GameManager.instance.InitAnimator(anim, AnimationCulling.Always);
		}
		GetMesh();
		if (during_load)
		{
			SetStatus(storedStatus);
			if (status == PickupStatus.NONE)
			{
				SetColliders(target: false);
			}
		}
		else
		{
			SetColliders(target: false);
			decay = -1f;
		}
		base.transform.DestroyChildrenWithLayer(Layers.IconGenerate);
		meshParent.DestroyChildrenWithLayer(Layers.IconGenerate);
		float[] array = new float[4] { 0f, 90f, 180f, 270f };
		switch (rotationMode)
		{
		case RotationMode.Y_2_SIDES:
			meshParent.localRotation = Quaternion.Euler(0f, Toolkit.CoinFlip() ? 0f : 180f, 0f);
			break;
		case RotationMode.Y_2_SIDES_UP_DOWN:
			meshParent.localRotation = Quaternion.Euler(Toolkit.CoinFlip() ? 0f : 180f, Toolkit.CoinFlip() ? 0f : 180f, 0f);
			break;
		case RotationMode.Y_4_SIDES:
			meshParent.localRotation = Quaternion.Euler(0f, array[UnityEngine.Random.Range(0, array.Length)], 0f);
			break;
		case RotationMode.Y_360:
			meshParent.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
			break;
		case RotationMode.FULL_SIX_SIDES:
			meshParent.localRotation = Quaternion.Euler(array[UnityEngine.Random.Range(0, array.Length)], array[UnityEngine.Random.Range(0, array.Length)], 0f);
			break;
		case RotationMode.FULL_360:
			meshParent.localRotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f));
			break;
		}
	}

	public void Fill(PickupType _type)
	{
		type = _type;
		data = PickupData.Get(_type);
	}

	public void LoadLinkAntsAndContainers()
	{
		exchangingTargetAnt = GameManager.instance.FindLink<Ant>(exchangingTargetAntId);
		exchangingTargetContainer = GameManager.instance.FindLink<PickupContainer>(exchangingTargetContainerId);
		if (status == PickupStatus.EXCHANGING && exchangingTargetContainer != null)
		{
			exchangingTargetContainer.PrepareForPickup(this, exchangedFromPoint);
		}
		if (status == PickupStatus.NONE)
		{
			SetStatus(PickupStatus.ON_GROUND);
		}
		base.transform.localPosition = loadedLocalPos;
		base.transform.localRotation = loadedLocalRot;
	}

	public void GetMesh()
	{
		if (meshIndex == -1 || meshIndex >= meshes.Count)
		{
			meshIndex = UnityEngine.Random.Range(0, meshes.Count);
		}
		for (int i = 0; i < meshes.Count; i++)
		{
			if (i != meshIndex)
			{
				UnityEngine.Object.Destroy(meshes[i]);
			}
		}
		meshes[meshIndex].SetObActive(active: true);
		meshes.Clear();
	}

	public override void OnClickDelete()
	{
		if (DebugSettings.standard.DeletableEverything())
		{
			Delete();
		}
	}

	protected override void DoDelete()
	{
		GameManager.instance.RemovePickup(this);
		if (ground != null)
		{
			ground.RemovePickupOnGround(this);
		}
		ClearAudio();
		UnityEngine.Object.Destroy(base.gameObject);
		base.DoDelete();
	}

	public void SetStatus(PickupStatus _status, Transform parent = null)
	{
		PickupStatus pickupStatus = status;
		if (status == PickupStatus.EXCHANGING && _status == PickupStatus.IN_CONTAINER)
		{
			SetWoosh(active: false);
		}
		status = _status;
		base.transform.parent = ((parent == null) ? GameManager.instance.spawnParent : parent);
		bool flag = status == PickupStatus.ON_GROUND;
		Ground ground = null;
		if (flag != (pickupStatus == PickupStatus.ON_GROUND))
		{
			SetColliders(flag);
			if (flag)
			{
				ConnectToTrails();
				if (pickupStatus != PickupStatus.NONE)
				{
					decay = data.decay;
				}
				ground = Toolkit.GetGround(base.transform.position);
			}
			else
			{
				DisconnectFromTrails();
			}
		}
		if (ground != this.ground)
		{
			if (this.ground != null)
			{
				this.ground.RemovePickupOnGround(this);
			}
			this.ground = ground;
			if (this.ground != null)
			{
				this.ground.AddPickupOnGround(this);
			}
		}
	}

	public PickupStatus GetStatus()
	{
		return status;
	}

	public void SetColliders(bool target)
	{
		if (cols == null)
		{
			cols = GetComponentsInChildren<Collider>(includeInactive: true);
		}
		Collider[] array = cols;
		foreach (Collider collider in array)
		{
			if (!(collider == null))
			{
				collider.enabled = target;
				collider.isTrigger = true;
			}
		}
	}

	public override float GetRadius()
	{
		return height / 2f;
	}

	public bool IsOnGround(out Ground g)
	{
		g = ground;
		if (status == PickupStatus.ON_GROUND)
		{
			return g != null;
		}
		return false;
	}

	public override ExchangeType TrailInteraction(Trail _trail)
	{
		if (status == PickupStatus.ON_GROUND)
		{
			if (_trail.CanDoExchangeType(ExchangeType.PICKUP))
			{
				return ExchangeType.PICKUP;
			}
			if (data.categories.Contains(PickupCategory.ENERGY) && _trail.CanDoExchangeType(ExchangeType.FORAGE))
			{
				return ExchangeType.FORAGE;
			}
		}
		return ExchangeType.NONE;
	}

	private IEnumerable<ExchangeType> EExchangeTypes()
	{
		yield return ExchangeType.PICKUP;
		if (data.categories.Contains(PickupCategory.ENERGY))
		{
			yield return ExchangeType.FORAGE;
		}
	}

	public bool CanForage()
	{
		return data.categories.Contains(PickupCategory.ENERGY);
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		if (!is_selected || status != PickupStatus.ON_GROUND)
		{
			return;
		}
		Ground ground = Toolkit.GetGround(base.transform.position);
		if (ground != null)
		{
			GameManager.instance.TryExchangePickupToInventory(ground, base.transform.position, this);
			return;
		}
		foreach (Ground item in GameManager.instance.EGrounds())
		{
			if (GameManager.instance.TryExchangePickupToInventory(item, base.transform.position, this))
			{
				break;
			}
		}
	}

	public float Exchange(Ant target_ant, ExchangeAnimationType anim)
	{
		exchangingTargetAnt = target_ant;
		exchangingTargetContainer = null;
		return SetExchange(target_ant.carryPos.position, anim);
	}

	public float Exchange(PickupContainer target_container, Vector3 target_pos, ExchangeAnimationType anim, float wait = 0f)
	{
		exchangedFromPoint = ((target_container is ExchangePoint exchangePoint) ? exchangePoint : null);
		target_container.PrepareForPickup(this, exchangedFromPoint);
		exchangingTargetContainer = target_container;
		exchangingTargetAnt = null;
		return SetExchange(target_pos, anim, wait);
	}

	public float Exchange(Vector3 target_pos, ExchangeAnimationType anim)
	{
		exchangingTargetAnt = null;
		exchangingTargetContainer = null;
		return SetExchange(target_pos, anim);
	}

	private float SetExchange(Vector3 target_pos, ExchangeAnimationType anim, float _wait = 0f)
	{
		if (status == PickupStatus.EXCHANGING)
		{
			Debug.LogError("Pickup shouldn't start new exchange during exchange");
		}
		SetStatus(PickupStatus.EXCHANGING);
		exchangeAnim.Start(base.transform.position, target_pos, anim, _wait);
		return exchangeAnim.GetTotalDuration();
	}

	public void LocalMove(Vector3 target_pos)
	{
		if (status == PickupStatus.EXCHANGING)
		{
			Debug.LogError("Pickup shouldn't start local move during exchange");
		}
		SetStatus(PickupStatus.MOVING_LOCALLY);
		exchangeAnim.Start(base.transform.localPosition, target_pos, ExchangeAnimationType.STRAIGHT);
	}

	public ExchangeAnimation GetExchangeAnim()
	{
		return exchangeAnim;
	}

	public void PickupUpdate(float dt, bool run_world)
	{
		if (run_world)
		{
			if (decay > 0f)
			{
				decay -= dt;
				if (decay <= 0f)
				{
					GameObject obj = UnityEngine.Object.Instantiate(base.gameObject);
					UnityEngine.Object.Destroy(obj.GetComponent<Pickup>());
					obj.AddComponent<DecayEffect>();
					obj.layer = 2;
					Delete();
					return;
				}
			}
		}
		else if (!exchangeAnim.ShouldRunDuringPause())
		{
			return;
		}
		if (status != PickupStatus.EXCHANGING && status != PickupStatus.MOVING_LOCALLY)
		{
			return;
		}
		if (exchangingTargetAnt != null)
		{
			exchangeAnim.posEnd = exchangingTargetAnt.carryPos.position;
		}
		bool done;
		Vector3? vector = exchangeAnim.Update(dt, out done);
		if (vector.HasValue)
		{
			if (status == PickupStatus.MOVING_LOCALLY)
			{
				base.transform.localPosition = vector.Value;
			}
			else
			{
				base.transform.position = vector.Value;
			}
		}
		if (!done)
		{
			return;
		}
		if (exchangingTargetAnt != null)
		{
			Ant ant = exchangingTargetAnt;
			exchangingTargetAnt = null;
			ant.OnPickupArrival(this);
		}
		else if (exchangingTargetContainer != null)
		{
			PickupContainer pickupContainer = exchangingTargetContainer;
			exchangingTargetContainer = null;
			pickupContainer.OnPickupArrival(this, null);
			if (exchangingOriginBuilding != null && pickupContainer is Building building)
			{
				if (exchangingOriginBuilding.ground != building.ground)
				{
					Progress.pickupsThrownToOtherIsland++;
				}
				exchangingOriginBuilding = null;
			}
		}
		else if (Toolkit.GetGround(base.transform.position) != null)
		{
			SetStatus(PickupStatus.ON_GROUND);
		}
		else
		{
			Delete();
		}
	}

	public override void SetHoverUI(UIHoverClickOb ui_hover)
	{
		ui_hover.SetTitle(PickupData.Get(type).GetTitle());
		ui_hover.SetCapabilities(Loc.GetUI("PICKUP_COLLECTWITH"));
		foreach (TrailType item in TrailData.ExchangeTypesToTrailTypes(EExchangeTypes()))
		{
			ui_hover.AddCapability(item);
		}
		if (DebugSettings.standard.DeletableEverything() || IsLarva())
		{
			Action on_click_delete = (DebugSettings.standard.DeletableEverything() ? new Action(base.Delete) : null);
			Action on_click_follow = (IsLarva() ? ((Action)delegate
			{
				CamController.instance.ToggleFollow(base.transform);
				Gameplay.instance.Select(null);
			}) : null);
			ui_hover.SetBottomButtons(on_click_delete, null, on_click_follow);
		}
		Progress.AddSeenPickup(data.type);
	}

	public override UIClickType GetUiClickType()
	{
		return UIClickType.PICKUP;
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		UIClickLayout_BiomeObject uIClickLayout_BiomeObject = (UIClickLayout_BiomeObject)ui_click;
		uIClickLayout_BiomeObject.SetTitle(PickupData.Get(type).GetTitle());
		uIClickLayout_BiomeObject.SetCapabilities(Loc.GetUI("PICKUP_COLLECTWITH"));
		foreach (TrailType item in TrailData.ExchangeTypesToTrailTypes(EExchangeTypes()))
		{
			uIClickLayout_BiomeObject.AddCapability(item);
		}
		if (DebugSettings.standard.DeletableEverything())
		{
			uIClickLayout_BiomeObject.SetButton(UIClickButtonType.Delete, base.Delete, InputAction.Delete);
		}
		uIClickLayout_BiomeObject.UpdateButton(UIClickButtonType.Delete, DebugSettings.standard.DeletableEverything());
		if (IsLarva())
		{
			uIClickLayout_BiomeObject.SetButton(UIClickButtonType.Follow, delegate
			{
				CamController.instance.ToggleFollow(base.transform);
				Gameplay.instance.Select(null);
			}, InputAction.FollowAnt);
		}
		uIClickLayout_BiomeObject.UpdateButton(UIClickButtonType.Follow, IsLarva());
		Progress.AddSeenPickup(data.type);
	}

	public bool IsLarva()
	{
		if (type >= PickupType.LARVAE_T1)
		{
			return type <= PickupType.LARVAE_T3;
		}
		return false;
	}

	public void SetWoosh(bool active)
	{
		if (active)
		{
			StartAudioLoop(AudioLinks.standard.pickupWooshLoop);
		}
		else
		{
			ClearAudio();
		}
	}

	private void StartAudioLoop(AudioLink audio_link)
	{
		if (audio_link.IsSet())
		{
			if (audioPickup == null)
			{
				audioPickup = AudioManager.GetLooseChannel();
				audioPickup.Lock();
				audioPickup.Attach(base.transform);
				audioPickup.SetDoppler(active: true);
			}
			audioPickup.Play(audio_link, looped: true);
		}
	}

	private void ClearAudio()
	{
		if (audioPickup != null)
		{
			audioPickup.Free();
			audioPickup = null;
		}
	}

	public void SetOriginBuilding(Building _build)
	{
		exchangingOriginBuilding = _build;
	}
}
