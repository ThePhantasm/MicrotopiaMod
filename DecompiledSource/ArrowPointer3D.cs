using UnityEngine;

public class ArrowPointer3D : MonoBehaviour
{
	private Transform followTarget;

	private void Update()
	{
		if (followTarget != null)
		{
			base.transform.position = followTarget.position;
		}
	}

	public void SetTarget(Transform _target)
	{
		followTarget = _target;
		base.transform.position = followTarget.position;
	}

	public void SetSize(float s)
	{
		base.transform.localScale = Vector3.one * s;
	}
}
