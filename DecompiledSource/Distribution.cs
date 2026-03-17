using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Distribution
{
	[HideInInspector]
	public DistributionType type;

	public bool selectThis;

	[Tooltip("Inverse the pattern")]
	public bool inverse;

	[Tooltip("Blob edge hardness")]
	[Range(0f, 1f)]
	public float contrast;

	[Tooltip("Extra multiplied value")]
	[Range(0f, 2f)]
	public float strength;

	[Tooltip("If non empty, distributions with the same tag use the same seed")]
	public string tag;

	[Tooltip("Blob sizes; 1 = very small, 50 = very large")]
	[Range(0f, 1f)]
	public float perlinSize;

	[Tooltip("Treat perlin valleys as hills (doubling the amount of blobs)")]
	public bool flipNegative;

	[Tooltip("Lower threshold; values below this become 0")]
	[Range(0f, 1f)]
	public float lowerThreshold;

	[Tooltip("Upper threshold; values above this become 1")]
	[Range(0f, 1f)]
	public float upperThreshold;

	[Tooltip("Use simplex instead of perlin (bit different pattern)")]
	public bool useSimplex;

	[Tooltip("If more than one, combine multiple noises for more detail")]
	[Range(1f, 4f)]
	public int nOctaves;

	[Tooltip("How much the frequency changes for each octave (default 2 = blobs get twice as small for each octave)")]
	[Range(1f, 4f)]
	public float lacunarity;

	[Tooltip("How much the amplitude changes for each octave (default .5 = added values are halved for each octave")]
	[Range(0f, 2f)]
	public float persistence;

	public MinMax innerRadius;

	public MinMax outerRadius;

	public MinMax centerLocation;

	public string templateName;

	public Biome.Element nearElement;

	public MinMax amount;

	[NonSerialized]
	public float checksumPrev;

	private static Dictionary<string, int> tagSeeds = new Dictionary<string, int>();

	private static Simplex simplex;

	private int seed;

	public Distribution()
	{
		perlinSize = 0.5f;
		lowerThreshold = 0.5f;
		upperThreshold = 1f;
		contrast = 0.5f;
		strength = 1f;
		type = DistributionType.PERLIN;
		nOctaves = 2;
		lacunarity = 2f;
		persistence = 0.5f;
		innerRadius = new MinMax(0f);
		outerRadius = new MinMax(100f);
		centerLocation = new MinMax(0f);
		amount = new MinMax(1f);
	}

	public static void Init()
	{
		tagSeeds.Clear();
		simplex = new Simplex();
	}

	public void Fill(float[,] grid, float amount_factor, bool keep_seed = false, float? contrast_override = null, float? strength_override = null, string tag_override = null)
	{
		if (type == DistributionType.TEMPLATE)
		{
			DistributionTemplate distributionTemplate = null;
			foreach (DistributionTemplate distributionTemplate2 in GlobalValues.standard.distributionTemplates)
			{
				if (distributionTemplate2.name == templateName)
				{
					distributionTemplate = distributionTemplate2;
				}
			}
			if (distributionTemplate != null)
			{
				distributionTemplate.distribution.Fill(grid, amount_factor, keep_seed, contrast, strength, tag);
			}
			else
			{
				Debug.LogWarning("Distribution.Fill: Template error (" + (string.IsNullOrEmpty(templateName) ? "empty" : templateName) + ")");
			}
			return;
		}
		float num = (contrast_override.HasValue ? contrast_override.Value : contrast);
		float num2 = (strength_override.HasValue ? strength_override.Value : strength);
		string text = ((tag_override != null) ? tag_override : tag);
		int length = grid.GetLength(0);
		int length2 = grid.GetLength(1);
		float num3 = 100f;
		float num4 = Mathf.Pow(10f, 0.5f + perlinSize * 1.5f);
		num4 = 100f / (num4 * num3);
		if (useSimplex)
		{
			num4 *= 0.65f;
		}
		if (!keep_seed)
		{
			if (string.IsNullOrEmpty(text))
			{
				seed = UnityEngine.Random.Range(1, int.MaxValue);
			}
			else if (!tagSeeds.TryGetValue(text, out seed))
			{
				seed = UnityEngine.Random.Range(1, int.MaxValue);
				tagSeeds[text] = seed;
			}
		}
		UnityEngine.Random.InitState(seed);
		float num5 = num * 0.5f;
		float num6 = 1f - num5;
		float num7 = 1f - num;
		switch (type)
		{
		default:
			_ = 50;
			break;
		case DistributionType.FILL:
		{
			for (int num27 = 0; num27 < length; num27++)
			{
				for (int num28 = 0; num28 < length2; num28++)
				{
					grid[num27, num28] = num2;
				}
			}
			break;
		}
		case DistributionType.PERLIN:
		{
			float num17 = UnityEngine.Random.value * 10000f;
			float num18 = UnityEngine.Random.value * 10000f;
			for (int n = 0; n < length; n++)
			{
				for (int num19 = 0; num19 < length2; num19++)
				{
					float num20;
					if (nOctaves == 1)
					{
						num20 = ((!useSimplex) ? Mathf.PerlinNoise(num17 + (float)n * num4, num18 + (float)num19 * num4) : simplex.Get(num17 + (float)n * num4, num18 + (float)num19 * num4));
					}
					else
					{
						float num21 = num4;
						float num22 = num17;
						float num23 = num18;
						float num24 = 1f;
						float num25 = 0f;
						num20 = 0f;
						for (int num26 = 0; num26 < nOctaves; num26++)
						{
							num20 = ((!useSimplex) ? (num20 + num24 * Mathf.PerlinNoise(num22 + (float)n * num21, num23 + (float)num19 * num21)) : (num20 + num24 * simplex.Get(num22 + (float)n * num21, num23 + (float)num19 * num21)));
							num21 *= lacunarity;
							num25 += num24;
							num24 *= persistence;
							num22 += 10f;
						}
						num20 /= num25;
					}
					if (flipNegative && num20 < 0.5f)
					{
						num20 = 1f - num20;
					}
					num20 = (num20 - lowerThreshold) / (upperThreshold - lowerThreshold);
					if (inverse)
					{
						num20 = 1f - num20;
					}
					num20 = ((!(num20 <= num5)) ? ((!(num20 >= num6)) ? ((num20 - num5) / num7) : 1f) : 0f);
					grid[n, num19] = Mathf.Clamp01(num20 * num2);
				}
			}
			break;
		}
		case DistributionType.CIRCLE:
		{
			float f2 = UnityEngine.Random.value * MathF.PI * 2f;
			float num29 = centerLocation.Lerp(UnityEngine.Random.value);
			float num30 = innerRadius.Lerp(UnityEngine.Random.value) / 8f;
			float num31 = outerRadius.Lerp(UnityEngine.Random.value) / 8f;
			if (num31 < num30)
			{
				num30 = num31 - 1f;
			}
			float num32 = num31 - num30;
			float num33 = num30 + num32 * 0.5f;
			Vector2 vector2 = new Vector2(0.5f + Mathf.Sin(f2) * num29 * 0.5f, 0.5f + Mathf.Cos(f2) * num29 * 0.5f) * num3;
			for (int num34 = 0; num34 < length; num34++)
			{
				for (int num35 = 0; num35 < length2; num35++)
				{
					float magnitude2 = (new Vector2(num34, num35) - vector2).magnitude;
					float num36 = ((num30 != 0f || !(magnitude2 < num33)) ? (1f - Mathf.Clamp01(Mathf.Abs(magnitude2 - num33) / num32)) : 1f);
					if (inverse)
					{
						num36 = 1f - num36;
					}
					num36 = ((!(num36 <= num5)) ? ((!(num36 >= num6)) ? ((num36 - num5) / num7) : 1f) : 0f);
					grid[num34, num35] = Mathf.Clamp01(num36 * num2);
				}
			}
			break;
		}
		case DistributionType.NEAR_ELEMENT:
		{
			Biome generatingBiome = Biome.generatingBiome;
			Ground generatingGround = Biome.generatingGround;
			List<Vector2Int> list = null;
			foreach (BiomeArea area in generatingBiome.areas)
			{
				foreach (BiomeElement element in area.elements)
				{
					if (element.element == nearElement)
					{
						if (list == null)
						{
							list = new List<Vector2Int>();
						}
						list.AddRange(element.spawned);
					}
				}
			}
			if (generatingBiome.preplacedSpawnedBobs != null)
			{
				string text2 = nearElement.ToString();
				foreach (BiomeObject preplacedSpawnedBob in generatingBiome.preplacedSpawnedBobs)
				{
					if (preplacedSpawnedBob.data.code == text2)
					{
						if (list == null)
						{
							list = new List<Vector2Int>();
						}
						Vector3 vector = preplacedSpawnedBob.transform.position - generatingGround.rectCorner;
						int x = Mathf.RoundToInt(Vector3.Project(vector, generatingGround.rectDir1).magnitude / 8f);
						int y = Mathf.RoundToInt(Vector3.Project(vector, generatingGround.rectDir2).magnitude / 8f);
						list.Add(new Vector2Int(x, y));
					}
				}
			}
			if (list == null)
			{
				Debug.LogWarning($"NEAR_ELEMENT ({nearElement}): no such element found in biome");
				break;
			}
			int num8 = Mathf.RoundToInt(amount.Lerp(UnityEngine.Random.value) * amount_factor);
			if (num8 == 0 && amount.min > 0f)
			{
				num8 = 1;
			}
			if (num8 > list.Count)
			{
				num8 = Mathf.RoundToInt(amount.min * amount_factor);
				if (num8 > list.Count)
				{
					Debug.LogWarning($"NEAR_ELEMENT ({nearElement}): want at least {num8} but only have {list.Count}");
				}
				num8 = list.Count;
			}
			for (int i = 0; i < length; i++)
			{
				for (int j = 0; j < length2; j++)
				{
					grid[i, j] = 0f;
				}
			}
			for (int k = 0; k < num8; k++)
			{
				Vector2Int vector2Int = list[list.Count - 1 - k];
				float num9 = outerRadius.Lerp(UnityEngine.Random.value) / 8f;
				float f = UnityEngine.Random.value * MathF.PI * 2f;
				float num10 = UnityEngine.Random.value * num9;
				vector2Int += new Vector2Int(Mathf.RoundToInt((0.5f + Mathf.Sin(f) * 0.5f) * num10), Mathf.RoundToInt((0.5f + Mathf.Cos(f) * 0.5f) * num10));
				int num11 = Mathf.RoundToInt(num9);
				int num12 = Mathf.Max(vector2Int.x - num11, 0);
				int num13 = Mathf.Min(vector2Int.x + num11, length - 1);
				int num14 = Mathf.Max(vector2Int.y - num11, 0);
				int num15 = Mathf.Min(vector2Int.y + num11, length2 - 1);
				for (int l = num12; l <= num13; l++)
				{
					for (int m = num14; m <= num15; m++)
					{
						float magnitude = (new Vector2(l, m) - vector2Int).magnitude;
						float num16 = 1f - Mathf.Clamp01(magnitude / num9);
						if (inverse)
						{
							num16 = 1f - num16;
						}
						num16 = ((!(num16 <= num5)) ? ((!(num16 >= num6)) ? ((num16 - num5) / num7) : 1f) : 0f);
						grid[l, m] = Mathf.Clamp01(grid[l, m] + num16 * num2);
					}
				}
			}
			break;
		}
		}
	}

	public float Checksum()
	{
		return perlinSize + lowerThreshold + upperThreshold + strength + contrast + flipNegative.Checksum() + inverse.Checksum() + (float)type + tag.Checksum() + (useSimplex ? 1f : 0f) + (float)nOctaves + lacunarity + persistence + innerRadius.Checksum() + outerRadius.Checksum() + templateName.Checksum() + (float)nearElement + amount.Checksum();
	}
}
