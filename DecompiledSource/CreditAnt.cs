using TMPro;
using UnityEngine;

public class CreditAnt : MonoBehaviour
{
	[SerializeField]
	private Animator anim;

	[SerializeField]
	private TMP_Text text;

	[SerializeField]
	private float animSpeed = 1f;

	private Transform tfAnt;

	private Transform tfText;

	private Vector3 dir;

	private float speed;

	private float remainingDist;

	public void Init(string _text, Vector3 _pos, Vector3 _dir, float _speed, float _remaining_dist)
	{
		dir = _dir;
		speed = _speed;
		remainingDist = _remaining_dist;
		tfAnt = base.transform;
		tfText = text.transform;
		tfAnt.SetPositionAndRotation(_pos, Quaternion.LookRotation(dir, Vector3.up));
		anim.SetBool("Walk", value: true);
		anim.SetBool("Carry", value: true);
		anim.SetFloat("Walk Speed", animSpeed);
		text.text = Loc.GetCredits(_text);
	}

	public bool DoUpdate(float dt)
	{
		float num = speed * dt;
		tfAnt.position += dir * num;
		remainingDist -= num;
		return remainingDist < 0f;
	}

	public void Stop()
	{
		anim.SetBool("Walk", value: false);
	}
}
