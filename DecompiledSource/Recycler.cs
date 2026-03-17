using System.Collections.Generic;
using UnityEngine;

public class Recycler : Factory
{
	public bool deconstructor;

	protected override bool HasHologram()
	{
		return false;
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.RECYCLER;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		if (!(ui_click is UIClickLayout_Recycler uIClickLayout_Recycler))
		{
			return;
		}
		if (processTime == 0f)
		{
			if (deconstructor)
			{
				uIClickLayout_Recycler.SetInfo(Loc.GetUI("BUILDING_DECONSTRUCTOR_WAIT"));
			}
			else
			{
				uIClickLayout_Recycler.SetInfo(Loc.GetUI("BUILDING_RECYCLER_WAIT"));
			}
			uIClickLayout_Recycler.UpdateProgressBar(enabled: false, 0f, "");
			return;
		}
		if (deconstructor)
		{
			PickupType pickupType = PickupType.NONE;
			foreach (KeyValuePair<PickupType, int> dicCollectedPickup in GetDicCollectedPickups(include_incoming: false))
			{
				if (dicCollectedPickup.Value > 0)
				{
					pickupType = dicCollectedPickup.Key;
					break;
				}
			}
			if (pickupType != PickupType.NONE)
			{
				FactoryRecipeData factoryRecipeData = FactoryRecipeData.Get("DECONSTRUCT_" + pickupType);
				if (factoryRecipeData == null)
				{
					Debug.LogError("Couldn't find recipe for DECONSTRUCT_" + pickupType);
					return;
				}
				uIClickLayout_Recycler.SetInfo(Loc.GetUI("BUILDING_DECONSTRUCTOR_DOING"));
				float num = Mathf.Clamp(processTime, 0f, factoryRecipeData.processTime);
				string txt = (factoryRecipeData.processTime - num).Unit(PhysUnit.TIME_MINUTES);
				float progress = 1f - (factoryRecipeData.processTime - num) / factoryRecipeData.processTime;
				uIClickLayout_Recycler.UpdateProgressBar(enabled: true, progress, txt);
			}
			return;
		}
		AntCaste antCaste = AntCaste.NONE;
		foreach (Ant item in antsInside)
		{
			if (item != null)
			{
				antCaste = item.caste;
				break;
			}
		}
		FactoryRecipeData factoryRecipeData2 = FactoryRecipeData.Get("RECYCLE_" + antCaste);
		if (factoryRecipeData2 == null)
		{
			Debug.LogError("Couldn't find recipe for RECYCLE_" + antCaste);
			return;
		}
		uIClickLayout_Recycler.SetInfo(Loc.GetUI("BUILDING_RECYCLER_DOING"));
		float num2 = Mathf.Clamp(processTime, 0f, factoryRecipeData2.processTime);
		string txt2 = (factoryRecipeData2.processTime - num2).Unit(PhysUnit.TIME_MINUTES);
		float progress2 = 1f - (factoryRecipeData2.processTime - num2) / factoryRecipeData2.processTime;
		uIClickLayout_Recycler.UpdateProgressBar(enabled: true, progress2, txt2);
	}
}
