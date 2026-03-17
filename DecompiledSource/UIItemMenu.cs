using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIItemMenu : UIBaseSingleton
{
	public static UIItemMenu instance;

	[SerializeField]
	private GridLayoutGroup gridLayout;

	[SerializeField]
	private UIButtonText btClose;

	private List<UIIconItem> spawnedItems = new List<UIIconItem>();

	private Action onApply;

	protected override void SetInstance()
	{
		SetInstance(ref instance, this);
	}

	protected override void ClearInstance()
	{
		instance = null;
	}

	public void InitPickupTypes(List<PickupType> selected_pickups, Action on_apply, params PickupType[] _include)
	{
		InitPickupTypes(selected_pickups, new List<PickupState>
		{
			PickupState.DUST,
			PickupState.LIQUID,
			PickupState.SOLID,
			PickupState.LIVING
		}, on_apply, _include);
	}

	public void InitPickupTypes(List<PickupType> selected_pickups, List<PickupState> allowed_states, Action on_apply, params PickupType[] _include)
	{
		List<PickupType> seenPickups = Progress.GetSeenPickups(_include);
		List<PickupType> list = new List<PickupType>();
		foreach (PickupType item in seenPickups)
		{
			if (item == PickupType.ANY || item == PickupType.NONE)
			{
				list.Add(item);
			}
			else if (allowed_states.Contains(PickupData.Get(item).state))
			{
				list.Add(item);
			}
		}
		InitPickupTypes_final(selected_pickups, list, on_apply);
	}

	public void InitPickupTypes(List<PickupType> selected_pickups, List<PickupType> available_pickups, Action on_apply, params PickupType[] _include)
	{
		List<PickupType> seenPickups = Progress.GetSeenPickups(_include);
		List<PickupType> list = new List<PickupType>();
		foreach (PickupType available_pickup in available_pickups)
		{
			if (seenPickups.Contains(available_pickup))
			{
				list.Add(available_pickup);
			}
		}
		InitPickupTypes_final(selected_pickups, list, on_apply);
	}

	private void InitPickupTypes_final(List<PickupType> selected_pickups, List<PickupType> selectable_types, Action on_apply)
	{
		bool single_selectable = true;
		if (spawnedItems.Count < selectable_types.Count)
		{
			int num = selectable_types.Count - spawnedItems.Count;
			for (int i = 0; i < num; i++)
			{
				UIIconItem component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem))).GetComponent<UIIconItem>();
				component.rtBase.SetParent(gridLayout.transform, worldPositionStays: false);
				spawnedItems.Add(component);
			}
		}
		foreach (UIIconItem spawnedItem in spawnedItems)
		{
			spawnedItem.SetObActive(active: false);
		}
		for (int j = 0; j < selectable_types.Count; j++)
		{
			PickupType type = selectable_types[j];
			spawnedItems[j].Init(type);
			string hoverText = Loc.GetUI((type == PickupType.ANY) ? "BUILDING_ANY_MATERIAL" : "GENERIC_NONE");
			if (type != PickupType.NONE && type != PickupType.ANY)
			{
				hoverText = PickupData.Get(type).GetTitle();
			}
			spawnedItems[j].SetHoverText(hoverText);
			if (selected_pickups.Contains(type))
			{
				spawnedItems[j].AddOverlay(OverlayTypes.SELECTED);
				spawnedItems[j].SetButton(delegate
				{
					if (single_selectable)
					{
						selected_pickups.Clear();
						selected_pickups.Add(PickupType.NONE);
					}
					else
					{
						selected_pickups.Remove(type);
					}
					InitPickupTypes_final(selected_pickups, selectable_types, on_apply);
				});
			}
			else
			{
				spawnedItems[j].SetButton(delegate
				{
					if (single_selectable)
					{
						selected_pickups.Clear();
					}
					selected_pickups.Add(type);
					InitPickupTypes_final(selected_pickups, selectable_types, on_apply);
				});
			}
			spawnedItems[j].SetObActive(active: true);
		}
		gridLayout.constraintCount = Mathf.Min(selectable_types.Count, 8);
		onApply = delegate
		{
			on_apply();
			onApply = null;
		};
		btClose.Init(delegate
		{
			if (onApply != null)
			{
				onApply();
			}
			Show(target: false);
		}, Loc.GetUI("GENERIC_DONE"));
	}

	public void InitAntCastes(List<AntCaste> selected_antcastes, Action on_apply)
	{
		bool single_selectable = true;
		List<AntCaste> seenAntCastes = Progress.GetSeenAntCastes();
		if (spawnedItems.Count < seenAntCastes.Count)
		{
			int num = seenAntCastes.Count - spawnedItems.Count;
			for (int i = 0; i < num; i++)
			{
				UIIconItem component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem))).GetComponent<UIIconItem>();
				component.rtBase.SetParent(gridLayout.transform, worldPositionStays: false);
				spawnedItems.Add(component);
			}
		}
		foreach (UIIconItem spawnedItem in spawnedItems)
		{
			spawnedItem.SetObActive(active: false);
		}
		for (int j = 0; j < seenAntCastes.Count; j++)
		{
			AntCaste _caste = seenAntCastes[j];
			spawnedItems[j].Init(_caste);
			string hoverText = Loc.GetUI("GENERIC_NONE");
			if (_caste != AntCaste.NONE)
			{
				hoverText = AntCasteData.Get(_caste).GetTitle();
			}
			spawnedItems[j].SetHoverText(hoverText);
			if (selected_antcastes.Contains(_caste))
			{
				spawnedItems[j].AddOverlay(OverlayTypes.SELECTED);
				spawnedItems[j].SetButton(delegate
				{
					if (single_selectable)
					{
						selected_antcastes.Clear();
						selected_antcastes.Add(AntCaste.NONE);
					}
					else
					{
						selected_antcastes.Remove(_caste);
					}
					InitAntCastes(selected_antcastes, on_apply);
				});
			}
			else
			{
				spawnedItems[j].SetButton(delegate
				{
					if (single_selectable)
					{
						selected_antcastes.Clear();
					}
					selected_antcastes.Add(_caste);
					InitAntCastes(selected_antcastes, on_apply);
				});
			}
			spawnedItems[j].SetObActive(active: true);
		}
		gridLayout.constraintCount = Mathf.Min(seenAntCastes.Count, 8);
		onApply = delegate
		{
			on_apply();
			onApply = null;
		};
		btClose.Init(delegate
		{
			if (onApply != null)
			{
				onApply();
			}
			Show(target: false);
		}, Loc.GetUI("GENERIC_DONE"));
	}

	public void SetPosition(Vector2 ui_pos)
	{
		rtBase.position = ui_pos;
	}

	public override void Show(bool target)
	{
		if (!target && onApply != null && base.gameObject.activeSelf)
		{
			onApply();
		}
		base.Show(target);
	}

	public void DoApply()
	{
		if (onApply != null)
		{
			onApply();
		}
	}
}
