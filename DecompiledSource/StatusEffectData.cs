using System;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectData
{
	private static Dictionary<StatusEffect, StatusEffectData> dicStatusEffectData;

	public StatusEffect statusEffect;

	public float duration;

	public float effectSpeedFactor;

	public float effectDrainFactor;

	public float effectRadiation;

	public float effectRadDeathFactor;

	public float effectDeath;

	public bool effectBlockActionPoints;

	public bool effectIsTrigger;

	public ExplosionType effectDeathExplosion;

	public int effectImmunitiesBits;

	public StatusEffect effectEffectArea;

	public float effectAreaRadius;

	public static StatusEffectData Get(StatusEffect status_effect)
	{
		if (dicStatusEffectData == null)
		{
			dicStatusEffectData = new Dictionary<StatusEffect, StatusEffectData>();
			foreach (StatusEffectData statusEffect in PrefabData.statusEffects)
			{
				dicStatusEffectData.Add(statusEffect.statusEffect, statusEffect);
			}
		}
		if (dicStatusEffectData.TryGetValue(status_effect, out var value))
		{
			return value;
		}
		Debug.LogWarning("StatusEffectData: Couldn't find status effect " + status_effect);
		if (PrefabData.statusEffects.Count == 0)
		{
			return null;
		}
		return PrefabData.statusEffects[0];
	}

	public static StatusEffect ParseStatusEffect(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return StatusEffect.NONE;
		}
		if (Enum.TryParse<StatusEffect>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("Prefabs: StatusEffect parse error; '" + str + "' invalid");
		return StatusEffect.NONE;
	}

	public static List<StatusEffect> ParseList(string str)
	{
		List<StatusEffect> list = new List<StatusEffect>();
		foreach (string item in str.EListItems())
		{
			list.Add(ParseStatusEffect(item));
		}
		return list;
	}

	public static IEnumerable<StatusEffect> EParseListStatusEffect(string str, string context = "")
	{
		foreach (string item in str.EListItems())
		{
			if (Enum.TryParse<StatusEffect>(item.ToUpper(), out var result))
			{
				yield return result;
			}
			else
			{
				Debug.LogError(context + "Don't know status effect " + item);
			}
		}
	}

	public static ExplosionType ParseExplosionType(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return ExplosionType.NONE;
		}
		if (Enum.TryParse<ExplosionType>(str.Trim(), out var result))
		{
			return result;
		}
		Debug.LogWarning("Prefabs: ExplosionType parse error; '" + str + "' invalid");
		return ExplosionType.NONE;
	}

	public string GetTitle()
	{
		return Loc.GetUI("STATUSEFFECT_" + statusEffect);
	}

	public string GetTitleMultiple(int n)
	{
		return Loc.GetUI("STATUSEFFECT_" + statusEffect.ToString() + "_MULTIPLE", n.ToString());
	}

	public string GetHover()
	{
		return Loc.GetUI("STATUSEFFECT_" + statusEffect.ToString() + "_HOVER");
	}
}
