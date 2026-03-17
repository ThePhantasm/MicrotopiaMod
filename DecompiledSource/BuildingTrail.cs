using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingTrail
{
	public string name;

	public bool invisible;

	public List<Transform> splitPoints;

	public List<BuildingSplitPointProperties> splitProperties;
}
