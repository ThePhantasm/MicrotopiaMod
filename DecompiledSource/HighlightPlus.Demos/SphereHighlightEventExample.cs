using UnityEngine;

namespace HighlightPlus.Demos;

public class SphereHighlightEventExample : MonoBehaviour
{
	private HighlightEffect effect;

	private void Start()
	{
		effect = GetComponent<HighlightEffect>();
		effect.OnObjectHighlightStart += ValidateHighlightObject;
	}

	private bool ValidateHighlightObject(GameObject obj)
	{
		return true;
	}

	private void HighlightStart()
	{
		Debug.Log("Gold sphere highlighted!");
	}

	private void HighlightEnd()
	{
		Debug.Log("Gold sphere not highlighted!");
	}

	private void Update()
	{
		if (InputProxy.GetKeyDown("space"))
		{
			effect.HitFX(Color.white, 0.2f);
		}
		if (InputProxy.GetKeyDown("c"))
		{
			effect.SetGlowColor(new Color(Random.value, Random.value, Random.value));
		}
	}
}
