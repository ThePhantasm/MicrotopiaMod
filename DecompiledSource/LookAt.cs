using UnityEngine;

[ExecuteInEditMode]
public class LookAt : MonoBehaviour
{
	public Transform lookTarget;

	private void Update()
	{
		if (lookTarget != null)
		{
			base.transform.LookAt(lookTarget);
			base.transform.rotation = Quaternion.Euler(base.transform.rotation.eulerAngles.x, base.transform.rotation.eulerAngles.y, 0f);
		}
	}
}
