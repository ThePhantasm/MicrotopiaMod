using UnityEngine;

public class Crusher : Factory
{
	[Header("Crusher")]
	public Transform antParent;

	public override float UseBuilding(int _entrance, Ant _ant, out bool ant_entered)
	{
		float result = base.UseBuilding(_entrance, _ant, out ant_entered);
		_ant.transform.parent = antParent;
		return result;
	}
}
