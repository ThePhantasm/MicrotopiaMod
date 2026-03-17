namespace HighlightPlus;

public static class QualityLevelExtensions
{
	public static bool UsesMultipleOffsets(this QualityLevel qualityLevel)
	{
		if (qualityLevel != QualityLevel.Medium)
		{
			return qualityLevel == QualityLevel.High;
		}
		return true;
	}
}
