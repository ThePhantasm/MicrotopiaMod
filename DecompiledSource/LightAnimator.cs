using UnityEngine;

public class LightAnimator : MonoBehaviour
{
	public Animator anim;

	private void OnEnable()
	{
		if (anim != null)
		{
			anim.SetFloat("Speed", Random.Range(0.5f, 1f));
		}
	}
}
