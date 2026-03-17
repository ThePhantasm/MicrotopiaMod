using System.Collections.Generic;
using UnityEngine;

public class Hunger
{
	public static Hunger main;

	private Queen queen;

	private float waitNextDrain;

	private int curTier;

	private int curMaxPopulation;

	private float curDrain;

	private float curSpawnDuration;

	private float curAnimSpeed;

	private float curLarvaRate;

	private bool curOvercharge;

	private List<HungerTier> tiers;

	public int maxPopulation => curMaxPopulation;

	public float queenAnimationSpeed => curAnimSpeed;

	public float larvaRate => curLarvaRate;

	private float energy
	{
		get
		{
			return queen.energy;
		}
		set
		{
			queen.energy = value;
		}
	}

	public Hunger(Queen queen)
	{
		main = this;
		this.queen = queen;
		tiers = GlobalValues.standard.hungerTiers;
		if (energy < 0f)
		{
			energy = GlobalValues.standard.hungerEnergyStart;
		}
		waitNextDrain = 1f;
		SetTier(GetTier(energy));
		UpdateHungerUI();
	}

	private int GetTier(float e)
	{
		if (DebugSettings.standard.cheatHungerTier > 0)
		{
			return DebugSettings.standard.cheatHungerTier;
		}
		int i;
		for (i = 0; i < tiers.Count; i++)
		{
			if (e < tiers[i].ifBelow)
			{
				return i;
			}
		}
		e -= tiers[^1].ifBelow;
		float overchargeTierPer = GlobalValues.standard.overchargeTierPer;
		while (e >= overchargeTierPer)
		{
			i++;
			e -= overchargeTierPer;
		}
		return i;
	}

	private void SetTier(int tier)
	{
		curTier = tier;
		curOvercharge = tier >= tiers.Count;
		curAnimSpeed = tiers[tier].animationSpeed;
		curDrain = GetCurrentDrain();
		if (curOvercharge)
		{
			int num = tier - (tiers.Count - 1);
			curMaxPopulation = tiers[^1].maxPopulation + num * GlobalValues.standard.overchargeMaxPopulationGain;
		}
		else
		{
			curMaxPopulation = tiers[tier].maxPopulation;
			curLarvaRate = tiers[tier].larvaPerMinute;
		}
		queen.UpdateAnimationSpeed();
	}

	public void Process(float dt)
	{
		waitNextDrain -= dt;
		if (!(waitNextDrain > 0f))
		{
			curDrain = GetCurrentDrain();
			energy = Mathf.Clamp(energy - curDrain, 0f, float.MaxValue);
			EnergyChanged();
			waitNextDrain = 1f;
		}
	}

	private float GetCurrentDrain()
	{
		if (curOvercharge)
		{
			int num = curTier - (tiers.Count - 1);
			return tiers[^1].drain + (float)num * GlobalValues.standard.overchargeDrainGain;
		}
		float num2 = 0f;
		if (curTier > 0)
		{
			num2 = tiers[curTier - 1].ifBelow;
		}
		float ifBelow = tiers[curTier].ifBelow;
		float num3 = energy;
		float num4 = 0f;
		if (curTier > 0)
		{
			num4 = tiers[curTier - 1].drain;
		}
		float drain = tiers[curTier].drain;
		return num4 + (drain - num4) * ((num3 - num2) / (ifBelow - num2));
	}

	public void EnergyChanged()
	{
		int tier = GetTier(energy);
		if (tier != curTier)
		{
			SetTier(tier);
		}
		UpdateHungerUI();
	}

	private void UpdateHungerUI()
	{
		if (!curOvercharge)
		{
			float frac = 0f;
			if (DebugSettings.standard.cheatHungerTier > 0 || curTier == tiers.Count - 1)
			{
				frac = 1f;
			}
			else if (curTier != 0)
			{
				float ifBelow = tiers[curTier - 1].ifBelow;
				float ifBelow2 = tiers[curTier].ifBelow;
				frac = Mathf.InverseLerp(ifBelow, ifBelow2, energy);
			}
			UIGame.instance.UpdateHungerBar(curTier, frac, tiers[curTier].color, energy, curDrain, tiers[curTier].larvaPerMinute);
		}
	}
}
