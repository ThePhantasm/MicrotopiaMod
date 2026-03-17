using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AssignLineStyle
{
	public string name;

	public List<AssignType> types;

	public AssignLineStatus status;

	public GameObject ob;

	public LineRenderer[] lrs;

	public bool arc;

	public AnimationStyle animationStyle;
}
