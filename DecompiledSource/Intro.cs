using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Intro : MonoBehaviour
{
	[SerializeField]
	private Image imLogo;

	public void Start()
	{
		imLogo.enabled = false;
		StartCoroutine(CStartIntro());
	}

	private IEnumerator CStartIntro()
	{
		AudioManager.instance.Init();
		Debug.Log("Start intro");
		imLogo.enabled = true;
		SetLogoAlpha(0f);
		yield return new WaitForSeconds(0.2f);
		GlobalGameState.GoToLoading();
	}

	private void SetLogoAlpha(float a)
	{
		imLogo.color = imLogo.color.SetAlpha(a);
	}
}
