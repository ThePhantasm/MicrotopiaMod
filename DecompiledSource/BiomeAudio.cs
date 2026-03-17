using System;
using UnityEngine;

[Serializable]
public struct BiomeAudio
{
	public BiomeType biomeType;

	public AudioClip audioAmbience;

	public AudioClip audioBase;

	public AudioClip audioBusy;

	public AudioClip audioPolluted;

	public AudioClip audioIslandAppears;
}
