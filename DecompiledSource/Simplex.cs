using System.Collections.Generic;
using UnityEngine;

public class Simplex
{
	private static Vector2[] grad2 = new Vector2[12]
	{
		new Vector2(1f, 1f),
		new Vector2(-1f, 1f),
		new Vector2(1f, -1f),
		new Vector2(-1f, -1f),
		new Vector2(1f, 0f),
		new Vector2(-1f, 0f),
		new Vector2(1f, 0f),
		new Vector2(-1f, 0f),
		new Vector2(0f, 1f),
		new Vector2(0f, -1f),
		new Vector2(0f, 1f),
		new Vector2(0f, -1f)
	};

	private static short[] p = new short[256];

	private short[] perm = new short[512];

	private short[] permMod12 = new short[512];

	private static float F2 = 0.5f * (Mathf.Sqrt(3f) - 1f);

	private static float G2 = (3f - Mathf.Sqrt(3f)) / 6f;

	private static float Dot(Vector2 g, float x, float y)
	{
		return g.x * x + g.y * y;
	}

	public Simplex()
	{
		List<short> list = new List<short>();
		for (short num = 0; num < 256; num++)
		{
			list.Add(num);
		}
		for (int i = 0; i < 256; i++)
		{
			int index = Random.Range(0, list.Count);
			p[i] = list[index];
			list.RemoveAt(index);
		}
		for (int j = 0; j < 512; j++)
		{
			perm[j] = p[j & 0xFF];
			permMod12[j] = (short)(perm[j] % 12);
		}
	}

	public float Get(float xin, float yin)
	{
		float num = (xin + yin) * F2;
		int num2 = FloorToInt(xin + num);
		int num3 = FloorToInt(yin + num);
		float num4 = (float)(num2 + num3) * G2;
		float num5 = (float)num2 - num4;
		float num6 = (float)num3 - num4;
		float num7 = xin - num5;
		float num8 = yin - num6;
		int num9;
		int num10;
		if (num7 > num8)
		{
			num9 = 1;
			num10 = 0;
		}
		else
		{
			num9 = 0;
			num10 = 1;
		}
		float num11 = num7 - (float)num9 + G2;
		float num12 = num8 - (float)num10 + G2;
		float num13 = num7 - 1f + 2f * G2;
		float num14 = num8 - 1f + 2f * G2;
		int num15 = num2 & 0xFF;
		int num16 = num3 & 0xFF;
		int num17 = permMod12[num15 + perm[num16]];
		int num18 = permMod12[num15 + num9 + perm[num16 + num10]];
		int num19 = permMod12[num15 + 1 + perm[num16 + 1]];
		float num20 = 0.5f - num7 * num7 - num8 * num8;
		float num21;
		if (num20 < 0f)
		{
			num21 = 0f;
		}
		else
		{
			num20 *= num20;
			num21 = num20 * num20 * Dot(grad2[num17], num7, num8);
		}
		float num22 = 0.5f - num11 * num11 - num12 * num12;
		float num23;
		if (num22 < 0f)
		{
			num23 = 0f;
		}
		else
		{
			num22 *= num22;
			num23 = num22 * num22 * Dot(grad2[num18], num11, num12);
		}
		float num24 = 0.5f - num13 * num13 - num14 * num14;
		float num25;
		if (num24 < 0f)
		{
			num25 = 0f;
		}
		else
		{
			num24 *= num24;
			num25 = num24 * num24 * Dot(grad2[num19], num13, num14);
		}
		return 0.5f + 35f * (num21 + num23 + num25);
	}

	public static int FloorToInt(float x)
	{
		int num = (int)x;
		if (!(x < (float)num))
		{
			return num;
		}
		return num - 1;
	}
}
