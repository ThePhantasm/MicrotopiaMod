using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIClickLayout_Gatherer : UIClickLayout_Building
{
	[Header("Gatherer")]
	[SerializeField]
	private Slider slRadius;

	[SerializeField]
	private TMP_Text lbRadius;

	private UIItemMenu uiItemMenu;

	private List<PickupType> selectedPickups;

	private Gatherer gatherer;

	public void SetupForGatherer(Gatherer _gatherer)
	{
		gatherer = _gatherer;
		SetButton(UIClickButtonType.Generic1, gatherer.ChangeGatherType, InputAction.None);
		SetButton(UIClickButtonType.Generic2, delegate
		{
			selectedPickups = new List<PickupType> { gatherer.curFilter };
			if (selectedPickups[0] == PickupType.ANY)
			{
				selectedPickups[0] = PickupType.NONE;
			}
			if (uiItemMenu == null)
			{
				uiItemMenu = UIBaseSingleton.Get(UIItemMenu.instance);
			}
			uiItemMenu.transform.SetParent(base.transform, worldPositionStays: false);
			uiItemMenu.SetPosition(GetButton(UIClickButtonType.Generic2).btButton_better.rtBase.transform.position);
			uiItemMenu.InitPickupTypes(selectedPickups, gatherer.GetPossiblePickups(), delegate
			{
				gatherer.ChangeFilter((selectedPickups == null || selectedPickups.Count == 0 || selectedPickups[0] == PickupType.NONE) ? PickupType.ANY : selectedPickups[0]);
			}, default(PickupType));
			uiItemMenu.Show(target: true);
		}, InputAction.None);
		slRadius.onValueChanged.RemoveAllListeners();
		slRadius.onValueChanged.AddListener(delegate(float v)
		{
			gatherer.SetSearchRadius01(v);
		});
	}

	public void ChangeGatherType(TrailType tt)
	{
		UITextImageButton btButton_better = GetButton(UIClickButtonType.Generic1).btButton_better;
		Color col;
		Sprite trailIcon = AssetLinks.standard.GetTrailIcon(tt, out col);
		btButton_better.SetImage(trailIcon);
		btButton_better.SetImageColor(col);
		string code = ((tt == TrailType.PLANT_CUTTING) ? "TRAIL_PLANTCUTTING" : $"TRAIL_{tt}");
		btButton_better.SetHoverText(Loc.GetObject(code));
	}

	public void ChangeFilter(PickupType pt)
	{
		UITextImageButton btButton_better = GetButton(UIClickButtonType.Generic2).btButton_better;
		if (pt == PickupType.NONE || pt == PickupType.ANY)
		{
			btButton_better.SetImage(null);
			btButton_better.SetHoverLocUI("BUILDING_ANY_MATERIAL");
			btButton_better.SetText("?");
		}
		else
		{
			btButton_better.SetImage(AssetLinks.standard.GetPickupThumbnail(pt));
			btButton_better.SetHoverLocObjects(PickupData.Get(pt).title);
			btButton_better.SetText("");
		}
	}

	public void ChangeRadius01(float r)
	{
		slRadius.SetValueWithoutNotify(r);
		lbRadius.text = Loc.GetUI((r == 1f) ? "GATHERER_RANGE_ISLAND" : "GATHERER_RANGE_RADIUS");
	}

	public override void Clear()
	{
		base.Clear();
		if (uiItemMenu != null)
		{
			uiItemMenu.DoApply();
			Object.Destroy(uiItemMenu.gameObject);
		}
		uiItemMenu = null;
	}
}
