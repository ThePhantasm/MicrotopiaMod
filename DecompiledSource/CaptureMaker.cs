using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CaptureMaker : MonoBehaviour
{
	public static CaptureMaker instance;

	public List<CameraSet> cameras = new List<CameraSet>();

	public Vector2Int resolution = new Vector2Int(1920, 1080);

	public float framerate = 24f;

	public int antiAliasing = 16;

	public float waitBeforeStart;

	public float duration = 60f;

	private string folder = "Capture Data";

	private string path = "";

	public bool speak;

	private int c;

	private bool multipleCams;

	private void Awake()
	{
		instance = this;
	}

	public void DoStart()
	{
		if (base.isActiveAndEnabled)
		{
			c = 0;
			StartCoroutine(CStartCapture(waitBeforeStart, duration));
		}
	}

	private IEnumerator CStartCapture(float wait_time, float capture_time)
	{
		Directory.CreateDirectory(folder);
		path = folder + "/Capture_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
		Directory.CreateDirectory(path);
		int num = 0;
		foreach (CameraSet camera in cameras)
		{
			if (camera.doCapture)
			{
				num++;
			}
		}
		multipleCams = num > 1;
		if (multipleCams)
		{
			foreach (CameraSet camera2 in cameras)
			{
				if (camera2.doCapture)
				{
					Directory.CreateDirectory(path + "/" + camera2.cam.name);
				}
			}
		}
		if (wait_time > 0f)
		{
			Debug.Log("Waiting " + wait_time + " seconds before starting with capturing");
			yield return new WaitForSeconds(wait_time);
			Debug.Log("Done waiting");
		}
		if (duration == 0f)
		{
			Debug.Log("Capturing indefinitely");
		}
		else
		{
			Debug.Log("Capturing for " + duration + " seconds");
		}
		StartCoroutine(CCapture());
	}

	private IEnumerator CCapture()
	{
		float count = 1f;
		float wait_time = 1f / framerate;
		while ((float)c / framerate < duration || duration == 0f)
		{
			float time_waited = 0f;
			while (time_waited < wait_time)
			{
				time_waited += Time.deltaTime;
				yield return null;
			}
			c++;
			foreach (CameraSet camera in cameras)
			{
				if (camera.doCapture)
				{
					MakeFrame(camera.cam, path + (multipleCams ? ("/" + camera.cam.name) : ""));
				}
			}
			if ((float)c / framerate > count)
			{
				if (speak)
				{
					Debug.Log(count);
				}
				count += 1f;
			}
			yield return null;
		}
	}

	private void MakeFrame(Camera cam, string _path)
	{
		string text = $"{_path}/{c:D04} .png";
		int x = resolution.x;
		int y = resolution.y;
		RenderTexture renderTexture = new RenderTexture(x, y, 24)
		{
			antiAliasing = antiAliasing
		};
		cam.targetTexture = renderTexture;
		cam.Render();
		Texture2D texture2D = new Texture2D(x, y, TextureFormat.RGB24, mipChain: false);
		RenderTexture.active = renderTexture;
		texture2D.ReadPixels(cam.pixelRect, 0, 0);
		texture2D.Apply();
		byte[] bytes = texture2D.EncodeToPNG();
		File.WriteAllBytes(text, bytes);
		cam.targetTexture = null;
		RenderTexture.active = null;
		renderTexture.Release();
		UnityEngine.Object.Destroy(renderTexture);
		UnityEngine.Object.Destroy(texture2D);
	}
}
