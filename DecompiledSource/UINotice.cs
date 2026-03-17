public class UINotice : UIBase
{
	public Notice notice;

	public void UpdateNotice()
	{
		Show(!Player.HasSeenNotice(notice));
	}

	public bool ShouldNotice()
	{
		return !Player.HasSeenNotice(notice);
	}
}
