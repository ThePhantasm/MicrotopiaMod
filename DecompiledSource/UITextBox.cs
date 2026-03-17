using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITextBox : MonoBehaviour
{
	public GameObject obBox;

	public List<TextMeshProUGUI> listText = new List<TextMeshProUGUI>();

	public void Clear()
	{
		foreach (TextMeshProUGUI item in listText)
		{
			item.text = "";
		}
	}

	public void EnableAllText()
	{
		foreach (TextMeshProUGUI item in listText)
		{
			item.SetObActive(active: true);
		}
	}
}
