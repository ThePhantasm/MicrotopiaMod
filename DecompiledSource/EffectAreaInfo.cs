using System;
using UnityEngine;

public class EffectAreaInfo
{
	public float x;

	public float z;

	public float radiusSq;

	public int effectBit;

	public Action<Ant> antReaction;

	public EffectAreaInfo(EffectArea effect_area)
	{
		radiusSq = effect_area.radius * effect_area.radius;
		effectBit = 1 << (int)effect_area.statusEffect;
	}

	public void SetPos(Vector3 pos)
	{
		float num = pos.x;
		float num2 = pos.z;
		x = num;
		z = num2;
	}
}
