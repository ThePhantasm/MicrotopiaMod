using System.Collections.Generic;
using UnityEngine;

public class SourceMesh : MonoBehaviour
{
	public Transform topPoint;

	public Transform sidePoint;

	public List<Transform> growPoints;

	public float chance = 1f;

	[Tooltip("How much more/less units of the material this variant has.")]
	public float multiplier = 1f;
}
