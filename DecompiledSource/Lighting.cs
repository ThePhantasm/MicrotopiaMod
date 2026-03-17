using AtmosphericHeightFog;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Lighting : MonoBehaviour
{
	public static Lighting instance;

	[SerializeField]
	private HeightFogGlobal heightFog;

	[SerializeField]
	private Camera mainCam;

	[SerializeField]
	private Volume volume;

	private Vignette vignette;

	private Bloom bloom;

	private ColorAdjustments colorAdjustments;

	private ChromaticAberration chromaticAberration;

	private FilmGrain filmGrain;

	private float fogDistanceEndOrig;

	private void Awake()
	{
		instance = this;
	}

	public void Init()
	{
		if ((Object)(object)heightFog == null)
		{
			Debug.LogWarning("Height fog not set");
		}
		if (volume == null)
		{
			Debug.LogWarning("Volume not set");
		}
		else
		{
			if (!volume.profile.TryGet<Vignette>(out vignette))
			{
				Debug.LogWarning("Couldn't get Vignette effect from volume");
			}
			if (!volume.profile.TryGet<Bloom>(out bloom))
			{
				Debug.LogWarning("Couldn't get Bloom effect from volume");
			}
			if (!volume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
			{
				Debug.LogWarning("Couldn't get ColorAdjustments effect from volume");
			}
			if (!volume.profile.TryGet<ChromaticAberration>(out chromaticAberration))
			{
				Debug.LogWarning("Couldn't get ChromaticAberration effect from volume");
			}
			if (!volume.profile.TryGet<FilmGrain>(out filmGrain))
			{
				Debug.LogWarning("Couldn't get FilmGrain effect from volume");
			}
		}
		if ((Object)(object)heightFog != null)
		{
			fogDistanceEndOrig = heightFog.fogDistanceEnd;
		}
	}

	public void SetFogOffset(float offset)
	{
		if ((Object)(object)heightFog != null)
		{
			heightFog.fogDistanceEnd = fogDistanceEndOrig + offset;
		}
	}

	public void Apply(BiomeLighting l, bool hide_fog = false)
	{
		if ((Object)(object)heightFog != null)
		{
			heightFog.fogIntensity = (hide_fog ? 0f : (l.fogIntensity * (Player.lessFog ? 0.5f : 1f)));
			heightFog.fogColorStart = l.fogColorStart;
			heightFog.fogColorEnd = l.fogColorEnd;
			heightFog.skyboxFogIntensity = (hide_fog ? 0f : l.skyboxFogIntensity);
			heightFog.directionalColor = l.fogDirectionalColor;
			heightFog.noiseIntensity = l.fogNoiseIntensity;
			heightFog.noiseDistanceEnd = l.fogNoiseDistanceEnd;
			heightFog.noiseScale = l.fogNoiseScale;
			heightFog.noiseSpeed = l.fogNoiseSpeed;
			heightFog.fogDistanceFalloff = l.fogDistanceFalloff;
		}
		mainCam.backgroundColor = l.skyboxColor;
		if (vignette != null)
		{
			volume.profile.TryGet<Vignette>(out vignette);
			vignette.intensity.Override(hide_fog ? 0f : l.vignetteIntensity);
			vignette.color.Override(l.vignetteColor);
		}
		if (bloom != null)
		{
			bloom.intensity.Override(l.bloomIntensity);
			bloom.threshold.Override(l.bloomThreshold);
			bloom.tint.Override(l.bloomTint);
		}
		if (colorAdjustments != null)
		{
			colorAdjustments.saturation.Override(l.colorAdjustmentsSaturation);
			colorAdjustments.colorFilter.Override(l.colorAdjustmentsColorFilter);
			colorAdjustments.contrast.Override(l.colorAdjustmentsContrast);
			colorAdjustments.postExposure.Override(l.colorAdjustmentsPostExposure);
		}
		if (chromaticAberration != null)
		{
			chromaticAberration.intensity.Override(l.chromaticAberrationIntensity);
		}
		if (filmGrain != null)
		{
			filmGrain.intensity.Override(l.filmGrainIntensity);
			filmGrain.response.Override(l.filmGrainResponse);
		}
	}
}
