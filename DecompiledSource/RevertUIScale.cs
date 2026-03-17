using UnityEngine;

public class RevertUIScale : UIBase
{
	private void OnEnable()
	{
		float num = 1f / Player.uiScale;
		rtBase.localScale = new Vector3(num, num, 1f);
	}
}
