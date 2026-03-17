using UnityEngine;

public class UIButtonBehavior : UIButton
{
	public ButtonBehavior behavior;

	private void OnEnable()
	{
		switch (behavior)
		{
		case ButtonBehavior.LINK_DISCORD:
			Init(delegate
			{
				Application.OpenURL(GlobalValues.standard.discordLink);
			});
			break;
		case ButtonBehavior.LINK_GOOGLEFORM:
			Init(delegate
			{
				Application.OpenURL(GlobalValues.standard.googleFormLink);
			});
			break;
		}
	}
}
