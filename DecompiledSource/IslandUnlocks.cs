using System.Collections.Generic;

public class IslandUnlocks
{
	public GeneralUnlocks generalUnlock;

	public List<string> unlockRecipes = new List<string>();

	public IslandUnlocks(GeneralUnlocks gu, List<string> urs)
	{
		generalUnlock = gu;
		unlockRecipes = urs;
	}
}
