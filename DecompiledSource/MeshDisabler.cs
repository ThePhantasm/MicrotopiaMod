using UnityEngine;

[ExecuteInEditMode]
public class MeshDisabler : MonoBehaviour
{
	[Header("Disable/Enable all mesh renderers parented under this object")]
	public bool toggleEnabled;

	private void Update()
	{
		if (toggleEnabled)
		{
			toggleEnabled = false;
			DoToggle();
		}
	}

	private void DoToggle()
	{
		MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer obj in componentsInChildren)
		{
			obj.enabled = !obj.enabled;
		}
	}
}
