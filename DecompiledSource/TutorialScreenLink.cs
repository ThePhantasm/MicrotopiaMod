using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TutorialScreenLink
{
	public string name;

	public Tutorial tutorial;

	public List<GameObject> screens = new List<GameObject>();

	public bool inList = true;
}
