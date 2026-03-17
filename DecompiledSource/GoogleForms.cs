using System;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleForms
{
	public static void Send(GoogleForm form, string text)
	{
		if (form == GoogleForm.MicrotopiaPlaytestText_nov24)
		{
			text = text.Trim();
			bool flag = text == "";
			string uri = "https://docs.google.com/forms/d/1mMgbP0n7ovTpFHPCzexQEsslNngYj4PUjh6bOPiEZsY/formResponse";
			string fieldName = "entry.1052326000";
			string fieldName2 = "entry.911965311";
			if (flag)
			{
				Debug.Log("GoogleForms.Send: empty feedback, skip");
				return;
			}
			WWWForm wWWForm = new WWWForm();
			string value = ((Platform.current == null) ? "?" : Platform.current.GetUserName()) + ", " + DateTime.Now.ToString("yyMMdd-HHmm");
			wWWForm.AddField(fieldName, value);
			wWWForm.AddField(fieldName2, text);
			UnityWebRequest.Post(uri, wWWForm).SendWebRequest();
			Debug.Log($"Sent text of length {text.Length} to {form}");
		}
		else
		{
			Debug.LogError("Unknown form " + form);
		}
	}
}
