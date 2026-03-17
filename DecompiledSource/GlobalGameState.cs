using UnityEngine;
using UnityEngine.SceneManagement;

public static class GlobalGameState
{
	public static bool resourcesLoaded;

	public static string saveFile;

	public static void GoToGame(string save_file)
	{
		Debug.Log("Load game scene");
		saveFile = save_file;
		SceneManager.LoadScene(3);
	}

	public static void GoToMainMenu()
	{
		Debug.Log("Load menu scene");
		SceneManager.LoadScene(2);
	}

	public static void GoToLoading()
	{
		Debug.Log("Load loading screen scene");
		SceneManager.LoadScene(1);
	}

	public static void GoToCredits()
	{
		Debug.Log("Go to credits");
		SceneManager.LoadScene(4);
	}

	public static void Quit()
	{
		Application.Quit();
	}
}
