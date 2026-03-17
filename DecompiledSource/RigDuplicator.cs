using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RigDuplicator : MonoBehaviour
{
	public bool copy;

	public bool paste;

	private List<Vector3> listPos = new List<Vector3>();

	private List<Quaternion> listRot = new List<Quaternion>();

	private List<Vector3> listScale = new List<Vector3>();

	private void Update()
	{
		if (copy)
		{
			copy = false;
			DoCopy();
		}
		if (paste)
		{
			paste = false;
			DoPaste();
		}
	}

	private void DoCopy()
	{
		listPos.Clear();
		listRot.Clear();
		listScale.Clear();
		Transform[] componentsInChildren = GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			listPos.Add(transform.position);
			listRot.Add(transform.rotation);
			listScale.Add(transform.localScale);
		}
		Debug.Log("Done copying children from " + base.name);
	}

	private void DoPaste()
	{
		if (listPos.Count == 0)
		{
			Debug.LogWarning("Tried to paste to " + base.name + " with no copy data");
			return;
		}
		Transform[] componentsInChildren = GetComponentsInChildren<Transform>();
		if (componentsInChildren.Length != listPos.Count)
		{
			Debug.LogWarning("Copied data does not match paste target " + base.name);
			return;
		}
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].position = listPos[i];
			componentsInChildren[i].rotation = listRot[i];
			componentsInChildren[i].localScale = listScale[i];
		}
		Debug.Log("Done pasting to " + base.name);
	}
}
