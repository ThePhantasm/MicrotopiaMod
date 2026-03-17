using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EffectArea
{
	private static List<EffectAreaInfo> effectAreaInfosList = new List<EffectAreaInfo>();

	public static EffectAreaInfo[] effectAreaInfos = new EffectAreaInfo[0];

	private static bool effectAreaInfosChanged;

	public StatusEffect statusEffect;

	public float radius;

	private bool active;

	private EffectAreaInfo effectAreaInfo;

	public EffectArea(StatusEffect _status_effect, float _radius)
	{
		statusEffect = _status_effect;
		radius = _radius;
	}

	public void SetActive(bool _active, Vector3 pos, bool force = false)
	{
		SetActive(_active, force);
		UpdatePos(pos);
	}

	public void SetActive(bool _active, bool force = false)
	{
		if (active != _active || force)
		{
			if (effectAreaInfo == null)
			{
				effectAreaInfo = new EffectAreaInfo(this);
			}
			active = _active;
			effectAreaInfosChanged = true;
			if (active)
			{
				effectAreaInfosList.Add(effectAreaInfo);
			}
			else
			{
				effectAreaInfosList.Remove(effectAreaInfo);
			}
		}
	}

	public static void Update()
	{
		if (effectAreaInfosChanged)
		{
			effectAreaInfos = effectAreaInfosList.ToArray();
		}
	}

	public void SetAntReaction(Action<Ant> ant_reaction)
	{
		if (effectAreaInfo != null)
		{
			effectAreaInfo.antReaction = ant_reaction;
		}
	}

	public void UpdatePos(Vector3 pos)
	{
		effectAreaInfo?.SetPos(pos);
	}
}
