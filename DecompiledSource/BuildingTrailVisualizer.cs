using UnityEngine;

[ExecuteInEditMode]
public class BuildingTrailVisualizer : MonoBehaviour
{
	public Building building;

	private void Update()
	{
		if (!(building != null))
		{
			return;
		}
		foreach (BuildingTrail buildingTrail in building.buildingTrails)
		{
			for (int i = 0; i < buildingTrail.splitPoints.Count - 1; i++)
			{
				Vector3 position = buildingTrail.splitPoints[i].position;
				Vector3 position2 = buildingTrail.splitPoints[i + 1].position;
				Debug.DrawLine(position, position2, Color.red);
			}
		}
	}
}
