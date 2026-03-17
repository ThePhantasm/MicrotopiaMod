using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
	public Transform tfRotate;

	public float rotateSpeed;

	public Animator animAnt;

	public TMP_Text text;

	public GameObject pfUiLoading;

	public void Start()
	{
		StartCoroutine(CStartLoadingAnim());
		UILoading ui_loading = UIBase.Spawn<UILoading>(pfUiLoading);
		ui_loading.Init(hide_logo: true);
		text.enabled = false;
		GameInit.instance.Setup(delegate(string fatal_error)
		{
			if (ui_loading != null)
			{
				Object.Destroy(ui_loading.gameObject);
			}
			if (fatal_error == null)
			{
				LoadingDone();
			}
			else
			{
				text.enabled = true;
				text.text = fatal_error;
				text.color = Color.red;
				tfRotate.SetObActive(active: false);
				StartCoroutine(CFatalEnd());
			}
		}, delegate(float f)
		{
			ui_loading.SetProgress(f);
		});
	}

	private IEnumerator CStartLoadingAnim()
	{
		AudioManager.instance.Init();
		animAnt.SetBool("Walk", value: true);
		float a = 0f;
		while (true)
		{
			a += rotateSpeed * Time.deltaTime;
			tfRotate.localRotation = Quaternion.Euler(0f, 0f, a);
			yield return null;
		}
	}

	private void LoadingDone()
	{
		GlobalGameState.GoToMainMenu();
	}

	private IEnumerator CFatalEnd()
	{
		yield return new WaitForSeconds(1f);
		while (!Input.anyKeyDown)
		{
			yield return null;
		}
		GlobalGameState.Quit();
	}
}
