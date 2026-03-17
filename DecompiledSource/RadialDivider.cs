using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RadialDivider : MonoBehaviour
{
	public GameObject arm;

	public int count = 1;

	public List<GameObject> arms;

	private void Awake()
	{
		arms = new List<GameObject>();
		foreach (Transform item in base.transform)
		{
			arms.Add(item.gameObject);
		}
	}

	private void Update()
	{
		if (!(arm == null))
		{
			if (count < 1)
			{
				count = 1;
			}
			if (arms.Count == 0)
			{
				arms.Add(arm);
			}
			if (arms.Count != count)
			{
				DoDivision();
			}
		}
	}

	private void DoDivision()
	{
		List<GameObject> list = new List<GameObject>();
		foreach (GameObject arm in arms)
		{
			if (arm == null)
			{
				list.Add(arm);
			}
		}
		foreach (GameObject item in list)
		{
			arms.Remove(item);
		}
		for (int i = 0; i < count; i++)
		{
			if (arms.Count < count)
			{
				arms.Add(Object.Instantiate(this.arm, base.transform));
			}
		}
		for (int num = arms.Count - 1; num > count - 1; num--)
		{
			Object.DestroyImmediate(arms[num]);
			arms.RemoveAt(num);
		}
		for (int j = 0; j < arms.Count; j++)
		{
			arms[j].transform.localPosition = Vector3.zero;
			arms[j].transform.localRotation = Quaternion.Euler(new Vector3(0f, 360f / (float)arms.Count * (float)j, 0f));
		}
	}
}
