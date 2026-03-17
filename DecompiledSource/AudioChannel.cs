using UnityEngine;
using UnityEngine.Audio;

public class AudioChannel
{
	public AudioSource source;

	private bool is3D;

	private bool attached;

	private Transform transform;

	private bool isLocked;

	private double startTime;

	public bool isCulled;

	public bool isPaused;

	public AudioChannel(AudioMixerGroup mixer_group, string name, bool _is_3d, float d_min = 5f, float d_max = 300f)
	{
		is3D = _is_3d;
		GameObject gameObject = new GameObject("AudioChannel (" + name + ")");
		transform = gameObject.transform;
		transform.SetParent(AudioManager.instance.transform);
		source = gameObject.AddComponent<AudioSource>();
		source.playOnAwake = false;
		source.pitch = 1f;
		source.volume = 1f;
		source.loop = false;
		source.spatialBlend = (is3D ? 1f : 0f);
		source.outputAudioMixerGroup = mixer_group;
		source.dopplerLevel = 0f;
		if (is3D)
		{
			source.rolloffMode = AudioRolloffMode.Linear;
			source.minDistance = d_min;
			source.maxDistance = d_max;
		}
	}

	public void Reset()
	{
		if (attached)
		{
			DeAttach();
		}
		source.loop = false;
		source.dopplerLevel = 0f;
		source.time = 0f;
		source.pitch = 1f;
		startTime = 0.0;
		isCulled = false;
	}

	public void Play(AudioLink link, bool looped = false, float start_time = 0f)
	{
		if (link.IsSet())
		{
			Play(link, looped, start_time, 0f);
		}
	}

	public void PlayDelayed(AudioLink link, bool looped, float delay)
	{
		if (link.IsSet())
		{
			Play(link, looped, 0f, delay);
		}
	}

	private void Play(AudioLink link, bool looped, float start_time, float delay)
	{
		source.pitch = ((link.pitchVariance == 0f) ? 1f : (1f + link.pitchVariance * (Random.value - 0.5f)));
		if (looped)
		{
			start_time = Random.Range(0f, link.GetLength() * 0.99f);
		}
		startTime = GameManager.instance.gameTime - (double)start_time;
		Play(link.clip, looped, start_time, delay);
	}

	public void Unpause()
	{
		if (!(source.clip == null))
		{
			float num = (float)(GameManager.instance.gameTime - startTime);
			if (num < source.clip.length)
			{
				source.time = num;
				source.Play();
			}
		}
	}

	public void Play(AudioClip clip, bool looped = false, float start_time = 0f, float delay = 0f)
	{
		source.clip = clip;
		source.loop = looped;
		if (!isCulled)
		{
			source.time = start_time;
			if (delay > 0f)
			{
				source.PlayDelayed(delay);
			}
			else
			{
				source.Play();
			}
		}
	}

	public bool OutOfRange()
	{
		return false;
	}

	public bool IsPlaying()
	{
		if (source.isPlaying)
		{
			return source.clip != null;
		}
		return false;
	}

	public bool IsFree()
	{
		if (!IsPlaying() && !isLocked)
		{
			return !isPaused;
		}
		return false;
	}

	public void Stop()
	{
		if (source == null)
		{
			Debug.LogError("GB null ref!");
		}
		else
		{
			source.Stop();
		}
	}

	public void PlayOnce(AudioClip clip)
	{
		source.PlayOneShot(clip);
	}

	public void SetPos(Vector3 pos)
	{
		transform.position = pos;
	}

	public void Attach(Transform tf)
	{
		if (transform.parent != tf)
		{
			transform.SetParent(tf, worldPositionStays: false);
			transform.localPosition = Vector3.zero;
		}
		attached = true;
	}

	public void DeAttach()
	{
		if (attached)
		{
			transform.SetParent(AudioManager.instance.transform, worldPositionStays: true);
			attached = false;
		}
	}

	public void SetDoppler(bool active)
	{
		source.dopplerLevel = (active ? 1f : 0f);
	}

	public void SetPitch(float _pitch = 1f)
	{
		source.pitch = _pitch;
	}

	public void Lock()
	{
		isLocked = true;
	}

	public void Free()
	{
		Stop();
		DeAttach();
		isLocked = false;
	}

	public void Destroy()
	{
		if (transform != null && transform.gameObject != null)
		{
			Object.Destroy(transform.gameObject);
		}
	}

	public void InitCulled()
	{
		isCulled = OutOfRange();
	}

	public void SetCulled(bool _culled)
	{
		if (isCulled == _culled)
		{
			return;
		}
		isCulled = _culled;
		if (isCulled)
		{
			source.Stop();
		}
		else
		{
			if (!(source.clip != null))
			{
				return;
			}
			if (source.loop)
			{
				source.time = Random.Range(0f, source.clip.length * 0.99f);
				source.Play();
				return;
			}
			float num = (float)(GameManager.instance.gameTime - startTime);
			if (num < source.clip.length)
			{
				source.time = num;
				source.Play();
			}
		}
	}
}
