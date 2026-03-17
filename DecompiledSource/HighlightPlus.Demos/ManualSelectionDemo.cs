using UnityEngine;

namespace HighlightPlus.Demos;

public class ManualSelectionDemo : MonoBehaviour
{
	private HighlightManager hm;

	public Transform objectToSelect;

	private void Start()
	{
		hm = Object.FindObjectOfType<HighlightManager>();
	}

	private void Update()
	{
	}
}
