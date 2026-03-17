using System.Collections.Generic;

public class SequenceStep
{
	public int step;

	public string code;

	public string text;

	public List<SequenceAction> sequenceActions;

	public List<SequenceCheck> sequenceChecks;

	public GroundGroup sequenceGround;

	public SequenceStep(int i)
	{
		step = i;
		sequenceActions = new List<SequenceAction>();
		sequenceChecks = new List<SequenceCheck>();
	}

	public void DoActions()
	{
		foreach (SequenceAction sequenceAction in sequenceActions)
		{
			sequenceAction.PerformAction();
		}
	}

	public bool CheckSatisfied()
	{
		bool result = true;
		foreach (SequenceCheck sequenceCheck in sequenceChecks)
		{
			if (!sequenceCheck.SequenceCheckSatisfied())
			{
				result = false;
			}
		}
		return result;
	}
}
