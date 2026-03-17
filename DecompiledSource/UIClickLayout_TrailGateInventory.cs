using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIClickLayout_TrailGateInventory : UIClickLayout
{
	[SerializeField]
	private TextMeshProUGUI lbGateCarryCaste;

	[SerializeField]
	private UIButton btToggleGateNot;

	[SerializeField]
	private UITextImageButton btGateCarryCaste;

	[SerializeField]
	private GridLayoutGroup gridGateCarryCaste;

	private UIItemMenu uiItemMenu;

	private List<UIIconItem> spawnedGateItems = new List<UIIconItem>();

	public override void Clear()
	{
		base.Clear();
		if (uiItemMenu != null)
		{
			UnityEngine.Object.Destroy(uiItemMenu.gameObject);
		}
		uiItemMenu = null;
	}

	public void ShowNot(TrailType gate, bool not)
	{
		switch (gate)
		{
		case TrailType.GATE_CARRY:
			lbGateCarryCaste.text = (not ? Loc.GetUI("GATE_CARRY_NOT") : Loc.GetUI("GATE_CARRY"));
			break;
		case TrailType.GATE_CASTE:
			lbGateCarryCaste.text = (not ? Loc.GetUI("GATE_CASTE_NOT") : Loc.GetUI("GATE_CASTE"));
			break;
		}
	}

	public void SetGateCarry(TrailGate_Carry gate_carry, List<PickupType> _pickups, Action on_apply)
	{
		ShowNot(TrailType.GATE_CARRY, gate_carry.not);
		btToggleGateNot.Init(delegate
		{
			gate_carry.not = !gate_carry.not;
			ShowNot(TrailType.GATE_CARRY, gate_carry.not);
		});
		btGateCarryCaste.SetButton(delegate
		{
			if (uiItemMenu == null)
			{
				uiItemMenu = UIBaseSingleton.Get(UIItemMenu.instance);
			}
			uiItemMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiItemMenu.SetPosition(btGateCarryCaste.rtBase.transform.position);
			uiItemMenu.InitPickupTypes(_pickups, on_apply, PickupType.ANY);
			uiItemMenu.Show(target: true);
		});
		btGateCarryCaste.SetHoverText(Loc.GetUI("GATE_CARRY_CLICK"));
	}

	public void SetGateCaste(TrailGate_Caste gate_caste, List<AntCaste> _castes, Action on_apply)
	{
		lbGateCarryCaste.text = (gate_caste.not ? Loc.GetUI("GATE_CASTE_NOT") : Loc.GetUI("GATE_CASTE"));
		btToggleGateNot.Init(delegate
		{
			gate_caste.not = !gate_caste.not;
			lbGateCarryCaste.text = (gate_caste.not ? Loc.GetUI("GATE_CASTE_NOT") : Loc.GetUI("GATE_CASTE"));
		});
		btGateCarryCaste.SetButton(delegate
		{
			if (uiItemMenu == null)
			{
				uiItemMenu = UIBaseSingleton.Get(UIItemMenu.instance);
			}
			uiItemMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiItemMenu.SetPosition(btGateCarryCaste.rtBase.transform.position);
			uiItemMenu.InitAntCastes(_castes, on_apply);
			uiItemMenu.Show(target: true);
		});
		btGateCarryCaste.SetHoverText(Loc.GetUI("GATE_CASTE_CLICK"));
	}

	public void UpdateGate(List<AntCaste> castes)
	{
		UpdateGate(null, castes);
	}

	public void UpdateGate(List<PickupType> _pickups, List<AntCaste> _castes = null)
	{
		int num = 3;
		if (spawnedGateItems.Count < num)
		{
			for (int i = 0; i < num; i++)
			{
				UIIconItem component = UnityEngine.Object.Instantiate(AssetLinks.standard.GetPrefab(typeof(UIIconItem)), gridGateCarryCaste.transform).GetComponent<UIIconItem>();
				spawnedGateItems.Add(component);
			}
		}
		foreach (UIIconItem spawnedGateItem in spawnedGateItems)
		{
			spawnedGateItem.SetObActive(active: false);
		}
		int num2 = 0;
		int num3 = 0;
		if (_pickups != null)
		{
			num3 = _pickups.Count;
			num2 = Mathf.Min(_pickups.Count, num);
		}
		else if (_castes != null)
		{
			num3 = _castes.Count;
			num2 = Mathf.Min(_castes.Count, num);
		}
		for (int j = 0; j < num2 && (_pickups == null || (_pickups.Count >= j - 1 && (_castes == null || _castes.Count >= j - 1))); j++)
		{
			spawnedGateItems[j].SetObActive(active: true);
			if (num3 > num && j + 1 >= num)
			{
				spawnedGateItems[j].Init("...");
				break;
			}
			if (_pickups != null)
			{
				spawnedGateItems[j].Init(_pickups[j]);
			}
			else if (_castes != null)
			{
				spawnedGateItems[j].Init(_castes[j]);
			}
		}
		gridGateCarryCaste.constraintCount = num2;
	}
}
