using UnityEngine;

public class AnimCullDelayer : MonoBehaviour
{
	private void OnBecameInvisible()
	{
		Animator componentInParent = GetComponentInParent<Animator>();
		if (componentInParent != null)
		{
			componentInParent.cullingMode = AnimatorCullingMode.CullCompletely;
		}
		Object.Destroy(this);
	}
}
