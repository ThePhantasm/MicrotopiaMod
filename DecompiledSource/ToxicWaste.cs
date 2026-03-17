using UnityEngine;

public class ToxicWaste : BiomeObject
{
	public Animator anim;

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		anim.SetFloat(ClickableObject.paramSpeed, Random.Range(0.5f, 1f));
	}
}
