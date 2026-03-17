using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BiomeElement
{
	[NonSerialized]
	public BiomeArea area;

	public Biome.Element element;

	public bool disabled;

	[Tooltip("Minimum size factor")]
	public float minSize = 0.5f;

	[Tooltip("Maximum size factor")]
	public float maxSize = 1.5f;

	[Tooltip("1 = completely random, 0 = depending on distribution strength")]
	[Range(0f, 1f)]
	public float sizeRandomness = 1f;

	[Tooltip("Range around which following instances of the same element are blocked (at size factor 1)")]
	public float radiusBlockOwn;

	[Tooltip("Range around which new elements are blocked (at size factor 1)")]
	public float radiusBlockOthers;

	[Tooltip("Minimum amount to spawn")]
	public int minAmount;

	[Tooltip("Maximum amount to spawn (0 = unlimited)")]
	public int maxAmount;

	public Distribution distribution;

	[NonSerialized]
	public List<Vector2Int> spawned;

	public BiomeElement(BiomeArea _area)
	{
		area = _area;
		distribution = new Distribution();
		minSize = 0.5f;
		maxSize = 1.5f;
		sizeRandomness = 1f;
		radiusBlockOwn = 1f;
		maxAmount = 0;
	}
}
