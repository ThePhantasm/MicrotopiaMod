using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Toolkit
{
	public static RaycastHit[] raycastHits = new RaycastHit[400];

	public static Collider[] overlapColliders = new Collider[400];

	private static long lastTick;

	public static Vector3 ZeroPosition(Vector3 pos)
	{
		return new Vector3(pos.x, 0f, pos.z);
	}

	public static Vector3 GroundPosition(Transform transform, Vector3 pos)
	{
		return new Vector3(pos.x, transform.position.y, pos.z);
	}

	public static Vector3 TargetHeightPosition(float y, Vector3 pos)
	{
		return new Vector3(pos.x, y, pos.z);
	}

	public static Vector3 RandomInCirclePosition(Vector3 pos, float circleSize)
	{
		Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
		pos.x += insideUnitCircle.x * circleSize;
		pos.z += insideUnitCircle.y * circleSize;
		return pos;
	}

	public static Vector3 RandomInCirclePosition(float circleSize)
	{
		return RandomInCirclePosition(Vector3.zero, circleSize);
	}

	public static Vector3 GetRandomInDonut(float min, float max)
	{
		float f = UnityEngine.Random.Range(0f, MathF.PI * 2f);
		float num = UnityEngine.Random.Range(min, max);
		return new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
	}

	public static Vector3 LookVector(Vector3 origin, Vector3 target)
	{
		return target - origin;
	}

	public static Vector3 LookVectorNormalized(Vector3 origin, Vector3 target)
	{
		return (target - origin).normalized;
	}

	public static float RandomRangeAveraged(float min, float max, float average)
	{
		return UnityEngine.Random.Range(UnityEngine.Random.Range(min, average), UnityEngine.Random.Range(average, max));
	}

	public static Vector3 GetAveragePosition(List<GameObject> list)
	{
		Vector3 zero = Vector3.zero;
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			zero += list[i].transform.position;
			num++;
		}
		return zero / num;
	}

	public static Vector3 GetRenderersCenter(Renderer[] rends)
	{
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < rends.Length; i++)
		{
			zero += rends[i].bounds.center;
		}
		return zero / rends.Length;
	}

	public static bool CoinFlip()
	{
		return Mathf.Round(UnityEngine.Random.Range(0f, 1f)) == 1f;
	}

	public static bool DiceRoll(int sides)
	{
		if (sides == 0)
		{
			return false;
		}
		return UnityEngine.Random.Range(0, sides) == 0;
	}

	public static bool CheckEnum(string parse, Type T, string log = "")
	{
		if (Enum.TryParse(T, parse, out var _))
		{
			return true;
		}
		Debug.LogWarning(((log != "") ? (log + ": ") : "") + parse + " not recognized as enum " + T.ToString());
		return false;
	}

	public static IEnumerator CJumpTo(Transform ob, Vector3 target_pos, Quaternion target_rot, float duration, AnimationCurve y_curve = null, float y_curve_height = 0f, Action on_finnish = null)
	{
		Vector3 start_pos = ob.position;
		Quaternion start_rot = ob.rotation;
		for (float t = 0f; t < duration; t += Time.deltaTime)
		{
			Vector3 position = Vector3.Lerp(start_pos, target_pos, t / duration);
			if (y_curve != null)
			{
				position.y = Mathf.Lerp(start_pos.y, target_pos.y, t / duration) + y_curve_height * y_curve.Evaluate(t / duration);
			}
			ob.SetPositionAndRotation(position, Quaternion.Lerp(start_rot, target_rot, t / duration));
			yield return null;
		}
		ob.SetPositionAndRotation(target_pos, target_rot);
		on_finnish?.Invoke();
	}

	public static IEnumerator CJumpTo(Transform ob, Transform target, Quaternion target_rot, float duration, AnimationCurve y_curve = null, float y_curve_height = 0f, Action on_finnish = null)
	{
		Vector3 start_pos = ob.position;
		Quaternion start_rot = ob.rotation;
		for (float t = 0f; t < duration; t += Time.deltaTime)
		{
			Vector3 position = Vector3.Lerp(start_pos, target.position, t / duration);
			if (y_curve != null)
			{
				position.y = Mathf.Lerp(start_pos.y, target.position.y, t / duration) + y_curve_height * y_curve.Evaluate(t / duration);
			}
			ob.SetPositionAndRotation(position, Quaternion.Lerp(start_rot, target_rot, t / duration));
			yield return null;
		}
		ob.SetPositionAndRotation(target.position, target_rot);
		on_finnish?.Invoke();
	}

	public static bool GetGroundPos(Vector3 pos, out Vector3 ground_pos)
	{
		if (Physics.Raycast(new Vector3(pos.x, 100f, pos.z), Vector3.down, out var hitInfo, 110f, Mask(Layers.Ground)) && hitInfo.normal.y > 0.99f)
		{
			ground_pos = hitInfo.point;
			return true;
		}
		ground_pos = Vector3.zero;
		return false;
	}

	public static bool IsOnGround(Vector3 pos)
	{
		if (!Physics.Raycast(new Vector3(pos.x, 100f, pos.z), Vector3.down, out var hitInfo, 110f, Mask(Layers.Ground)))
		{
			return false;
		}
		return hitInfo.normal.y > 0.99f;
	}

	public static bool IsOverEdge(Vector3 pos, float radius, int n_checks = 8)
	{
		for (int i = 0; i < n_checks; i++)
		{
			float f = (float)i * (MathF.PI * 2f / (float)n_checks);
			if (GetGround(pos + new Vector3(Mathf.Sin(f) * radius, 0f, Mathf.Cos(f) * radius)) == null)
			{
				return true;
			}
		}
		return false;
	}

	public static int CheckFreeGroundPos(ref Vector3 pos)
	{
		int result;
		if (!Physics.Raycast(new Vector3(pos.x, 500f, pos.z), Vector3.down, out var hitInfo, 510f, Mask(Layers.Ground, Layers.Sources, Layers.Buildings, Layers.BuildingElement)))
		{
			result = 1;
		}
		else if (hitInfo.transform.gameObject.layer != 14)
		{
			result = 2;
		}
		else if ((double)hitInfo.normal.y < 0.99)
		{
			result = 3;
		}
		else
		{
			result = 0;
			pos = hitInfo.point;
		}
		return result;
	}

	public static Ground GetGround(Vector3 pos)
	{
		if (!Physics.Raycast(new Vector3(pos.x, 100f, pos.z), Vector3.down, out var hitInfo, 110f, Mask(Layers.Ground)))
		{
			return null;
		}
		return hitInfo.transform.GetComponentInParent<Ground>();
	}

	public static IEnumerable<Trail> EFindTrailsNear(Vector3 pos, float range)
	{
		int found = Physics.OverlapSphereNonAlloc(pos, range, overlapColliders, Mask(Layers.Trails));
		for (int i = 0; i < found; i++)
		{
			Trail componentInParent = overlapColliders[i].GetComponentInParent<Trail>();
			if (componentInParent != null && componentInParent.trailType != TrailType.COMMAND)
			{
				yield return componentInParent;
			}
		}
	}

	public static int Mask(Layers l)
	{
		return 1 << (int)l;
	}

	public static int Mask(params Layers[] l)
	{
		int num = 0;
		for (int i = 0; i < l.Length; i++)
		{
			num |= 1 << (int)l[i];
		}
		return num;
	}

	public static void DebugDrawCircle(Vector2 pos, float radius, Color color, float duration = 0f, bool depth_test = false)
	{
		DebugDrawCircle(pos.To3D(), radius, color, duration, depth_test);
	}

	public static void DebugDrawCircle(Vector3 pos, float radius, Color color, float duration = 0f, bool depth_test = false)
	{
		int num = 32;
		float num2 = 1f / (float)num;
		Vector3 start = pos + new Vector3(Mathf.Sin(0f), 0f, Mathf.Cos(0f)) * radius;
		for (float num3 = 0f; num3 < 1f; num3 += num2)
		{
			float f = MathF.PI * 2f * (num3 + num2);
			Vector3 vector = pos + new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * radius;
			Debug.DrawLine(start, vector, color, duration, depth_test);
			start = vector;
		}
	}

	public static void DebugDrawPoint(Vector3 pos, float radius, Color color, float duration = 0f, bool depth_test = false)
	{
		Debug.DrawLine(pos - Vector3.up * radius, pos + Vector3.up * radius, color, duration, depth_test);
		Debug.DrawLine(pos - Vector3.right * radius, pos + Vector3.right * radius, color, duration, depth_test);
		Debug.DrawLine(pos - Vector3.forward * radius, pos + Vector3.forward * radius, color, duration, depth_test);
	}

	public static void DebugDrawSketch(Vector2 p1, Vector2 p2, Color col)
	{
		Debug.DrawLine((p1 + new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)).To3D(), (p2 + new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)).To3D(), col, 1000f, depthTest: false);
	}

	public static void SetRandomSeed(int seed, int seed_index)
	{
		UnityEngine.Random.InitState(seed ^ (seed_index * 131));
	}

	public static Quaternion RandomYRotation()
	{
		return Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
	}

	public static float Easify(float f, Ease ease)
	{
		switch (ease)
		{
		case Ease.In:
			if (f < 0.5f)
			{
				return f * f * (3f - 2f * f);
			}
			return f;
		case Ease.Out:
			if (f > 0.5f)
			{
				return f * f * (3f - 2f * f);
			}
			return f;
		case Ease.InOut:
			return f * f * (3f - 2f * f);
		default:
			return f;
		}
	}

	public static void LogDur(string context = null)
	{
		if (context != null)
		{
			Debug.Log($"DUR {context}: {(float)(DateTime.Now.Ticks - lastTick) / 10000000f: 0.00} s");
		}
		lastTick = DateTime.Now.Ticks;
	}

	public static Vector3 GetNearestPosOnLinePiece(Vector3 p1, Vector3 p2, Vector3 pos)
	{
		if (p1 == p2)
		{
			return p1;
		}
		Vector3 vector = p2 - p1;
		float value = Vector3.Dot(pos - p1, vector) / vector.sqrMagnitude;
		return p1 + Mathf.Clamp01(value) * vector;
	}

	public static void ResetRandomState()
	{
		UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
	}

	public static int SaveVersion()
	{
		return 94;
	}

	public static void SetHotkeyButton(GameObject ob, TMP_Text lb, string str)
	{
		bool flag = string.IsNullOrEmpty(str);
		ob.SetObActive(!flag);
		if (!flag)
		{
			lb.text = str;
			RectTransform component = ob.GetComponent<RectTransform>();
			Vector2 sizeDelta = component.sizeDelta;
			sizeDelta.x = ((str.Length > 1) ? (sizeDelta.y * 1.5f) : sizeDelta.y);
			component.sizeDelta = sizeDelta;
		}
	}

	public static void AdjustContrastBrightness(ref Texture2D tex, float contrast, int brightness)
	{
		Color32[] pixels = tex.GetPixels32();
		for (int i = 0; i < pixels.Length; i++)
		{
			Color32 color = pixels[i];
			color.r = (byte)Mathf.Clamp(Mathf.RoundToInt((float)(color.r - 128) * contrast + 128f + (float)brightness), 0, 255);
			color.g = (byte)Mathf.Clamp(Mathf.RoundToInt((float)(color.g - 128) * contrast + 128f + (float)brightness), 0, 255);
			color.b = (byte)Mathf.Clamp(Mathf.RoundToInt((float)(color.b - 128) * contrast + 128f + (float)brightness), 0, 255);
			pixels[i] = color;
		}
		tex.SetPixels32(pixels);
		tex.Apply();
	}
}
