using UnityEngine;

[ExecuteInEditMode]
public class BuildingPileCounter : MonoBehaviour
{
	public Storage storage;

	public int count_NONE;

	public int count_INPUT;

	public int count_OUTPUT;

	private void Update()
	{
		count_NONE = 0;
		count_INPUT = 0;
		count_OUTPUT = 0;
		if (!(storage != null))
		{
			return;
		}
		foreach (Pile pile in storage.piles)
		{
			switch (pile.pileType)
			{
			case PileType.NONE:
				count_NONE += pile.maxHeight;
				break;
			case PileType.INPUT:
				count_INPUT += pile.maxHeight;
				break;
			case PileType.OUTPUT:
				count_OUTPUT += pile.maxHeight;
				break;
			}
		}
	}
}
