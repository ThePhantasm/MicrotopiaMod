using System;
using UnityEngine;
using UnityEngine.UI;

namespace Radishmouse;

public class UILineRenderer : Graphic
{
	public Vector2[] points;

	public float thickness = 10f;

	public bool center = true;

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
		if (points.Length < 2)
		{
			return;
		}
		for (int i = 0; i < points.Length - 1; i++)
		{
			CreateLineSegment(points[i], points[i + 1], vh);
			int num = i * 5;
			vh.AddTriangle(num, num + 1, num + 3);
			vh.AddTriangle(num + 3, num + 2, num);
			if (i != 0)
			{
				vh.AddTriangle(num, num - 1, num - 3);
				vh.AddTriangle(num + 1, num - 1, num - 2);
			}
		}
	}

	private void CreateLineSegment(Vector3 point1, Vector3 point2, VertexHelper vh)
	{
		Vector3 vector = (center ? (base.rectTransform.sizeDelta / 2f) : Vector2.zero);
		UIVertex simpleVert = UIVertex.simpleVert;
		simpleVert.color = color;
		Quaternion quaternion = Quaternion.Euler(0f, 0f, RotatePointTowards(point1, point2) + 90f);
		simpleVert.position = quaternion * new Vector3((0f - thickness) / 2f, 0f);
		simpleVert.position += point1 - vector;
		vh.AddVert(simpleVert);
		simpleVert.position = quaternion * new Vector3(thickness / 2f, 0f);
		simpleVert.position += point1 - vector;
		vh.AddVert(simpleVert);
		Quaternion quaternion2 = Quaternion.Euler(0f, 0f, RotatePointTowards(point2, point1) - 90f);
		simpleVert.position = quaternion2 * new Vector3((0f - thickness) / 2f, 0f);
		simpleVert.position += point2 - vector;
		vh.AddVert(simpleVert);
		simpleVert.position = quaternion2 * new Vector3(thickness / 2f, 0f);
		simpleVert.position += point2 - vector;
		vh.AddVert(simpleVert);
		simpleVert.position = point2 - vector;
		vh.AddVert(simpleVert);
	}

	private float RotatePointTowards(Vector2 vertex, Vector2 target)
	{
		return Mathf.Atan2(target.y - vertex.y, target.x - vertex.x) * (180f / MathF.PI);
	}
}
