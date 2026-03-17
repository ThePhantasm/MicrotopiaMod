using System;
using UnityEngine;

public class ExchangeAnimation
{
	[NonSerialized]
	public Vector3 posStart;

	[NonSerialized]
	public Vector3 posEnd;

	private float progressMain;

	private float progressEaseIn;

	private float arcHeight;

	private float durMain;

	private float durEaseIn;

	private float startDelay;

	private ExchangeAnimationType animType;

	private const float magnetPullSpeed = 20f;

	public void Start(Vector3 pos_start, Vector3 pos_end, ExchangeAnimationType anim_type, float start_delay = 0f)
	{
		posStart = pos_start;
		posEnd = pos_end;
		float magnitude = (pos_end - pos_start).magnitude;
		animType = anim_type;
		progressEaseIn = 0f;
		progressMain = 0f;
		startDelay = start_delay;
		switch (animType)
		{
		case ExchangeAnimationType.STRAIGHT:
			progressEaseIn = 1f;
			durMain = magnitude / 25f;
			durMain = Mathf.Clamp(durMain, 0.001f, 1f);
			break;
		case ExchangeAnimationType.ARC:
		case ExchangeAnimationType.ARC_UNSCALED:
			arcHeight = magnitude * 0.5f;
			progressEaseIn = 1f;
			durMain = magnitude / 25f;
			durMain = Mathf.Clamp(durMain, 0.001f, 1f);
			break;
		case ExchangeAnimationType.SHOOT:
			arcHeight = 15f + UnityEngine.Random.Range(-1f, 1f);
			durEaseIn = 0.25f;
			durMain = magnitude / 200f;
			durMain = Mathf.Clamp(durMain, 0.001f, 1f);
			break;
		case ExchangeAnimationType.TELEPORT:
			progressEaseIn = 1f;
			durMain = 0f;
			break;
		case ExchangeAnimationType.MAGNET_PULL:
			progressEaseIn = 1f;
			durMain = magnitude / 20f;
			break;
		}
	}

	public Vector3? Update(float dt, out bool done)
	{
		if (startDelay > 0f)
		{
			startDelay -= dt;
			done = false;
			return null;
		}
		if (progressEaseIn < 1f)
		{
			progressEaseIn = Mathf.Clamp01(progressEaseIn + dt * (1f / durEaseIn));
		}
		else
		{
			progressMain = Mathf.Clamp01(progressMain + dt * (1f / durMain));
		}
		Vector3 value = Vector3.zero;
		switch (animType)
		{
		case ExchangeAnimationType.STRAIGHT:
			value = Vector3.Lerp(posStart, posEnd, progressMain);
			break;
		case ExchangeAnimationType.ARC:
		case ExchangeAnimationType.ARC_UNSCALED:
		{
			value = Vector3.Lerp(posStart, posEnd, progressMain);
			float num = progressMain * 2f - 1f;
			value.y += (1f - num * num) * arcHeight;
			break;
		}
		case ExchangeAnimationType.SHOOT:
		{
			Vector3 vector = posStart.TargetYPosition(arcHeight);
			value = ((!(progressEaseIn < 1f)) ? Vector3.Lerp(vector, posEnd, progressMain) : Vector3.Lerp(posStart, vector, GlobalValues.standard.curveEaseInHeavy.Evaluate(progressEaseIn)));
			break;
		}
		case ExchangeAnimationType.MAGNET_PULL:
		{
			float t = GlobalValues.standard.curveEaseOutHeavy.Evaluate(progressMain);
			float t2 = GlobalValues.standard.curveEaseInHeavy.Evaluate(progressMain);
			value = Vector3.Lerp(posStart, posEnd, t).TargetYPosition(Mathf.Lerp(posStart.y, posEnd.y, t2));
			break;
		}
		}
		done = progressMain >= 1f;
		return value;
	}

	public bool IsActive()
	{
		if (!(startDelay > 0f))
		{
			return progressMain < 1f;
		}
		return true;
	}

	public float GetTotalDuration()
	{
		return startDelay + durMain;
	}

	public void Write(Save save)
	{
		save.Write((int)animType);
		save.Write(posStart);
		save.Write(posEnd);
		save.Write(progressEaseIn);
		save.Write(progressMain);
		save.Write(durEaseIn);
		save.Write(durMain);
		save.Write(startDelay);
		save.Write(arcHeight);
	}

	public void Read(Save save)
	{
		animType = (ExchangeAnimationType)save.ReadInt();
		posStart = save.ReadVector3();
		posEnd = save.ReadVector3();
		progressEaseIn = save.ReadFloat();
		progressMain = save.ReadFloat();
		durEaseIn = save.ReadFloat();
		durMain = save.ReadFloat();
		startDelay = save.ReadFloat();
		arcHeight = save.ReadFloat();
	}

	public bool ShouldRunDuringPause()
	{
		ExchangeAnimationType exchangeAnimationType = animType;
		if (exchangeAnimationType == ExchangeAnimationType.SHOOT || exchangeAnimationType == ExchangeAnimationType.ARC_UNSCALED)
		{
			return true;
		}
		return false;
	}
}
