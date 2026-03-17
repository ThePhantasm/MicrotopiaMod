using System.Collections.Generic;
using UnityEngine;

public class StatusEffects
{
	private class EffectState
	{
		public StatusEffect effect;

		public bool inArea;

		public float duration;

		private GameObject obVisual;

		private ParticleSystem psVisual;

		public EffectState(Ant _ant, StatusEffect _effect, float _duration, bool _in_area)
		{
			effect = _effect;
			duration = _duration;
			inArea = _in_area;
			GameObject statusEffectParticleEffect = AssetLinks.standard.GetStatusEffectParticleEffect(effect);
			if (statusEffectParticleEffect != null)
			{
				if (_ant.statusPos == null)
				{
					Debug.LogError("Couldn't spawn status effect visual; ant " + _ant.name + " doesn't have statusPos set");
					return;
				}
				obVisual = Object.Instantiate(statusEffectParticleEffect, _ant.statusPos);
				psVisual = obVisual.GetComponentInChildren<ParticleSystem>();
				GameManager.instance.AddPausableParticles(psVisual);
			}
		}

		public void SetParticles(bool target)
		{
			if (psVisual != null)
			{
				if (target)
				{
					psVisual.Play();
				}
				else
				{
					psVisual.Stop();
				}
			}
			else if (obVisual != null)
			{
				obVisual.SetObActive(target);
			}
		}

		public void ClearParticles()
		{
			if (obVisual != null)
			{
				if (psVisual != null)
				{
					GameManager.instance.RemovePausableParticles(psVisual);
					psVisual = null;
				}
				Object.Destroy(obVisual);
				obVisual = null;
			}
		}
	}

	private List<EffectState> effectStates = new List<EffectState>();

	private Ant ant;

	public List<StatusEffect> currentEffects = new List<StatusEffect>();

	public float speedFactor;

	public float lifeDrainFactor;

	public float radiationChange;

	public float radDeath;

	public int activeEffectBits;

	public bool blockActionPoints;

	public ExplosionType deathExplosion;

	public StatusEffect materialEffect;

	public StatusEffect currentEffectArea;

	public float currentEffectAreaRadius;

	private static bool hadSpeed50;

	private const float RADIATION_FADE = 0f;

	public StatusEffects(Ant _ant)
	{
		ant = _ant;
		speedFactor = 1f;
		lifeDrainFactor = 1f;
		radiationChange = 0f;
		blockActionPoints = false;
	}

	public void Write(Save save)
	{
		int num = 0;
		foreach (EffectState effectState in effectStates)
		{
			if (effectState.duration > 0f)
			{
				num++;
			}
		}
		save.Write(num);
		foreach (EffectState effectState2 in effectStates)
		{
			if (effectState2.duration > 0f)
			{
				save.Write((int)effectState2.effect);
				if (effectState2.inArea)
				{
					save.Write(0f - effectState2.duration);
				}
				else
				{
					save.Write(effectState2.duration);
				}
			}
		}
	}

