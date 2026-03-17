using System;
using UnityEngine;

[Serializable]
public struct AudioLink
{
	public AudioClip clip;

	public float pitchVariance;

	public bool IsSet()
	{
		return clip != null;
	}

	public float GetLength()
	{
		if (!(clip == null))
		{
			return clip.length;
		}
		return 0f;
	}
}
