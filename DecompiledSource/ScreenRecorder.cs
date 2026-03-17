using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScreenRecorder : MonoBehaviour
{
	public static ScreenRecorder instance;

	public int maxFrames;

	public int frameRate = 24;

	private Thread encoderThread;

	private RenderTexture tempRenderTexture;

	private Texture2D tempTexture2D;

	private float captureFrameTime;

	private float lastFrameTime;

	private int frameNumber;

	private int savingFrameNumber;

	private Queue<byte[]> frameQueue;

	private string path;

	private int screenWidth;

	private int screenHeight;

	private bool threadIsProcessing;

	private bool terminateThreadWhenDone;

	private void Awake()
	{
		instance = this;
	}

	public void DoStart()
	{
		Camera component = GetComponent<Camera>();
		string text = "Capture Data";
		Directory.CreateDirectory(text);
		path = text + "/Capture_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
		Directory.CreateDirectory(path);
		Directory.CreateDirectory(path + "/" + component.name);
		screenWidth = component.pixelWidth;
		screenHeight = component.pixelHeight;
		tempRenderTexture = new RenderTexture(screenWidth, screenHeight, 0);
		tempTexture2D = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, mipChain: false);
		frameQueue = new Queue<byte[]>();
		frameNumber = 0;
		savingFrameNumber = 0;
		captureFrameTime = 1f / (float)frameRate;
		lastFrameTime = Time.time;
		if (encoderThread != null && (threadIsProcessing || encoderThread.IsAlive))
		{
			threadIsProcessing = false;
			encoderThread.Join();
		}
		threadIsProcessing = true;
		encoderThread = new Thread(EncodeAndSave);
		encoderThread.Start();
	}

	private void OnDisable()
	{
		Application.targetFrameRate = -1;
		terminateThreadWhenDone = true;
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		MonoBehaviour.print("Doing OnRenderImage");
		if (frameNumber <= maxFrames)
		{
			if (source.width != screenWidth || source.height != screenHeight)
			{
				threadIsProcessing = false;
				base.enabled = false;
				throw new UnityException("ScreenRecorder render target size has changed!");
			}
			float time = Time.time;
			int num = (int)(time / captureFrameTime) - (int)(lastFrameTime / captureFrameTime);
			if (num > 0)
			{
				Graphics.Blit(source, tempRenderTexture);
				RenderTexture.active = tempRenderTexture;
				tempTexture2D.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
				RenderTexture.active = null;
			}
			for (int i = 0; i < num; i++)
			{
				if (frameNumber > maxFrames)
				{
					break;
				}
				frameQueue.Enqueue(tempTexture2D.GetRawTextureData());
				frameNumber++;
				if (frameNumber % frameRate == 0)
				{
					MonoBehaviour.print("Frame " + frameNumber);
				}
			}
			lastFrameTime = time;
		}
		else
		{
			terminateThreadWhenDone = true;
			base.enabled = false;
		}
		Graphics.Blit(source, destination);
	}

	private void EncodeAndSave()
	{
		MonoBehaviour.print("SCREENRECORDER IO THREAD STARTED");
		while (threadIsProcessing)
		{
			if (frameQueue.Count > 0)
			{
				using (FileStream fileStream = new FileStream(path + "/frame" + savingFrameNumber + ".bmp", FileMode.Create))
				{
					BitmapEncoder.WriteBitmap(fileStream, screenWidth, screenHeight, frameQueue.Dequeue());
					fileStream.Close();
				}
				savingFrameNumber++;
				MonoBehaviour.print("Saved " + savingFrameNumber + " frames. " + frameQueue.Count + " frames remaining.");
			}
			else
			{
				if (terminateThreadWhenDone)
				{
					break;
				}
				Thread.Sleep(1);
			}
		}
		terminateThreadWhenDone = false;
		threadIsProcessing = false;
		MonoBehaviour.print("SCREENRECORDER IO THREAD FINISHED");
	}
}
