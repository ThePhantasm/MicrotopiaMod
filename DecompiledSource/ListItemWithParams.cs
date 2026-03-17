using UnityEngine;

public abstract class ListItemWithParams
{
	protected string className = "ListItemWithParams";

	protected abstract void Parse(string txt, string[] strs);

	protected ListItemWithParams()
	{
	}

	public ListItemWithParams(string txt)
	{
		if (!string.IsNullOrEmpty(txt))
		{
			string[] strs = txt.Trim().Split(' ');
			Parse(txt, strs);
		}
	}

	protected bool ArgCountOk(string txt, string[] strs, int n_args)
	{
		if (strs.Length == n_args + 1)
		{
			return true;
		}
		Debug.LogWarning(className + ": '" + txt + "' parse error (args != " + n_args + ")");
		return false;
	}
}
