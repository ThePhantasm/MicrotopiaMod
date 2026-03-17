using System.Collections.Generic;

public static class Filters
{
	private static int filtersSelected;

	private static int filtersActive;

	public static void Init()
	{
		Reset();
	}

	private static void Reset()
	{
		for (Filter filter = Filter.FLOATING_TRAILS; filter < Filter._MAX; filter++)
		{
			Select(filter, selected: false);
		}
	}

	public static void Read(Save save)
	{
		if (save.version == 48)
		{
			save.ReadBool();
			save.ReadBool();
		}
		else if (save.version >= 49)
		{
			Reset();
		}
		else
		{
			Reset();
		}
	}

	public static void Write(Save save)
	{
	}

	private static bool IsSet(int bits, Filter filter)
	{
		return (bits & (1 << (int)(filter - 1))) != 0;
	}

	private static void Set(ref int bits, Filter filter, bool active)
	{
		if (active)
		{
			bits |= 1 << (int)(filter - 1);
		}
		else
		{
			bits &= ~(1 << (int)(filter - 1));
		}
	}

	private static bool IsSelected(Filter filter)
	{
		return IsSet(filtersSelected, filter);
	}

	public static bool IsActive(Filter filter)
	{
		return IsSet(filtersActive, filter);
	}

	public static void OnlySelect(Filter filter, bool selected)
	{
		for (Filter filter2 = Filter.FLOATING_TRAILS; filter2 < Filter._MAX; filter2++)
		{
			Select(filter2, filter2 == filter && selected);
		}
	}

	public static void CheckInput()
	{
		if (InputManager.filterHideUI)
		{
			Toggle(Filter.HIDE_UI);
		}
		if (DebugSettings.standard.enableFilterHotKeys)
		{
			if (InputManager.filterFloatingTrails)
			{
				Toggle(Filter.FLOATING_TRAILS);
			}
			if (InputManager.filterHideTrails)
			{
				Toggle(Filter.HIDE_TRAILS);
			}
			if (InputManager.filterHideAnts)
			{
				Toggle(Filter.HIDE_ANTS);
			}
		}
	}

	private static void Toggle(Filter filter)
	{
		OnlySelect(filter, !IsSelected(filter));
	}

	public static void Select(Filter filter, bool selected)
	{
		if (IsSelected(filter) != selected)
		{
			Set(ref filtersSelected, filter, selected);
			Update(filter);
		}
	}

	public static void Update(Filter filter)
	{
		bool flag = IsSelected(filter);
		switch (filter)
		{
		case Filter.HIDE_UI:
			if (GameManager.instance != null && (GameManager.instance.IsMenuOpen() || (UITutorial.instance != null && UITutorial.instance.IsShown())))
			{
				flag = false;
			}
			Set(ref filtersActive, filter, flag);
			if (UIGlobal.instance != null)
			{
				UIGlobal.instance.canvas.enabled = !flag;
			}
			break;
		case Filter.FLOATING_TRAILS:
			if (GameManager.instance != null && GameManager.instance.mapMode)
			{
				flag = false;
			}
			Set(ref filtersActive, filter, flag);
			if (CamController.instance != null)
			{
				CamController.instance.SetTrailOverlay(flag);
			}
			break;
		case Filter.HIDE_TRAILS:
		case Filter.HIDE_ANTS:
			Set(ref filtersActive, filter, flag);
			if (CamController.instance != null)
			{
				CamController.instance.UpdateCamCulling();
			}
			break;
		}
	}

	public static IEnumerable<Filter> EOptions()
	{
		yield return Filter.NONE;
		yield return Filter.FLOATING_TRAILS;
		yield return Filter.HIDE_TRAILS;
		yield return Filter.HIDE_ANTS;
		yield return Filter.HIDE_UI;
	}
}
