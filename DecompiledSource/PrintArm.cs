using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrintArm : MonoBehaviour
{
	[SerializeField]
	private Animator anim;

	[SerializeField]
	private List<GameObject> particleEffects = new List<GameObject>();

	private const float windUpDuration = 1f;

	private float windUpTimer;

	public void Init()
	{
		SetHeight(0f);
		windUpTimer = 0f;
		SetAction(target: false);
		anim.SetBool("Done", value: false);
	}

	public bool IsWindingUp(float dt)
	{
		windUpTimer += dt;
		return windUpTimer < 1f;
	}

	public void SetAction(bool target)
	{
		if (base.isActiveAndEnabled)
		{
			anim.SetBool("DoAction", target);
		}
		foreach (GameObject particleEffect in particleEffects)
		{
			particleEffect.SetObActive(target);
		}
	}

	public void SetHeight(float h)
	{
		base.transform.localPosition = base.transform.localPosition.TargetYPosition(h);
	}

	public IEnumerator CWindDown(bool instant)
	{
		if (base.isActiveAndEnabled)
		{
			anim.SetBool("Done", value: true);
		}
		if (!instant)
		{
			for (float t = 0f; t < 3f; t += Time.deltaTime * GameManager.instance.GetPlaySpeed())
			{
				yield return null;
			}
		}
		this.SetObActive(active: false);
	}
}
