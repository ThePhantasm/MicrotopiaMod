using UnityEngine;

public class Mover : MonoBehaviour
{
	public Vector3 deltaPosition = Vector3.zero;

	public Vector3 deltaRotation = Vector3.zero;

	public Vector3 deltaScale = Vector3.zero;

	private void Update()
	{
		Vector3 position = base.transform.position;
		position += deltaPosition * Time.deltaTime;
		base.transform.position = position;
		Vector3 eulerAngles = base.transform.rotation.eulerAngles;
		eulerAngles += deltaRotation * Time.deltaTime;
		base.transform.rotation = Quaternion.Euler(eulerAngles);
		Vector3 localScale = base.transform.localScale;
		localScale += deltaScale * Time.deltaTime;
		base.transform.localScale = localScale;
	}
}
