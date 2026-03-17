using UnityEngine;

namespace HighlightPlus;

public static class InputProxy
{
	public static Vector3 mousePosition => Input.mousePosition;

	public static int touchCount => Input.touchCount;

	public static void Init()
	{
	}

	public static bool GetMouseButtonDown(int buttonIndex)
	{
		return Input.GetMouseButtonDown(buttonIndex);
	}

	public static int GetFingerIdFromTouch(int touchIndex)
	{
		return Input.GetTouch(touchIndex).fingerId;
	}

	public static bool GetKeyDown(string name)
	{
		return Input.GetKeyDown(name);
	}
}
