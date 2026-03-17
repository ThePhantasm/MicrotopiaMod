using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace J4F;

public static class GameObjectExtensions
{
	public static void ClearChildren(Transform transform)
	{
		List<GameObject> list = new List<GameObject>();
		foreach (Transform item in transform)
		{
			list.Add(item.gameObject);
		}
		list.ForEach(delegate(GameObject child)
		{
			UnityEngine.Object.Destroy(child);
		});
	}

	public static T[] GetInterfaces<T>(this GameObject gObj)
	{
		if (!typeof(T).IsInterface)
		{
			throw new SystemException("Specified type is not an interface!");
		}
		return (from a in gObj.GetComponents<MonoBehaviour>()
			where a.GetType().GetInterfaces().Any((Type k) => k == typeof(T))
			select (T)(object)a).ToArray();
	}

	public static T GetInterface<T>(this GameObject gObj)
	{
		if (!typeof(T).IsInterface)
		{
			throw new SystemException("Specified type is not an interface!");
		}
		return gObj.GetInterfaces<T>().FirstOrDefault();
	}

	public static T GetInterfaceInChildren<T>(this GameObject gObj)
	{
		if (!typeof(T).IsInterface)
		{
			throw new SystemException("Specified type is not an interface!");
		}
		return gObj.GetInterfacesInChildren<T>().FirstOrDefault();
	}

	public static T[] GetInterfacesInChildren<T>(this GameObject gObj)
	{
		if (!typeof(T).IsInterface)
		{
			throw new SystemException("Specified type is not an interface!");
		}
		return (from a in gObj.GetComponentsInChildren<MonoBehaviour>()
			where a.GetType().GetInterfaces().Any((Type k) => k == typeof(T))
			select (T)(object)a).ToArray();
	}

	public static Bounds GetGlobalBounds(this GameObject go)
	{
		Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>();
		Bounds result = default(Bounds);
		Renderer[] array = componentsInChildren;
		foreach (Renderer renderer in array)
		{
			result.Encapsulate(renderer.bounds);
		}
		return result;
	}
}
