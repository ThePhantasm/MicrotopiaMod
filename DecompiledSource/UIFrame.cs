using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFrame : MonoBehaviour
{
	public static UIFrame instance;

	[SerializeField]
	private List<Image> imList = new List<Image>();

	private void Awake()
	{
		instance = this;
		SetFrame();
	}

	public void SetFrame()
	{
		SetFrame(Color.clear);
	}

	public void SetFrame(Color col)
	{
		foreach (Image im in imList)
		{
			im.color = col;
		}
	}
}
