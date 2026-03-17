using System.Collections.Generic;
using UnityEngine;

public class NavPoint
{
	public Vector2 pos;

	public NavPoint[] neighbours;

	public int neighbourLinks;

	public float fScore;

	public float gScore;

	public NavPoint prevPoint;

	public const float MAX_SCORE = 1E+09f;

	public NavPoint(Vector2 _pos)
	{
		pos = _pos;
		ResetScores();
		ResetLinks();
	}

	public void ResetScores()
	{
		fScore = 1E+09f;
		gScore = 1E+09f;
		prevPoint = null;
	}

	public void SetNeighbours(List<NavPoint> ns)
	{
		neighbours = ns.ToArray();
	}

	public void ResetLinks()
	{
		neighbourLinks = 0;
	}

	public NavPoint GetNeighbourWithOffset(Vector2 d)
	{
		for (int i = 0; i < neighbours.Length; i++)
		{
			NavPoint navPoint = neighbours[i];
			if ((navPoint.pos - pos - d).sqrMagnitude < 1f)
			{
				return navPoint;
			}
		}
		return null;
	}
}
