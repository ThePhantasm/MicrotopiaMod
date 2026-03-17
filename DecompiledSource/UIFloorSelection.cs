using UnityEngine;

public class UIFloorSelection : UIBaseSingleton
{
	[SerializeField]
	private UIBuildingButton btRelocate;

	[SerializeField]
	private UIBuildingButton btBlueprint;

	[SerializeField]
	private UIBuildingButton btDuplicate;

	[SerializeField]
	private UIBuildingButton btDelete;

	public static UIFloorSelection instance;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void Init()
	{
		btRelocate.SetButton(OnClickRelocate);
		btRelocate.SetHotkey(InputManager.GetDesc(InputAction.Relocate));
		btRelocate.SetHoverLocUI("FLOORSELECT_HOVER_RELOCATE");
		btDelete.SetButton(OnClickDelete);
		btDelete.SetHotkey(InputManager.GetDesc(InputAction.Delete));
		btDelete.SetHoverLocUI("FLOORSELECT_HOVER_DEMOLISH");
		btBlueprint.SetButton(OnClickBlueprint);
		btBlueprint.SetHotkey(InputManager.GetDesc(InputAction.CopySettings));
		btBlueprint.Show(Progress.HasUnlocked(GeneralUnlocks.BLUEPRINTS));
		btBlueprint.SetHoverLocUI("FLOORSELECT_HOVER_BLUEPRINT");
		btDuplicate.SetButton(OnClickDuplicate);
		btDuplicate.SetHotkey(InputManager.GetDesc(InputAction.PasteSettings));
		btDuplicate.SetHoverLocUI("FLOORSELECT_HOVER_COPY");
	}

	public static bool IsActive()
	{
		if (instance != null)
		{
			return instance.IsShown();
		}
		return false;
	}

	public void UpdateFloor()
	{
		bool flag = Gameplay.instance.GetSelectedFloorTiles().Count > 0;
		if (flag != IsShown())
		{
			Show(flag);
		}
	}

	public void OnClickDelete()
	{
		FloorEditing.DeleteSelectedFloor();
	}

	public void OnClickRelocate()
	{
		FloorEditing.RelocateSelectedFloor();
	}

	public void OnClickBlueprint()
	{
		FloorEditing.CreateBlueprintOfSelectedFloor();
	}

	public void OnClickDuplicate()
	{
		FloorEditing.DuplicateSelectedFloor();
	}
}
