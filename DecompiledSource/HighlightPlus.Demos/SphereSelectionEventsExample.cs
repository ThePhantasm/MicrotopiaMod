using UnityEngine;

namespace HighlightPlus.Demos;

public class SphereSelectionEventsExample : MonoBehaviour
{
	private void Start()
	{
		HighlightManager.instance.OnObjectSelected += OnObjectSelected;
		HighlightManager.instance.OnObjectUnSelected += OnObjectUnSelected;
	}

	private bool OnObjectSelected(GameObject go)
	{
		Debug.Log(go.name + " selected!");
		return true;
	}

	private bool OnObjectUnSelected(GameObject go)
	{
		Debug.Log(go.name + " un-selected!");
		return true;
	}
}