	public void Read(Save save)
	{
		if (save.version < 9)
		{
			return;
		}
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			StatusEffect effect = (StatusEffect)save.ReadInt();
			float num2 = save.ReadFloat();
			bool flag = num2 < 0f;
			if (flag)
			{
				num2 = 0f - num2;
			}
			effectStates.Add(new EffectState(ant, effect, num2, flag));
		}
		CombineEffects();
	}

	private EffectState GetEffectState(StatusEffect effect)
	{
		foreach (EffectState effectState in effectStates)
		{
			if (effectState.effect == effect)
			{
				return effectState;
			}
		}
		return null;
	}

	private List<EffectState> GetEffectStates(StatusEffect effect)
	{
		List<EffectState> list = new List<EffectState>();
		foreach (EffectState effectState in effectStates)
		{
			if (effectState.effect == effect)
			{
				list.Add(effectState);
			}
		}
		return list;
	}

	public void Gain(StatusEffect effect, bool in_area = false)
	{
		StatusEffectData statusEffectData = StatusEffectData.Get(effect);
		EffectState effectState = GetEffectState(effect);
		if (statusEffectData.effectIsTrigger)
		{
			in_area = false;
		}
		bool flag = false;
		if (effectState == null)
		{
			effectState = new EffectState(ant, effect, statusEffectData.duration, in_area);
			effectStates.Add(effectState);
			flag = true;
		}
		else if (effectState.duration <= 0f)
		{
			effectState.duration = statusEffectData.duration;
			if (in_area)
			{
				effectState.inArea = true;
			}
			flag = true;
		}
		else
		{
			if (!statusEffectData.effectIsTrigger)
			{
				effectState.duration = statusEffectData.duration;
			}
			if (in_area)
			{
				effectState.inArea = true;
			}
		}
		if (flag)
		{
			if (statusEffectData.effectDeath != 0f)
			{
				ant.deathTimer = statusEffectData.effectDeath;
			}
			effectState.SetParticles(target: true);
			if (statusEffectData.effectEffectArea != StatusEffect.NONE)
			{
				ant.GainEffectArea(statusEffectData.effectEffectArea, statusEffectData.effectAreaRadius);
			}
		}
		CombineEffects();
	}

	public void Lose(StatusEffect effect)
	{
		EffectState effectState = GetEffectState(effect);
		if (effectState == null)
		{
			Debug.LogError("Tried losing effect " + effect.ToString() + " but returned null");
		}
		else
		{
			Lose(effectState);
		}
	}

	private void Lose(EffectState effect_state)
	{
		effect_state.duration = 0f;
		effect_state.SetParticles(target: false);
		StatusEffectData statusEffectData = StatusEffectData.Get(effect_state.effect);
		if (statusEffectData.effectEffectArea != StatusEffect.NONE)
		{
			ant.LoseEffectArea(statusEffectData.effectEffectArea);
		}
		CombineEffects();
	}

	private void CombineEffects()
	{
		currentEffects.Clear();
		speedFactor = 1f;
		lifeDrainFactor = 1f;
		radiationChange = 0f;
		radDeath = 0f;
		blockActionPoints = false;
		activeEffectBits = 0;
		deathExplosion = ExplosionType.NONE;
		int num = 0;
		foreach (EffectState effectState2 in effectStates)
		{
			if (effectState2.duration > 0f)
			{
				StatusEffect effect = effectState2.effect;
				currentEffects.Add(effect);
				StatusEffectData statusEffectData = StatusEffectData.Get(effect);
				speedFactor *= statusEffectData.effectSpeedFactor;
				lifeDrainFactor *= statusEffectData.effectDrainFactor;
				radiationChange += statusEffectData.effectRadiation;
				radDeath += statusEffectData.effectRadDeathFactor;
				if (statusEffectData.effectBlockActionPoints)
				{
					blockActionPoints = true;
				}
				activeEffectBits |= 1 << (int)effect;
				if (statusEffectData.effectDeathExplosion != ExplosionType.NONE)
				{
					deathExplosion = statusEffectData.effectDeathExplosion;
				}
				num |= statusEffectData.effectImmunitiesBits;
			}
		}
		if (!hadSpeed50 && speedFactor > 1.5f && ant.GetSpeed() * speedFactor >= 50f)
		{
			Platform.current.GainAchievement(Achievement.SPEED_50);
			hadSpeed50 = true;
		}
		if (radiationChange == 0f)
		{
			radiationChange = -0f;
		}
		if (currentEffects.Contains(StatusEffect.RADIATED_HEAVY))
		{
			materialEffect = StatusEffect.RADIATED_HEAVY;
		}
		else if (currentEffects.Contains(StatusEffect.RADIATED_MEDIUM))
		{
			materialEffect = StatusEffect.RADIATED_MEDIUM;
		}
		else if (currentEffects.Contains(StatusEffect.RADIATED_LIGHT))
		{
			materialEffect = StatusEffect.RADIATED_LIGHT;
		}
		else if (currentEffects.Contains(StatusEffect.OLD))
		{
			materialEffect = StatusEffect.OLD;
		}
		ant.UpdateMaterial();
		if (num == 0 || !ant.AddImmunity(num))
		{
			return;
		}
		for (int num2 = effectStates.Count - 1; num2 >= 0; num2--)
		{
			EffectState effectState = effectStates[num2];
			if (effectState.duration > 0f && (num & (1 << (int)effectState.effect)) != 0)
			{
				Lose(effectState);
			}
		}
	}

	public void ApplyAreaEffectBits(int effect_bits)
	{
		int num = ~activeEffectBits & effect_bits;
		int num2 = activeEffectBits & ~effect_bits;
		int num3 = 0;
		while (num != 0)
		{
			if ((num & 1) == 1)
			{
				Gain((StatusEffect)num3, in_area: true);
			}
			num >>= 1;
			num3++;
		}
		num3 = 0;
		while (num2 != 0)
		{
			if ((num2 & 1) == 1)
			{
				GetEffectState((StatusEffect)num3).inArea = false;
			}
			num2 >>= 1;
			num3++;
		}
	}

	public void Process(float dt)
	{
		foreach (EffectState effectState in effectStates)
		{
			if (effectState.duration > 0f && !effectState.inArea)
			{
				effectState.duration -= dt;
				if (effectState.duration <= 0f)
				{
					Lose(effectState);
				}
			}
		}
	}

	public void Clear()
	{
		foreach (EffectState effectState in effectStates)
		{
			effectState.ClearParticles();
		}
		effectStates.Clear();
		CombineEffects();
	}
}
