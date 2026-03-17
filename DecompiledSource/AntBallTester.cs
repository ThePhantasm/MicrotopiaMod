using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AntBallTester : MonoBehaviour
{
	public List<AntBallSpawnData> data;

	public bool randomizeX;

	public bool randomizeY;

	public bool randomizeZ;

	public Vector2 yDif = Vector2.zero;

	public bool doSpawn;

	private void Start()
	{
	}

	private void Update()
	{
		if (!doSpawn)
		{
			return;
		}
		doSpawn = false;
		int childCount = base.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Object.DestroyImmediate(base.transform.GetChild(0).gameObject);
		}
		foreach (AntBallSpawnData datum in data)
		{
			for (int j = 0; j < datum.count; j++)
			{
				GameObject gameObject = Object.Instantiate(datum.prefab, base.transform);
				gameObject.transform.localPosition = Vector3.zero;
				List<GameObject> list = new List<GameObject>();
				Transform[] componentsInChildren = gameObject.transform.GetComponentsInChildren<Transform>();
				foreach (Transform transform in componentsInChildren)
				{
					if (transform.gameObject.layer == 22)
					{
						list.Add(transform.gameObject);
					}
				}
				foreach (GameObject item in list)
				{
					Object.DestroyImmediate(item);
				}
				Collider[] componentsInChildren2 = GetComponentsInChildren<Collider>();
				for (int k = 0; k < componentsInChildren2.Length; k++)
				{
					Object.DestroyImmediate(componentsInChildren2[k]);
				}
				Ant[] componentsInChildren3 = GetComponentsInChildren<Ant>();
				for (int k = 0; k < componentsInChildren3.Length; k++)
				{
					Object.DestroyImmediate(componentsInChildren3[k]);
				}
				Vector3 zero = Vector3.zero;
				if (randomizeX)
				{
					zero.x = Random.Range(0f, 360f);
				}
				if (randomizeY)
				{
					zero.y = Random.Range(0f, 360f);
				}
				if (randomizeZ)
				{
					zero.z = Random.Range(0f, 360f);
				}
				gameObject.transform.rotation = Quaternion.Euler(zero);
				Vector3 zero2 = Vector3.zero;
				zero2 += gameObject.transform.up * Random.Range(yDif.x, yDif.y);
				gameObject.transform.localPosition = zero2;
			}
		}
	}
}
