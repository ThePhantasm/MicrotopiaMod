using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : Singleton
{
	private enum MusicTransitionState
	{
		None,
		Waiting,
		Active,
		Queued
	}

	public static AudioManager instance;

	[SerializeField]
	private Transform tfListener;

	[SerializeField]
	private AudioMixer mixer;

	[SerializeField]
	[Tooltip("Base mixer channel volumes")]
	private float volumeMusic = 0.2f;

	[SerializeField]
	[Tooltip("Base mixer channel volumes")]
	private float volumeSfxUI = 1f;

	[SerializeField]
	[Tooltip("Base mixer channel volumes")]
	private float volumeSfxWorld = 1f;

	[SerializeField]
	[Tooltip("Factor for ambience audio, on top of Volume Sfx World")]
	private float volumeFactorAmbience = 0.5f;

	[SerializeField]
	[Tooltip("Range at which 3d world sounds starts to fade, this wholly determines attenuation curve")]
	private float min3DRange = 20f;

	[SerializeField]
	[Tooltip("Not audible beyond this range (but doesn't affect attenuation curve)")]
	private float max3DRange = 300f;

	[SerializeField]
	[Tooltip("Range at which ambience audio has completely faded out (not using real attenuation)")]
	private float maxAmbienceRange = 500f;

	[SerializeField]
	[Tooltip("Time after camera is looking at new biome before corresponding music starts fading in")]
	private float biomeMusicChangeDelay = 8f;

	private static AudioMixerGroup mixerMusic;

	private static AudioMixerGroup mixerMusic_Normal;

	private static AudioMixerGroup mixerSfxUI;

	private static AudioMixerGroup mixerSfxWorld;

	private static AudioChannel channelMusic;

	private static AudioChannel channelMusic_transition;

	private static AudioChannel channelMusicExtra;

	private static AudioChannel channelUI;

	private static AudioChannel channelUILooped;

	private static AudioChannel channelAmbience;

	private static AudioChannel channelTrrr;

	private static AudioChannel channelWorldShared;

	private static bool dontStopMusicOnClear;

	private static List<AudioChannel> channelsUI3D;

	private static List<AudioChannel> channelsWorld;

	private static float TrrrCumulative;

	private static float lastTrrr;

	private static bool sfxWorldMuted;

	private static AudioClip curMusic;

	private static AudioClip newMusic;

	private static MusicTransitionState musicTransitionState;

	private static float musicTransition;

	private static float musicChangeDuration;

	private static MusicType prevMusicType;

	private static Coroutine cPlayMusicExtra;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	protected override void Awake()
	{
		if (instance != null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		base.Awake();
		base.enabled = false;
	}

	public void Init()
	{
		mixerMusic = mixer.FindMatchingGroups("Music")[0];
		mixerMusic_Normal = mixer.FindMatchingGroups("Music_normal")[0];
		mixerSfxUI = mixer.FindMatchingGroups("Sfx_UI")[0];
		mixerSfxWorld = mixer.FindMatchingGroups("Sfx_world")[0];
		UpdateVolumes();
		channelMusic = new AudioChannel(mixerMusic_Normal, "MusicBase", _is_3d: false);
		channelMusic_transition = new AudioChannel(mixerMusic_Normal, "MusicBase Transition", _is_3d: false);
		channelMusicExtra = new AudioChannel(mixerMusic, "MusicExtra", _is_3d: false);
		channelUI = new AudioChannel(mixerSfxUI, "UI", _is_3d: false);
		channelUILooped = new AudioChannel(mixerSfxUI, "UI", _is_3d: false);
		channelAmbience = new AudioChannel(mixerSfxWorld, "Ambience", _is_3d: true, 0f, maxAmbienceRange);
		channelAmbience.source.rolloffMode = AudioRolloffMode.Custom;
		channelTrrr = new AudioChannel(mixerSfxUI, "Trrr", _is_3d: true, 2000f, 2000f);
		channelWorldShared = new AudioChannel(mixerSfxWorld, "WorldShared", _is_3d: true, min3DRange, max3DRange);
		channelsUI3D = new List<AudioChannel>();
		channelsWorld = new List<AudioChannel>();
		base.enabled = true;
	}

	public void UpdateVolumes()
	{
		mixer.SetFloat("volMusic", VolumeToDb(volumeMusic * Player.musicVolume * Player.globalVolume));
		mixer.SetFloat("volSfxUI", VolumeToDb(volumeSfxUI * Player.uiVolume * Player.globalVolume));
		mixer.SetFloat("volSfxWorld", VolumeToDb(sfxWorldMuted ? 0f : (volumeSfxWorld * Player.worldVolume * Player.globalVolume)));
	}

	public static void SetWorldMuted(bool muted)
	{
		sfxWorldMuted = muted;
		instance.UpdateVolumes();
	}

	public static void SetPause(bool paused)
	{
		if (paused)
		{
			foreach (AudioChannel item in channelsWorld)
			{
				item.isPaused = item.IsPlaying() && !item.source.loop;
			}
			return;
		}
		foreach (AudioChannel item2 in channelsWorld)
		{
			if (item2.isPaused)
			{
				item2.Unpause();
				item2.isPaused = false;
			}
		}
	}

	public void UpdateMainCamera()
	{
		channelAmbience.Attach(CamController.instance.transform);
	}

	public static void PlayWorldShort(Vector3 pos, AudioClip clip)
	{
		channelWorldShared.SetPos(pos);
		channelWorldShared.PlayOnce(clip);
	}

	public static void PlayUI(UISfx ui_sfx, float pitch = 1f)
	{
		PlayUI(AudioLinks.standard.GetClip(ui_sfx), pitch);
	}

	public static void PlayUI(AudioClip clip, float pitch = 1f)
	{
		if (!(clip == null))
		{
			channelUI.SetPitch(pitch);
			channelUI.PlayOnce(clip);
		}
	}

	public static void PlayUI(Vector3 pos, UISfx3D ui_sfx_3d)
	{
		AudioClip clip = AudioLinks.standard.GetClip(ui_sfx_3d);
		if (!(clip == null))
		{
			AudioChannel freeChannel = GetFreeChannel(channelsUI3D, () => new AudioChannel(mixerSfxUI, "UI3D", _is_3d: true, 2000f, 2000f));
			freeChannel.SetPos(pos);
			freeChannel.Play(clip);
		}
	}

	public static void PlayUILoop(UISfx ui_sfx)
	{
		AudioClip clip = AudioLinks.standard.GetClip(ui_sfx);
		if (!(clip == null))
		{
			channelUILooped.Play(clip, looped: true);
		}
	}

	public static void StopUILoop()
	{
		channelUILooped.Stop();
	}

	public static AudioChannel GetBuildingChannel()
	{
		return GetWorldChannel();
	}

	public static AudioChannel GetAntChannel()
	{
		return GetWorldChannel();
	}

	public static AudioChannel GetLooseChannel()
	{
		return GetWorldChannel();
	}

	private static AudioChannel GetWorldChannel()
	{
		return GetFreeChannel(channelsWorld, () => new AudioChannel(mixerSfxWorld, "World", _is_3d: true, instance.min3DRange, instance.max3DRange));
	}

	public static void PlayMusic(MusicType music_type, bool dont_stop = false)
	{
		AudioClip clipMusic = AudioLinks.standard.GetClipMusic(music_type);
		StopMusic();
		if (clipMusic != null)
		{
			channelMusic.Play(clipMusic, looped: true);
			dontStopMusicOnClear = dont_stop;
		}
	}

	public static void PlayMusicExtra(UISfx ui_sfx)
	{
		AudioClip clip = AudioLinks.standard.GetClip(ui_sfx);
		if (!(clip == null))
		{
			StopMusicExtra();
			cPlayMusicExtra = instance.StartCoroutine(instance.CPlayMusicExtra(clip));
		}
	}

	private IEnumerator CPlayMusicExtra(AudioClip clip)
	{
		channelMusicExtra.Play(clip);
		float dur = 4f;
		for (float f = 1f; f >= 0f; f -= Time.deltaTime / dur)
		{
			SetMusicNormal(f);
			yield return null;
		}
		SetMusicNormal(0f);
		while (channelMusicExtra.IsPlaying())
		{
			yield return null;
		}
		for (float f = 0f; f < 1f; f += Time.deltaTime / dur)
		{
			SetMusicNormal(f);
			yield return null;
		}
		SetMusicNormal(1f);
		cPlayMusicExtra = null;
	}

	private void SetMusicNormal(float f)
	{
		mixer.SetFloat("volMusicNormal", VolumeToDb(f));
	}

	private static void StopMusicExtra()
	{
		if (cPlayMusicExtra != null)
		{
			channelMusicExtra.Stop();
			cPlayMusicExtra = null;
		}
		SetMusicVolumeFactor(1f);
	}

	public static void SetMusicVolumeFactor(float f)
	{
		channelMusic.source.volume = f;
	}

	public static void SetAmbience(BiomeType ambient, float factor)
	{
		AudioSource source = channelAmbience.source;
		if (ambient == BiomeType.NONE)
		{
			source.volume = 0f;
			return;
		}
		AudioClip clipAmbience = AudioLinks.standard.GetClipAmbience(ambient);
		if (clipAmbience != source.clip)
		{
			source.clip = clipAmbience;
			if (clipAmbience == null)
			{
				source.Stop();
			}
			else
			{
				source.time = Time.realtimeSinceStartup % source.clip.length;
				source.loop = true;
				source.Play();
			}
		}
		source.volume = factor * instance.volumeFactorAmbience;
	}

	public static void SetGameMusic(MusicType music, BiomeType biome_type, bool _busy, bool _polluted, int pop_intensity)
	{
		AudioClip audioClip = null;
		switch (music)
		{
		case MusicType.Biome:
			audioClip = ((!_polluted) ? ((!_busy) ? AudioLinks.standard.GetClipBiomeMusicBase(biome_type) : AudioLinks.standard.GetClipBiomeMusicBusy(biome_type)) : AudioLinks.standard.GetClipBiomeMusicPolluted(biome_type));
			break;
		case MusicType.Map:
			audioClip = AudioLinks.standard.GetClipMapMusic(pop_intensity);
			break;
		case MusicType.TechTree:
			audioClip = AudioLinks.standard.GetClipMusic(music);
			break;
		}
		if (audioClip == ((musicTransitionState == MusicTransitionState.None) ? curMusic : newMusic))
		{
			return;
		}
		if (!channelMusic.IsPlaying())
		{
			curMusic = audioClip;
			dontStopMusicOnClear = false;
			channelMusic.source.volume = 1f;
			channelMusic.Play(audioClip, looped: true);
			musicTransitionState = MusicTransitionState.None;
			channelMusic_transition.Stop();
		}
		else
		{
			float num;
			switch (prevMusicType)
			{
			case MusicType.Biome:
			{
				float num2 = ((music != MusicType.Biome) ? 1f : 10f);
				num = num2;
				break;
			}
			case MusicType.Map:
			{
				float num2 = ((music != MusicType.Map) ? 2f : 1f);
				num = num2;
				break;
			}
			case MusicType.TechTree:
				num = 1f;
				break;
			default:
				num = 0.001f;
				break;
			}
			float duration = num;
			instance.StartMusicTransition(audioClip, duration);
		}
		prevMusicType = music;
		switch (music)
		{
		case MusicType.TechTree:
		case MusicType.Map:
			SetAmbience(BiomeType.NONE, 0f);
			break;
		case MusicType.Biome:
			GameManager.instance.SetAmbienceAudio();
			break;
		}
	}

	public static void StopMusic()
	{
		channelMusic.Stop();
		channelMusic_transition.Stop();
		StopMusicExtra();
		curMusic = (newMusic = null);
		musicTransitionState = MusicTransitionState.None;
		prevMusicType = MusicType.None;
	}

	private void StartMusicTransition(AudioClip clip, float duration)
	{
		switch (musicTransitionState)
		{
		case MusicTransitionState.None:
		case MusicTransitionState.Waiting:
			newMusic = clip;
			musicChangeDuration = duration;
			musicTransition = 0f;
			musicTransitionState = MusicTransitionState.Waiting;
			channelMusic_transition.source.volume = 0f;
			channelMusic_transition.Play(newMusic, looped: true);
			break;
		case MusicTransitionState.Active:
		{
			bool flag = false;
			if (curMusic == clip)
			{
				AudioClip audioClip = newMusic;
				AudioClip audioClip2 = curMusic;
				curMusic = audioClip;
				newMusic = audioClip2;
				musicChangeDuration = duration;
				flag = true;
			}
			else
			{
				if (musicTransition < 0.5f)
				{
					flag = true;
				}
				musicTransitionState = MusicTransitionState.Queued;
				newMusic = clip;
				musicChangeDuration = duration;
			}
			if (flag)
			{
				AudioChannel audioChannel = channelMusic_transition;
				AudioChannel audioChannel2 = channelMusic;
				channelMusic = audioChannel;
				channelMusic_transition = audioChannel2;
				musicTransition = 1f - musicTransition;
			}
			break;
		}
		case MusicTransitionState.Queued:
			newMusic = clip;
			musicChangeDuration = duration;
			break;
		}
	}

	private void Update()
	{
		switch (musicTransitionState)
		{
		case MusicTransitionState.Waiting:
			musicTransition += Time.deltaTime;
			if (musicTransition > Mathf.Max(0.1f, instance.biomeMusicChangeDelay))
			{
				musicTransition = 0f;
				channelMusic_transition.source.time = ((newMusic == null) ? 0f : (Time.realtimeSinceStartup % newMusic.length));
				musicTransitionState = MusicTransitionState.Active;
			}
			break;
		case MusicTransitionState.Active:
		case MusicTransitionState.Queued:
		{
			bool flag = musicTransitionState == MusicTransitionState.Queued;
			musicTransition += Time.deltaTime / (flag ? 0.5f : musicChangeDuration);
			if (musicTransition < 1f)
			{
				channelMusic_transition.source.volume = musicTransition;
				channelMusic.source.volume = 1f - musicTransition;
				break;
			}
			channelMusic_transition.source.volume = 1f;
			channelMusic.Stop();
			AudioChannel audioChannel = channelMusic_transition;
			AudioChannel audioChannel2 = channelMusic;
			channelMusic = audioChannel;
			channelMusic_transition = audioChannel2;
			curMusic = channelMusic.source.clip;
			musicTransitionState = MusicTransitionState.None;
			if (flag)
			{
				StartMusicTransition(newMusic, musicChangeDuration);
			}
			else
			{
				newMusic = null;
			}
			break;
		}
		}
		CullWorldAudio();
	}

	private void CullWorldAudio()
	{
		foreach (AudioChannel item in channelsWorld)
		{
			if (!item.IsFree() && (item.isCulled || item.IsPlaying()))
			{
				item.SetCulled(item.OutOfRange());
			}
		}
	}

	private static float VolumeToDb(float v)
	{
		if (v == 0f)
		{
			return float.MinValue;
		}
		return 10f * Mathf.Log10(v * v);
	}

	public static void StartTrrr(UISfx3D sfx)
	{
		channelTrrr.source.clip = AudioLinks.standard.GetClip(sfx);
		lastTrrr = Time.realtimeSinceStartup;
	}

	public static void EndTrrr()
	{
	}

	public static void PlayTrrr(Vector3 pos, float d_move)
	{
		if (d_move > 0f)
		{
			TrrrCumulative += d_move;
			if ((TrrrCumulative > 15f && lastTrrr < Time.realtimeSinceStartup - 0.03f) || lastTrrr < Time.realtimeSinceStartup - 0.25f)
			{
				channelTrrr.SetPos(pos);
				channelTrrr.source.Play();
				lastTrrr = Time.realtimeSinceStartup;
				TrrrCumulative = 0f;
			}
		}
	}

	private static AudioChannel GetFreeChannel(List<AudioChannel> list, Func<AudioChannel> func_add_new)
	{
		AudioChannel audioChannel = null;
		for (int i = 0; i < list.Count; i++)
		{
			AudioChannel audioChannel2 = list[i];
			if (audioChannel2.source == null)
			{
				return list[i] = func_add_new();
			}
			if (audioChannel2.IsFree())
			{
				audioChannel2.Reset();
				audioChannel = audioChannel2;
				break;
			}
		}
		if (audioChannel == null)
		{
			audioChannel = func_add_new();
			list.Add(audioChannel);
		}
		return audioChannel;
	}

	public static WorldSfx GetPickupSfx(PickupType type)
	{
		switch (type)
		{
		case PickupType.ENERGY_POD:
			return WorldSfx.AntPickupEnergyPod;
		case PickupType.LARVAE_T1:
			return WorldSfx.AntPickupLarva;
		case PickupType.IRON_RAW:
			return WorldSfx.AntPickupIronRaw;
		case PickupType.IRON_BAR:
			return WorldSfx.AntPickupIronBar;
		case PickupType.COPPER_RAW:
			return WorldSfx.AntPickupCopperRaw;
		case PickupType.COPPER_BAR:
			return WorldSfx.AntPickupCopperBar;
		case PickupType.FIBER_SPIKETREE:
		case PickupType.FIBER_WORMTREE:
		case PickupType.FIBER_BUBBLETREE:
		case PickupType.FIBER_FLOWER:
			return WorldSfx.AntPickupFiber;
		default:
			return WorldSfx.AntPickup;
		}
	}

	public static WorldSfx GetDropSfx(PickupType type)
	{
		switch (type)
		{
		case PickupType.ENERGY_POD:
			return WorldSfx.AntDropEnergyPod;
		case PickupType.LARVAE_T1:
			return WorldSfx.AntDropLarva;
		case PickupType.IRON_RAW:
			return WorldSfx.AntDropIronRaw;
		case PickupType.IRON_BAR:
			return WorldSfx.AntDropIronBar;
		case PickupType.COPPER_RAW:
			return WorldSfx.AntDropCopperRaw;
		case PickupType.COPPER_BAR:
			return WorldSfx.AntDropCopperBar;
		case PickupType.FIBER_SPIKETREE:
		case PickupType.FIBER_WORMTREE:
		case PickupType.FIBER_BUBBLETREE:
		case PickupType.FIBER_FLOWER:
			return WorldSfx.AntDropFiber;
		default:
			return WorldSfx.AntDrop;
		}
	}

	public static void Clear()
	{
		TrrrCumulative = (lastTrrr = 0f);
		sfxWorldMuted = false;
		if (!dontStopMusicOnClear)
		{
			StopMusic();
		}
		foreach (AudioChannel item in channelsWorld)
		{
			item.Destroy();
		}
		channelsWorld.Clear();
	}
}
