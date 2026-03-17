using System;
using UnityEngine;

public class CommandLine
{
	public static bool overrideResolution = false;

	public static FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;

	public static int screenWidth = -1;

	public static int screenHeight = -1;

	public static void Process()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		Debug.Log("Command line arguments: " + string.Join(" ", commandLineArgs));
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string text = commandLineArgs[i].ToLower();
			if (i == 0 && text.Contains(".exe"))
			{
				continue;
			}
			int result = -1;
			if (i < commandLineArgs.Length - 1)
			{
				int.TryParse(commandLineArgs[i + 1], out result);
			}
			switch (text)
			{
			case "-w":
			case "-width":
			case "-screen-width":
				if (result > 0)
				{
					overrideResolution = true;
					screenWidth = result;
					i++;
				}
				break;
			case "-h":
			case "-height":
			case "-screen-height":
				if (result > 0)
				{
					overrideResolution = true;
					screenHeight = result;
					i++;
				}
				break;
			case "-screen-fullscreen":
				if (result >= 0)
				{
					overrideResolution = true;
					fullScreenMode = ((result != 0) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
					i++;
				}
				break;
			case "-window":
			case "-sw":
			case "-startwindowed":
			case "-windowed":
				overrideResolution = true;
				fullScreenMode = FullScreenMode.Windowed;
				break;
			case "-full":
			case "-fullscreen":
				overrideResolution = true;
				fullScreenMode = FullScreenMode.FullScreenWindow;
				break;
			case "-exclusive":
				overrideResolution = true;
				fullScreenMode = FullScreenMode.ExclusiveFullScreen;
				break;
			default:
				Debug.Log("Unknown command line arg '" + text + "'");
				break;
			}
		}
	}
}
