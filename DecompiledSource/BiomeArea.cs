using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BiomeArea
{
	public string name;

	public bool disabled;

	[Tooltip("Value above this blocks following areas (0 = steal whole area even for very low values; 1 = don't block, allow mixing")]
	[Range(0f, 1f)]
	public float blockThreshold;

	[Tooltip("Color for debug area map")]
	public Color showColor;

	public Distribution distribution;

	public List<BiomeElement> elements;

	public BiomeArea()
	{
		elements = new List<BiomeElement>();
		distribution = new Distribution();
		showColor = Color.gray;
		blockThreshold = 1f;
	}
}
