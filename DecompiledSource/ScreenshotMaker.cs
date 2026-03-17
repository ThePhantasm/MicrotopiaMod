using System;
using System.IO;
using UnityEngine;

public class ScreenshotMaker : MonoBehaviour
{
	public Vector2Int resolution = new Vector2Int(1920, 1080);

	public int antiAliasing = 16;

	private string folder = "Screenshots";

	public bool speak;

	public void Update()
	{
		if (Input.GetKey(KeyCode.F2))
		{
			MakeFrame(Camera.main);
		}
	}

	private void MakeFrame(Camera cam)
	{
		Directory.CreateDirectory(folder);
		string path = string.Concat(folder + "/Screenshot_" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), ".png");
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
		File.WriteAllBytes(path, bytes);
		cam.targetTexture = null;
		RenderTexture.active = null;
		renderTexture.Release();
		UnityEngine.Object.Destroy(renderTexture);
		UnityEngine.Object.Destroy(texture2D);
		if (speak)
		{
			Debug.Log("Screenshot made");
		}
	}
}
