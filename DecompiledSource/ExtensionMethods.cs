using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ExtensionMethods
{
	private static NumberFormatInfo numberFormatDots = new NumberFormatInfo
	{
		NumberDecimalSeparator = "."
	};

	public static void SetVisible(this Button button, bool visible)
	{
		button.SetObActive(visible);
	}

	public static bool GetVisible(this Button button)
	{
		return button.gameObject.activeInHierarchy;
	}

	public static Vector2 ToVector2(this Vector3 v)
	{
		return v;
	}

	public static Vector3 ZeroPosition(this Vector3 v)
	{
		return new Vector3(v.x, 0f, v.z);
	}

	public static Vector3 ZeroPosition(this Transform t)
	{
		return new Vector3(t.position.x, 0f, t.position.z);
	}

	public static Vector3 FloorPosition(this Vector3 other, Vector3 self)
	{
		return new Vector3(other.x, self.y, other.z);
	}

	public static Vector3 TargetYPosition(this Vector3 v, float y)
	{
		return new Vector3(v.x, y, v.z);
	}

	public static Vector3 TransformYPosition(this Vector3 v, Transform trans)
	{
		return new Vector3(v.x, trans.position.y, v.z);
	}

	public static Vector2 XY(this Vector3 v)
	{
		return v;
	}

	public static Vector2 XZ(this Vector3 v)
	{
		return new Vector2(v.x, v.z);
	}

	public static Vector3 To3D(this Vector2 v)
	{
		return new Vector3(v.x, 0f, v.y);
	}

	public static Vector3 To3D(this Vector2 v, float y)
	{
		return new Vector3(v.x, y, v.y);
	}

	public static Vector3 SetX(this Vector3 v, float x)
	{
		v.x = x;
		return v;
	}

	public static Vector3 SetY(this Vector3 v, float y)
	{
		v.y = y;
		return v;
	}

	public static Vector3 SetZ(this Vector3 v, float z)
	{
		v.z = z;
		return v;
	}

	public static Vector3 SetXY(this Vector3 v, Vector2 v2)
	{
		v.x = v2.x;
		v.y = v2.y;
		return v;
	}

	public static Vector2 SetX(this Vector2 v, float x)
	{
		v.x = x;
		return v;
	}

	public static Vector2 SetY(this Vector2 v, float y)
	{
		v.y = y;
		return v;
	}

	public static void SetColor(this LineRenderer lr, Color col)
	{
		Color startColor = (lr.endColor = col);
		lr.startColor = startColor;
	}

	public static string Unit(this float f, PhysUnit pu)
	{
		switch (pu)
		{
		case PhysUnit.MASS:
			f /= 50f;
			if (f < 1f)
			{
				return Loc.GetUI("PHYSUNIT_G", (f * 1000f).ToString("0"));
			}
			return Loc.GetUI("PHYSUNIT_KG", f.ToString("0.0"));
		case PhysUnit.SIZE:
			f /= 2.5f;
			if (f < 1f)
			{
				return Loc.GetUI("PHYSUNIT_CM", (f * 100f).ToString("0"));
			}
			return Loc.GetUI("PHYSUNIT_M", f.ToString("0.0"));
		case PhysUnit.ALTITUDE:
			if (f >= 100000f)
			{
				return Loc.GetUI("PHYSUNIT_KM", (f / 1000f).ToString("0"));
			}
			if (f >= 10000f)
			{
				return Loc.GetUI("PHYSUNIT_KM", (f / 1000f).ToString("0.0"));
			}
			if (f >= 1000f)
			{
				return Loc.GetUI("PHYSUNIT_KM", (f / 1000f).ToString("0.00"));
			}
			return Loc.GetUI("PHYSUNIT_M", f.ToString("0"));
		case PhysUnit.ALTITUDE_PRECISE:
			return Loc.GetUI("PHYSUNIT_M", f.ToString("0"));
		case PhysUnit.TIME:
			return Loc.GetUI("PHYSUNIT_SEC", f.ToString("0"));
		case PhysUnit.TIME_MINUTES:
			return Mathf.Floor(f / 60f).ToString("0") + ":" + Mathf.Floor(f % 60f).ToString("00");
		case PhysUnit.CAP_FUEL:
			f /= 4f;
			if (f < 1f)
			{
				return (f * 1000f).ToString("0") + " ml";
			}
			if (f < 10f)
			{
				return f.ToString("0.0") + " l";
			}
			return f.ToString("0") + " l";
		case PhysUnit.CAP_OXYGEN:
			f /= 10f;
			return f.ToString("0") + " m3";
		case PhysUnit.RATE_FUEL:
			return f.ToString("0") + " l/s";
		case PhysUnit.RATE_OXYGEN:
			return f.ToString("0") + " l/s";
		case PhysUnit.ENGINE_POWER:
			return f.ToString("0.0");
		case PhysUnit.SPEED:
			return (f * 3.6f).ToString("0") + " km/h";
		default:
			return f.ToString("0.0");
		}
	}

	public static string Unit(this int i, PhysUnit pu)
	{
		return ((float)i).Unit(pu);
	}

	public static int ToInt(this string str, int def, string error = null)
	{
		try
		{
			return Convert.ToInt32(str);
		}
		catch
		{
			if (error != null)
			{
				Debug.LogError(error + " - Couldn't convert '" + str + "' to int");
			}
			return def;
		}
	}

	public static float ToFloat(this string str, float def, string error = null)
	{
		try
		{
			str = str.Replace(',', '.');
			return Convert.ToSingle(str, numberFormatDots);
		}
		catch
		{
			if (error != null)
			{
				Debug.LogError(error + " - Couldn't convert '" + str + "' to float");
			}
			return def;
		}
	}

	public static string ToText(this string str)
	{
		return str.Replace('|', '\n');
	}

	public static bool SetObActive(this Component c, bool active)
	{
		if (c.gameObject.activeSelf != active)
		{
			c.gameObject.SetActive(active);
			return true;
		}
		return false;
	}

	public static bool SetObActive(this GameObject ob, bool active)
	{
		if (ob.activeSelf != active)
		{
			ob.SetActive(active);
			return true;
		}
		return false;
	}

	public static void SetText(this Button bt, string text)
	{
		bt.GetComponentInChildren<Text>().text = text;
	}

	public static void Shuffle<T>(this IList<T> list)
	{
		for (int num = list.Count - 1; num > 0; num--)
		{
			int num2 = UnityEngine.Random.Range(0, num);
			int index = num2;
			int index2 = num;
			T val = list[num];
			T val2 = list[num2];
			T val3 = (list[index] = val);
			val3 = (list[index2] = val2);
		}
	}

	public static bool CheckIfOwnSplit(this Trail trail, Split split)
	{
		bool result = false;
		if ((bool)trail)
		{
			if ((bool)trail.splitStart && trail.splitStart == split)
			{
				result = true;
			}
			if ((bool)trail.splitEnd && trail.splitEnd == split)
			{
				result = true;
			}
		}
		return result;
	}

	public static IEnumerable<string> EListItems(this string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			yield break;
		}
		string[] strs = str.Split(',');
		for (int i = 0; i < strs.Length; i++)
		{
			string text = strs[i].Trim();
			if (!string.IsNullOrEmpty(text))
			{
				yield return text;
			}
		}
	}

	public static Quaternion RandomYRotation(this Quaternion quat)
	{
		return Quaternion.Euler(quat.eulerAngles.x, UnityEngine.Random.Range(0f, 360f), quat.eulerAngles.y);
	}

	public static Quaternion ToLocalRotation(this Quaternion rot_world, Quaternion parent_rot_world)
	{
		return Quaternion.Inverse(parent_rot_world) * rot_world;
	}

	public static T GetCopyOf<T>(this T comp, T other) where T : Component
	{
		Type type = comp.GetType();
		Type type2 = other.GetType();
		if (type != type2)
		{
			Debug.LogError($"The type \"{type.AssemblyQualifiedName}\" of \"{comp}\" does not match the type \"{type2.AssemblyQualifiedName}\" of \"{other}\"!");
			return null;
		}
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		PropertyInfo[] properties = type.GetProperties(bindingAttr);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.CanWrite && propertyInfo.Name != "name" && propertyInfo.Name != "tag" && propertyInfo.Name != "enabled")
			{
				try
				{
					propertyInfo.SetValue(comp, propertyInfo.GetValue(other, null), null);
				}
				catch
				{
				}
			}
		}
		FieldInfo[] fields = type.GetFields(bindingAttr);
		foreach (FieldInfo fieldInfo in fields)
		{
			fieldInfo.SetValue(comp, fieldInfo.GetValue(other));
		}
		return comp;
	}

	public static Vector3 GetPointAtY(this Ray ray, float y)
	{
		Vector3 origin = ray.origin;
		Vector3 direction = ray.direction;
		if (Mathf.Abs(direction.y) < Mathf.Epsilon)
		{
			Debug.LogError("Ray.GetPointAtY: ray needs to go up/down");
			return Vector3.zero;
		}
		return origin + (y - origin.y) / direction.y * direction;
	}

	public static Color SetAlpha(this Color col, float alpha)
	{
		col.a = alpha;
		return col;
	}

	public static bool TriggersAtTime(this float time, float target_time, float dt)
	{
		if (time <= target_time)
		{
			return time + dt > target_time;
		}
		return false;
	}

	public static float Checksum(this bool v)
	{
		if (!v)
		{
			return 0f;
		}
		return 1f;
	}

	public static float Checksum(this string v)
	{
		int num = 0;
		for (int i = 0; i < v.Length; i++)
		{
			num ^= v[i];
			num <<= 1;
		}
		return num;
	}

	public static float Checksum(this Color col)
	{
		return col.r + col.g + col.b + col.a;
	}

	public static float Checksum(this Vector3 v)
	{
		return v.x + v.y + v.z;
	}

	public static void Remove<T>(this Queue<T> queue, T delete_el) where T : class
	{
		if (!queue.Contains(delete_el))
		{
			return;
		}
		List<T> list = new List<T>();
		T result;
		while (queue.TryDequeue(out result))
		{
			if (result != delete_el)
			{
				list.Add(result);
			}
		}
		queue.Clear();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			queue.Enqueue(list[num]);
		}
	}

	public static Dictionary<PickupType, int> ToDictionary(this List<PickupCost> _costs)
	{
		Dictionary<PickupType, int> dictionary = new Dictionary<PickupType, int>();
		foreach (PickupCost _cost in _costs)
		{
			if (!dictionary.ContainsKey(_cost.type))
			{
				dictionary.Add(_cost.type, 0);
			}
			dictionary[_cost.type] += _cost.intValue;
		}
		return dictionary;
	}

	public static string NamesString<T>(this List<T> list) where T : UnityEngine.Object
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (T item in list)
		{
			stringBuilder.Append((item == null) ? "NULL" : item.name).Append(", ");
		}
		string text = stringBuilder.ToString();
		if (text.Length > 0)
		{
			text = text[..^2];
		}
		return text;
	}

	public static string PropertiesString<T>(this IEnumerable<T> list, Func<T, string> get_string)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (T item in list)
		{
			stringBuilder.Append(get_string(item)).Append(", ");
		}
		string text = stringBuilder.ToString();
		if (text.Length > 0)
		{
			text = text[..^2];
		}
		return text;
	}

	public static string Name(this UnityEngine.Object ob)
	{
		if (ob == null)
		{
			return "NULL";
		}
		return ob.name;
	}

	public static Vector3 GetCenter<T>(this IEnumerable<T> list) where T : Component
	{
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		foreach (T item in list)
		{
			Vector3 position = item.transform.position;
			if (position.x < vector.x)
			{
				vector.x = position.x;
			}
			if (position.y < vector.y)
			{
				vector.y = position.y;
			}
			if (position.z < vector.z)
			{
				vector.z = position.z;
			}
			if (position.x > vector2.x)
			{
				vector2.x = position.x;
			}
			if (position.y > vector2.y)
			{
				vector2.y = position.y;
			}
			if (position.z > vector2.z)
			{
				vector2.z = position.z;
			}
		}
		return (vector + vector2) * 0.5f;
	}

	public static void Set(this TMP_Text lb, string txt)
	{
		if (txt == "")
		{
			lb.SetObActive(active: false);
			return;
		}
		lb.text = txt;
		lb.SetObActive(active: true);
	}

	public static bool IsBuildingTrail(this TrailType _type)
	{
		if (_type != TrailType.IN_BUILDING)
		{
			return _type == TrailType.IN_BUILDING_GATE;
		}
		return true;
	}

	public static Dictionary<T, int> AddDictionary<T>(this Dictionary<T, int> dic, Dictionary<T, int> other)
	{
		foreach (KeyValuePair<T, int> item in other)
		{
			if (!dic.ContainsKey(item.Key))
			{
				dic.Add(item.Key, 0);
			}
			dic[item.Key] += item.Value;
		}
		return dic;
	}

	public static List<T> ToList<T>(this Dictionary<T, int> dic)
	{
		List<T> list = new List<T>();
		foreach (KeyValuePair<T, int> item in dic)
		{
			list.Add(item.Key);
		}
		return list;
	}

	public static void SetLayerRecursive(this GameObject ob, int layer)
	{
		ob.layer = layer;
		foreach (Transform item in ob.transform)
		{
			item.gameObject.SetLayerRecursive(layer);
		}
	}

	public static float RoundAt(this float v, float precision)
	{
		return Mathf.Round(v / precision) * precision;
	}

	public static List<AntCaste> ToEnumList(this List<AntCasteAmount> amounts)
	{
		List<AntCaste> list = new List<AntCaste>();
		foreach (AntCasteAmount amount in amounts)
		{
			for (int i = 0; i < amount.intValue; i++)
			{
				list.Add(amount.type);
			}
		}
		return list;
	}

	public static int Count(this List<AntCasteAmount> amounts)
	{
		int num = 0;
		foreach (AntCasteAmount amount in amounts)
		{
			num += amount.intValue;
		}
		return num;
	}

	public static Dictionary<AntCaste, int> ToDictionary(this List<AntCasteAmount> amounts)
	{
		Dictionary<AntCaste, int> dictionary = new Dictionary<AntCaste, int>();
		foreach (AntCasteAmount amount in amounts)
		{
			dictionary.Add(amount.type, amount.intValue);
		}
		return dictionary;
	}

	public static string DebugName(this Transform tf)
	{
		if (tf == null || tf.gameObject == null)
		{
			return "NULL";
		}
		while (tf.parent != null)
		{
			tf = tf.parent;
		}
		string text = tf.gameObject.name;
		int num;
		do
		{
			num = text.IndexOf("(");
			if (num >= 0)
			{
				int num2 = text.IndexOf(")");
				text = (text[..num] + text[(num2 + 1)..]).Trim();
			}
		}
		while (num >= 0);
		return text + Mathf.Abs(tf.GetInstanceID()) % 1000;
	}

	public static string DebugName(this GameObject ob)
	{
		return ob.transform.DebugName();
	}

	public static void ForEachChild(this Transform tf, Action<Transform> action)
	{
		for (int num = tf.childCount - 1; num >= 0; num--)
		{
			action(tf.GetChild(num));
		}
	}

	public static void DestroyChildrenWithLayer(this Transform tf, Layers layer)
	{
		for (int num = tf.childCount - 1; num >= 0; num--)
		{
			if (tf.GetChild(num).gameObject.layer == (int)layer)
			{
				UnityEngine.Object.Destroy(tf.GetChild(num).gameObject);
			}
		}
	}

	public static void DestroyChildrenWithLayer(this GameObject ob, Layers layer)
	{
		ob.transform.DestroyChildrenWithLayer(layer);
	}

	public static bool ContainsMouse(this Component component)
	{
		return RectTransformUtility.RectangleContainsScreenPoint(component.GetComponent<RectTransform>(), Input.mousePosition);
	}

	public static T GetAny<T>(this HashSet<T> hashset) where T : UnityEngine.Object
	{
		using (HashSet<T>.Enumerator enumerator = hashset.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return null;
	}

	public static Dictionary<AntCaste, int> ToDictionary(this List<Ant> _ants)
	{
		Dictionary<AntCaste, int> dictionary = new Dictionary<AntCaste, int>();
		foreach (Ant _ant in _ants)
		{
			if (!(_ant == null))
			{
				if (!dictionary.TryGetValue(_ant.caste, out var value))
				{
					value = 0;
				}
				dictionary[_ant.caste] = value + 1;
			}
		}
		return dictionary;
	}

	public static void AddIfNew<T>(this List<T> list, T t)
	{
		if (!list.Contains(t))
		{
			list.Add(t);
		}
	}

	public static string ToBits(this int i)
	{
		return Convert.ToString(i, 2);
	}
}
