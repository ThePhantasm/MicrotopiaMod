using UnityEngine;

public class UIClickLayout_TrailGateCounter : UIClickLayout
{
	[SerializeField]
	private UISliderExtra sliderCrewSize;

	private int crewSizePrev;

	public void SetCounter(TrailGate_Counter counter)
	{
		crewSizePrev = counter.crewSize;
		sliderCrewSize.Init(50, () => counter.crewSize, delegate(int value)
		{
			crewSizePrev = (counter.crewSize = value);
		});
	}

	public void UpdateCounter(TrailGate_Counter counter)
	{
		if (counter.crewSize != crewSizePrev)
		{
			sliderCrewSize.UpdateValue();
			crewSizePrev = counter.crewSize;
		}
	}
}
