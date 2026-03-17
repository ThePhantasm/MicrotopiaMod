using UnityEngine;

[ExecuteInEditMode]
public class CubeAlligner : MonoBehaviour
{
	public int gridWidth = 10;

	public float gridDistance = 1f;

	private void Update()
	{
		GetComponentsInChildren<Transform>();
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			if (!(child == base.transform) && !(child.parent != base.transform))
			{
				float x = gridDistance * (float)num;
				float z = gridDistance * (float)num2;
				child.localPosition = new Vector3(x, 0f, z);
				num++;
				if (num >= gridWidth)
				{
					num = 0;
					num2++;
				}
			}
		}
	}
}
