using System;
using UnityEngine;

[Serializable]
public class HungerTier
{
	public string name;

	[Tooltip("Tier for energy below this value")]
	public float ifBelow;

	public float larvaPerMinute;

	[Tooltip("Speed of the queen spawn animation")]
	public float animationSpeed = 1f;

	public int maxPopulation;

	[Tooltip("Energy drain per second")]
	public float drain;

	public Color color;
}
