using System;
using UnityEngine;

[Serializable]
public class BiomeLighting
{
	[Range(0f, 1f)]
	public float fogIntensity = 0.839f;

	public Color fogColorStart = new Color(0f, 0.027f, 0.012f);

	public Color fogColorEnd = new Color(0.859f, 0.859f, 0.859f);

	public Color fogDirectionalColor = new Color(0f, 0f, 0f);

	[Range(1f, 8f)]
	public float fogDistanceFalloff = 3.95f;

	[Range(0f, 1f)]
	public float fogNoiseIntensity = 1f;

	public float fogNoiseDistanceEnd = -1f;

	public float fogNoiseScale = 120f;

	public Vector3 fogNoiseSpeed = new Vector3(0.1f, 0.2f, 0.1f);

	[Range(0f, 1f)]
	public float skyboxFogIntensity = 1f;

	public Color skyboxColor = new Color(0f, 0f, 0f);

	[Range(0f, 1f)]
	public float vignetteIntensity = 0.377f;

	public Color vignetteColor = new Color(0f, 0f, 0f);

	[Range(0f, 5f)]
	public float bloomIntensity = 0.8f;

	[Range(0f, 1f)]
	public float bloomThreshold = 0.8f;

	public Color bloomTint = new Color(0.22f, 0.34f, 0.43f);

	[Range(-100f, 100f)]
	public float colorAdjustmentsSaturation = 95f;

	public Color colorAdjustmentsColorFilter = new Color(0.95f, 0.84f, 0.87f);

	[Range(-100f, 100f)]
	public float colorAdjustmentsContrast = 27f;

	public float colorAdjustmentsPostExposure = 2f;

	[Range(0f, 1f)]
	public float chromaticAberrationIntensity;

	[Range(0f, 1f)]
	public float filmGrainIntensity;

	[Range(0f, 1f)]
	public float filmGrainResponse;

	[NonSerialized]
	public float checksumPrev;

	public float Checksum()
	{
		return fogIntensity + fogColorStart.Checksum() + fogColorEnd.Checksum() + skyboxFogIntensity + skyboxColor.Checksum() + vignetteIntensity + vignetteColor.Checksum() + bloomIntensity + bloomThreshold + bloomTint.Checksum() + colorAdjustmentsSaturation + colorAdjustmentsColorFilter.Checksum() + colorAdjustmentsContrast + fogDirectionalColor.Checksum() + fogNoiseIntensity + fogNoiseDistanceEnd + fogNoiseScale + fogNoiseSpeed.Checksum() + colorAdjustmentsPostExposure + fogDistanceFalloff;
	}

	public static BiomeLighting Lerp(BiomeLighting l1, BiomeLighting l2, float f)
	{
		BiomeLighting obj = new BiomeLighting
		{
			fogIntensity = Mathf.Lerp(l1.fogIntensity, l2.fogIntensity, f),
			fogColorStart = Color.Lerp(l1.fogColorStart, l2.fogColorStart, f),
			fogColorEnd = Color.Lerp(l1.fogColorEnd, l2.fogColorEnd, f),
			skyboxFogIntensity = Mathf.Lerp(l1.skyboxFogIntensity, l2.skyboxFogIntensity, f),
			skyboxColor = Color.Lerp(l1.skyboxColor, l2.skyboxColor, f),
			vignetteIntensity = Mathf.Lerp(l1.vignetteIntensity, l2.vignetteIntensity, f),
			vignetteColor = Color.Lerp(l1.vignetteColor, l2.vignetteColor, f),
			bloomIntensity = Mathf.Lerp(l1.bloomIntensity, l2.bloomIntensity, f),
			bloomThreshold = Mathf.Lerp(l1.bloomThreshold, l2.bloomThreshold, f),
			bloomTint = Color.Lerp(l1.bloomTint, l2.bloomTint, f),
			colorAdjustmentsSaturation = Mathf.Lerp(l1.colorAdjustmentsSaturation, l2.colorAdjustmentsSaturation, f),
			colorAdjustmentsColorFilter = Color.Lerp(l1.colorAdjustmentsColorFilter, l2.colorAdjustmentsColorFilter, f),
			colorAdjustmentsContrast = Mathf.Lerp(l1.colorAdjustmentsContrast, l2.colorAdjustmentsContrast, f),
			colorAdjustmentsPostExposure = Mathf.Lerp(l1.colorAdjustmentsPostExposure, l2.colorAdjustmentsPostExposure, f),
			fogDirectionalColor = Color.Lerp(l1.fogDirectionalColor, l2.fogDirectionalColor, f),
			fogNoiseIntensity = Mathf.Lerp(l1.fogNoiseIntensity, l2.fogNoiseIntensity, f),
			fogNoiseDistanceEnd = Mathf.Lerp(l1.fogNoiseDistanceEnd, l2.fogNoiseDistanceEnd, f),
			fogDistanceFalloff = Mathf.Lerp(l1.fogDistanceFalloff, l2.fogDistanceFalloff, f)
		};
		BiomeLighting biomeLighting = ((f < 0.5f) ? l1 : l2);
		obj.fogNoiseScale = biomeLighting.fogNoiseScale;
		obj.fogNoiseSpeed = biomeLighting.fogNoiseSpeed;
		return obj;
	}
}
