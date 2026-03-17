using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PileConfigurer : MonoBehaviour
{
	public Storage storage;

	public List<PileList> lists;

	[Space(10f)]
	public bool configurePiles;

	public bool enablePileChildren;

	public bool disablePileChildren;

	private void Update()
	{
		if (configurePiles)
		{
			configurePiles = false;
			storage.piles.Clear();
			foreach (PileList list in lists)
			{
				foreach (Transform pile in list.piles)
				{
					storage.piles.Add(new Pile(pile, list.type, list.pileHeight));
				}
			}
		}
		if (enablePileChildren)
		{
			enablePileChildren = false;
			foreach (PileList list2 in lists)
			{
				foreach (Transform pile2 in list2.piles)
				{
					for (int i = 0; i < pile2.childCount; i++)
					{
						pile2.GetChild(i).SetObActive(active: true);
					}
				}
			}
		}
		if (!disablePileChildren)
		{
			return;
		}
		disablePileChildren = false;
		foreach (PileList list3 in lists)
		{
			foreach (Transform pile3 in list3.piles)
			{
				for (int j = 0; j < pile3.childCount; j++)
				{
					pile3.GetChild(j).SetObActive(active: false);
				}
			}
		}
	}
}
