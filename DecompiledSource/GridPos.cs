using UnityEngine;

public class GridPos
{
	public int x;

	public int y;

	public int clearance;

	public bool obstacle;

	public Vector3 pos;

	public GridPos(int _x, int _y, Vector3 _pos)
	{
		x = _x;
		y = _y;
		pos = _pos;
		clearance = -1;
	}
}
