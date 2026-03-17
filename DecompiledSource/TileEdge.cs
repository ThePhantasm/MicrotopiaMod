using System;
using UnityEngine;

public struct TileEdge
{
	public FloorTile tile;

	public Vector2 pos;

	public float angle;

	public void DebugDraw(Color col)
	{
		DebugDraw(col, Vector3.zero);
	}

	public void DebugDraw(Color col, Vector3 offset)
	{
		Vector3 vector = pos.To3D() + offset;
		float f = (angle - 90f) * (MathF.PI / 180f);
		Toolkit.DebugDrawCircle(vector, 1f, col);
		Debug.DrawLine(vector, vector + 2f * new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)), col, 0f, depthTest: false);
	}

	public TileEdge Inverse()
	{
		TileEdge result = default(TileEdge);
		result.tile = tile;
		result.pos = pos;
		result.angle = ((angle < 180f) ? (angle + 180f) : (angle - 180f));
		return result;
	}
}
