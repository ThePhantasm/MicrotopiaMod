using UnityEngine;

public class LookVectorVisualizer : MonoBehaviour
{
	public Vector3 lookVector;

	private void Update()
	{
		lookVector = base.transform.forward;
	}
}
