using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.EventSystems;

namespace J4F;

public class Tools
{
	public static BinaryFormatter bf = new BinaryFormatter();

	private static Dictionary<string, UnityEngine.Object> preloads;

	public static void BroadcastMessageToAll(string fun)
	{
		BroadcastMessageToAll(fun, null);
	}

	public static void BroadcastMessageToAll(string fun, object msg)
	{
		GameObject[] array = (GameObject[])UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
		foreach (GameObject gameObject in array)
		{
			if ((bool)gameObject && gameObject.transform.parent == null)
			{
				if (msg == null)
				{
					gameObject.BroadcastMessage(fun, SendMessageOptions.DontRequireReceiver);
				}
				else
				{
					gameObject.BroadcastMessage(fun, msg, SendMessageOptions.DontRequireReceiver);
				}
			}
		}
	}

	public static string FormatTime(float time)
	{
		return string.Concat(string.Concat("" + ((int)time / 60).ToString("00") + ":", ((int)time % 60).ToString("00"), "."), ((int)((time - Mathf.Floor(time)) * 1000f)).ToString("000"));
	}

	public static IEnumerator DeactivateGameObjectPhysic(GameObject obj)
	{
		yield return new WaitForEndOfFrame();
		obj.SetActive(value: false);
	}

	public static void Save(string prefKey, object serializableObject)
	{
		MemoryStream memoryStream = new MemoryStream();
		bf.Serialize(memoryStream, serializableObject);
		string value = Convert.ToBase64String(memoryStream.ToArray());
		PlayerPrefs.SetString(prefKey, value);
	}

	public static T Load<T>(string prefKey)
	{
		if (!PlayerPrefs.HasKey(prefKey))
		{
			return default(T);
		}
		MemoryStream serializationStream = new MemoryStream(Convert.FromBase64String(PlayerPrefs.GetString(prefKey)));
		return (T)bf.Deserialize(serializationStream);
	}

	public static void QualitySwitch(bool value)
	{
	}

	public static Rect BoundsToScreenRect(Bounds bounds)
	{
		Vector3 vector = Camera.main.WorldToViewportPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z));
		Vector3 vector2 = Camera.main.WorldToViewportPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.max.z));
		return new Rect(vector.x, (float)Screen.height - vector.y, vector2.x - vector.x, vector.y - vector2.y);
	}

	public static GameObject InstantiateFromResource(string resourcePath)
	{
		if (preloads == null)
		{
			preloads = new Dictionary<string, UnityEngine.Object>();
		}
		if (!preloads.ContainsKey(resourcePath))
		{
			preloads.Add(resourcePath, Resources.Load(resourcePath));
		}
		preloads.TryGetValue(resourcePath, out var value);
		return (GameObject)UnityEngine.Object.Instantiate(value);
	}

	public static bool IsUserTouchingUI()
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			return true;
		}
		return false;
	}
}
