using System;
using System.Collections.Generic;
using UnityEngine;

public class CargoAnt : Ant
{
	private enum AntExchangeState
	{
		None,
		PickingUp,
		Dropping
	}

	[Header("Cargo Ant")]
	[SerializeField]
	private Renderer[] centipedeHeadRenderers;

	[NonSerialized]
	public CargoAnt centipedeLeader;

	[NonSerialized]
	public CargoAnt centipedeFollower;

	[NonSerialized]
	public CargoAnt centipedeHead;

	private int centipedeHeadLinkId;

	private int centipedeLeaderLinkId;

	private int centipedeFollowerLinkId;

	private const int N_PREV_TRAILS = 3;

	private Trail[] prevTrails = new Trail[3];

	private int prevTrailIndex;

	private ExchangeAnimation antExchangeAnimation = new ExchangeAnimation();

	private AntExchangeState antExchangeState;

	public Ant carriedAnt;

	private int carriedAntLinkId;

	private Trail dropTrail;

	private const float CENTIPEDE_GAP = -0.5f;

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if (centipedeHead == null)
		{
			SetCentipedeConnection(this, null, null);
		}
	}

	public override void Write(Save save)
	{
		save.Write((!(centipedeHead == null)) ? centipedeHead.linkId : 0);
		save.Write((!(centipedeLeader == null)) ? centipedeLeader.linkId : 0);
		save.Write((!(centipedeFollower == null)) ? centipedeFollower.linkId : 0);
		int num = prevTrailIndex;
		for (int i = 0; i < 3; i++)
		{
			save.Write((!(prevTrails[num] == null)) ? prevTrails[num].linkId : 0);
			num++;
			if (num == 3)
			{
				num = 0;
			}
		}
		save.Write((!(carriedAnt == null)) ? carriedAnt.linkId : 0);
		base.Write(save);
	}

	public override void Read(Save save)
	{
		centipedeHeadLinkId = save.ReadInt();
		centipedeLeaderLinkId = save.ReadInt();
		centipedeFollowerLinkId = save.ReadInt();
		keepCommandTrail = centipedeFollowerLinkId > 0;
		for (int i = 0; i < 3; i++)
		{
			prevTrails[i] = GameManager.instance.FindLink<Trail>(save.ReadInt());
		}
		prevTrailIndex = 0;
		carriedAntLinkId = save.ReadInt();
		base.Read(save);
	}

	public override void LoadLinks()
	{
		base.LoadLinks();
		SetCentipedeConnection((centipedeHeadLinkId == 0) ? null : GameManager.instance.FindLink<CargoAnt>(centipedeHeadLinkId), (centipedeLeaderLinkId == 0) ? null : GameManager.instance.FindLink<CargoAnt>(centipedeLeaderLinkId), (centipedeFollowerLinkId == 0) ? null : GameManager.instance.FindLink<CargoAnt>(centipedeFollowerLinkId));
		carriedAnt = ((carriedAntLinkId == 0) ? null : GameManager.instance.FindLink<Ant>(carriedAntLinkId));
		if (carriedAnt != null)
		{
			DirectCarry(carriedAnt);
		}
	}

	public void SetCentipedeConnection(CargoAnt head, CargoAnt leader, CargoAnt follower)
	{
		centipedeHead = head;
		centipedeLeader = leader;
		centipedeFollower = follower;
		if (centipedeLeader != null)
		{
			HideHead();
		}
		keepCommandTrail = centipedeFollower != null;
		isHeadless = centipedeLeader != null;
	}

	public void HideHead()
	{
		for (int i = 0; i < centipedeHeadRenderers.Length; i++)
		{
			centipedeHeadRenderers[i].enabled = false;
		}
	}

	public override void AntUpdate(float dt)
	{
		switch (antExchangeState)
		{
		case AntExchangeState.PickingUp:
			PickingUp(dt);
			break;
		case AntExchangeState.Dropping:
			Dropping(dt);
			break;
		}
		base.AntUpdate(dt);
	}

	protected override void MoveOnTrail(float dt)
	{
		if (centipedeLeader != null && centipedeLeader.currentTrail != currentTrail && !centipedeLeader.WasOnTrail(currentTrail))
		{
			SetCurrentTrail(null);
		}
		else
		{
			base.MoveOnTrail(dt);
		}
	}

	protected override bool CanContinueOnTrail(ref float progress, out string warning)
	{
		if (centipedeLeader != null && centipedeLeader.currentTrail == currentTrail && centipedeLeader.trailProgress < trailProgress)
		{
			progress = trailProgress;
			warning = "";
			return false;
		}
		return base.CanContinueOnTrail(ref progress, out warning);
	}

	protected override float GetAntGap(Ant ant)
	{
		if (ant is CargoAnt other && CentipedeAntIsTrailingMe(other))
		{
			return -1000f;
		}
		if (!(ant == centipedeLeader))
		{
			return 2f;
		}
		return -0.5f;
	}

	protected override Trail ChooseNextTrail(bool final, out string warning)
	{
		if (centipedeLeader == null)
		{
			return base.ChooseNextTrail(final, out warning);
		}
		warning = "";
		if (centipedeLeader.currentTrail == null)
		{
			return null;
		}
		Split splitEnd = currentTrail.splitEnd;
		foreach (Trail connectedTrail in splitEnd.connectedTrails)
		{
			if (!(connectedTrail.splitStart != splitEnd) && (centipedeLeader.currentTrail == connectedTrail || centipedeLeader.WasOnTrail(connectedTrail)))
			{
				return connectedTrail;
			}
		}
		return null;
	}

	public override void SetCurrentTrail(Trail _trail, float progress = float.MinValue)
	{
		if (_trail == currentTrail)
		{
			return;
		}
		if (_trail == null)
		{
			for (int i = 0; i < 3; i++)
			{
				prevTrails[i] = null;
			}
		}
		else
		{
			prevTrails[prevTrailIndex++] = currentTrail;
			if (prevTrailIndex == 3)
			{
				prevTrailIndex = 0;
			}
		}
		base.SetCurrentTrail(_trail, progress);
	}

	public bool WasOnTrail(Trail trail)
	{
		for (int i = 0; i < 3; i++)
		{
			if (prevTrails[i] == trail)
			{
				return true;
			}
		}
		return false;
	}

	public override float GetSpeed()
	{
		if (centipedeLeader != null)
		{
			return base.GetSpeed() * 1.1f;
		}
		return base.GetSpeed();
	}

	private bool CentipedeAntIsTrailingMe(CargoAnt other)
	{
		if (other.centipedeHead == null || other.centipedeHead != centipedeHead)
		{
			return false;
		}
		CargoAnt cargoAnt = centipedeFollower;
		while (cargoAnt != null)
		{
			if (cargoAnt == other)
			{
				return true;
			}
			cargoAnt = cargoAnt.centipedeFollower;
		}
		return false;
	}

	protected override void RotateTowards(Vector3 dir, float dt)
	{
		if (centipedeLeader == null)
		{
			base.RotateTowards(dir, dt);
			return;
		}
		Vector3 dir2 = centipedeLeader.transform.position - base.transform.position;
		base.RotateTowards(dir2, dt);
	}

	public IEnumerable<CargoAnt> EAllSubAnts()
	{
		if (centipedeHead == null)
		{
			yield return this;
			yield break;
		}
		CargoAnt a = centipedeHead;
		while (a != null)
		{
			yield return a;
			a = a.centipedeFollower;
		}
	}

	protected override void AntUpdateHoverUI(UIHoverClickOb ui_hover)
	{
		if (!IsImmortal())
		{
			float num = centipedeHead.energy;
			float num2 = centipedeHead.energyTotal;
			ui_hover.UpdateHealth($"{num:0.0}", num / num2);
		}
		ui_hover.ShowButtonWithText(target: false);
	}

	public override void Die(DeathCause _cause)
	{
		if (carriedAnt != null)
		{
			carriedAnt.transform.position = carriedAnt.transform.position.ZeroPosition();
			carriedAnt.transform.SetParent(null, worldPositionStays: true);
			carriedAnt.SetMoveState(MoveState.Normal);
		}
		if (centipedeHead == this)
		{
			foreach (CargoAnt item in new List<CargoAnt>(EAllSubAnts()))
			{
				if (item != this)
				{
					item.Die(_cause);
				}
			}
		}
		base.Die(_cause);
	}

	public void PickupAnt(Ant ant)
	{
		ant.SetMoveState(MoveState.Carried);
		ant.SetCurrentTrail(null);
		carriedAnt = ant;
		SetCarryAnim(target: true);
		antExchangeAnimation.Start(ant.transform.position, carryPos.position, ExchangeAnimationType.ARC);
		antExchangeState = AntExchangeState.PickingUp;
	}

	private void PickingUp(float dt)
	{
		antExchangeAnimation.posEnd = carryPos.position;
		bool done;
		Vector3? vector = antExchangeAnimation.Update(dt, out done);
		if (done)
		{
			carriedAnt.transform.SetParent(carryPos);
			carriedAnt.transform.localPosition = Vector3.zero;
			carriedAnt.transform.localRotation = Quaternion.identity;
			antExchangeState = AntExchangeState.None;
		}
		else if (vector.HasValue)
		{
			carriedAnt.transform.position = vector.Value;
		}
	}

	public void DirectCarry(Ant ant)
	{
		carriedAnt = ant;
		ant.transform.SetParent(carryPos);
		ant.transform.localPosition = Vector3.zero;
		ant.transform.localRotation = Quaternion.identity;
		SetCarryAnim(target: true);
	}

	public void DropAnt(Ant ant, Trail trail)
	{
		ant.transform.SetParent(null);
		SetCarryAnim(target: false);
		dropTrail = trail;
		ant.transform.rotation = Quaternion.LookRotation(dropTrail.direction);
		antExchangeAnimation.Start(ant.transform.position, dropTrail.posStart, ExchangeAnimationType.ARC);
		antExchangeState = AntExchangeState.Dropping;
	}

	private void Dropping(float dt)
	{
		bool done;
		Vector3? vector = antExchangeAnimation.Update(dt, out done);
		if (done)
		{
			carriedAnt.SetMoveState(MoveState.Normal);
			carriedAnt.transform.position = dropTrail.posStart;
			carriedAnt.SetCurrentTrail(dropTrail);
			carriedAnt = null;
			antExchangeState = AntExchangeState.None;
		}
		else if (vector.HasValue)
		{
			carriedAnt.transform.position = vector.Value;
		}
	}
}
