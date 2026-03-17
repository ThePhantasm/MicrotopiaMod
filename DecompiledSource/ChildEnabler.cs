using System.Collections.Generic;
using UnityEngine;

public class ChildEnabler : MonoBehaviour
{
	public Vector2Int amountToEnable = new Vector2Int(1, 1);

	[Space(10f)]
	public bool automaticMode = true;

	[Header("If automatic mode is disabled, enable from this list")]
	public List<Transform> children = new List<Transform>();

	private List<Transform> _children = new List<Transform>();

	private void Start()
	{
		if (automaticMode)
		{
			Transform[] componentsInChildren = GetComponentsInChildren<Transform>();
			foreach (Transform transform in componentsInChildren)
			{
				if (transform != base.transform && transform.parent == base.transform)
				{
					_children.Add(transform);
				}
			}
		}
		else
		{
			_children.AddRange(children);
		}
		if (_children.Count == 0)
		{
			Debug.Log("Child Enabler: No children found");
			return;
		}
		List<int> list = new List<int>();
		for (int j = 0; j < _children.Count; j++)
		{
			list.Add(j);
		}
		List<int> list2 = new List<int>();
		int num = Random.Range(amountToEnable.x, amountToEnable.y);
		for (int k = 0; k < num; k++)
		{
			int index = Random.Range(0, list.Count);
			list2.Add(list[index]);
			list.RemoveAt(index);
		}
		for (int l = 0; l < _children.Count; l++)
		{
			_children[l].gameObject.SetActive(list2.Contains(l));
		}
	}
}
