using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem psExplosion;

	[SerializeField]
	private Collider col;

	[SerializeField]
	private float duration = 0.5f;

	[SerializeField]
	private float launchPower = 50f;

	[SerializeField]
	private float randomness = 10f;

	[SerializeField]
	private float radiation;

	[SerializeField]
	private AudioLink audioExplosion;

	[SerializeField]
	private float audioPitchChange;

	private List<Collider> hitCols = new List<Collider>();

	private List<Ant> launchedAnts = new List<Ant>();

	private float time;

	public void Init()
	{
		if (audioExplosion.IsSet())
		{
			AudioChannel looseChannel = AudioManager.GetLooseChannel();
			looseChannel.SetPos(base.transform.position);
			looseChannel.SetPitch(Random.Range(1f - audioPitchChange, 1f + audioPitchChange));
			looseChannel.Play(audioExplosion);
		}
	}

	public void EffectUpdate(float dt)
	{
		if (dt == 0f)
		{
			psExplosion.Pause();
		}
		else if (psExplosion.isPaused)
		{
			psExplosion.Play();
		}
		time += dt;
		if (time >= duration)
		{
			GameManager.instance.DeleteExplosion(this);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (launchPower != 0f && other != col && !hitCols.Contains(other))
		{
			hitCols.Add(other);
			Ant componentInParent = other.GetComponentInParent<Ant>();
			if (componentInParent != null && !launchedAnts.Contains(componentInParent))
			{
				LaunchAnt(componentInParent);
			}
		}
	}

	public void LaunchAnt(Ant _ant)
	{
		if (!launchedAnts.Contains(_ant))
		{
			launchedAnts.Add(_ant);
			MoveState moveState = _ant.moveState;
			if ((uint)(moveState - 7) > 1u)
			{
				Vector3 vector = Toolkit.LookVectorNormalized(base.transform.position.TargetYPosition(base.transform.position.y - 10f), _ant.transform.position);
				Vector3 vector2 = Quaternion.AngleAxis(Random.Range(0f, randomness), Random.onUnitSphere) * vector;
				_ant.StartLaunch(vector2 * launchPower, LaunchCause.EXPLOSION);
				_ant.AddRadiation(radiation);
			}
		}
	}
}
