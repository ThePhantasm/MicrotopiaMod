using System;
using System.Collections;
using UnityEngine;

public class PlatformNone : PlatformBase
{
	public override IEnumerator KInit(Action<string> callback, Action<float> func_progress)
	{
		string fatal_error = null;
		KoroutineId kid = SetFinalizer(delegate
		{
			func_progress(1f);
			callback(fatal_error);
		});
		try
		{
			func_progress(0f);
			yield return null;
			inited = true;
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	public override void Outit()
	{
	}

	public override string GetUserName()
	{
		string text = Application.persistentDataPath.ToLower();
		int num = text.IndexOf("users");
		if (num > 0)
		{
			text = text[(num + 6)..].Replace("\\", "/");
			return text[..text.IndexOf("/")];
		}
		return "";
	}

	protected override void UpdateGynesFlownReal(int v)
	{
		Debug.Log($"PlatformNone: UpdateGynesFlown -> {v}");
	}

	protected override void GainAchievementReal(Achievement achievement)
	{
		Debug.Log($"PlatformNone: Gain achievement {achievement}");
	}
}
