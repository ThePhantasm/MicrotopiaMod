using UnityEngine;

public class DecayEffect : MonoBehaviour
{
	private float duration = 2f;

	private Vector3 startScale;

	private Vector3 startPos;

	private Vector3 offset = new Vector3(0f, -0.8f, 0f);

	private float fade;

	private void Start()
	{
		startScale = base.transform.localScale;
		startPos = base.transform.position;
	}

	private void Update()
	{
		fade += Time.deltaTime / duration;
		if (fade >= 1f)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		base.transform.localScale = startScale * (1f - fade);
		base.transform.position = startPos + offset * fade;
	}
}
