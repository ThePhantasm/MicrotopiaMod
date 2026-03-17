using System;
using UnityEngine;

[Serializable]
public struct MinMax
{
	public float min;

	public float max;

	public MinMax(float _v)
	{
		min = (max = _v);
	}

	public MinMax(float _min, float _max)
	{
		min = _min;
		max = _max;
	}

	public MinMax Include(float _v)
	{
		if (_v < min)
		{
			min = _v;
		}
		if (_v > max)
		{
			max = _v;
		}
		return this;
	}

	public float GetLength()
	{
		return max - min;
	}

	public float GetRandom()
	{
		return min + (max - min) * UnityEngine.Random.value;
	}

	public float Lerp(float f)
	{
		return min + (max - min) * f;
	}

	public float Checksum()
	{
		return min + max;
	}

	public static MinMax operator *(MinMax mm, float f)
	{
		return new MinMax(mm.min * f, mm.max * f);
	}

	public static MinMax operator /(MinMax mm, float f)
	{
		return new MinMax(mm.min / f, mm.max / f);
	}

	public override string ToString()
	{
		return $"{min} - {max}";
	}
}
