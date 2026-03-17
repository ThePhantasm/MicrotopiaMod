using TMPro;
using UnityEngine;

public class AutoLoc : MonoBehaviour
{
	public TMP_Text text;

	public LocType type = LocType.UI;

	public string code;

	public bool allCaps;

	private void OnValidate()
	{
		text = GetComponent<TMP_Text>();
	}

	private void Start()
	{
		Loc.Register(this);
		if (Loc.loaded)
		{
			FillText();
		}
	}

	public void FillText()
	{
		if (!string.IsNullOrEmpty(code))
		{
			string text = type switch
			{
				LocType.UI => Loc.GetUI(code), 
				LocType.OBJECT => Loc.GetObject(code), 
				LocType.TUTORIAL => Loc.GetTutorial(code), 
				_ => "?_" + type.ToString() + "_?", 
			};
			this.text.Set(allCaps ? Loc.Upper(text) : text);
		}
	}

	private void OnDestroy()
	{
		Loc.Deregister(this);
	}
}
