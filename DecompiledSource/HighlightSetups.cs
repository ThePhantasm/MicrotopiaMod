using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;

public class HighlightSetups : MonoBehaviour
{
	public HighlightStatusLink[] highlightList;

	private Dictionary<HighlightStatus, HighlightEffect> dicHighlights;

	public HighlightEffect GetHighlightEffect(HighlightStatus _status)
	{
		if (dicHighlights == null)
		{
			dicHighlights = new Dictionary<HighlightStatus, HighlightEffect>();
			HighlightStatusLink[] array = highlightList;
			for (int i = 0; i < array.Length; i++)
			{
				HighlightStatusLink highlightStatusLink = array[i];
				if (!dicHighlights.ContainsKey(highlightStatusLink.status))
				{
					dicHighlights.Add(highlightStatusLink.status, highlightStatusLink.effect);
				}
			}
		}
		return dicHighlights[_status];
	}
}
