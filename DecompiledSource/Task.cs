using System.Collections.Generic;

public class Task
{
	public TaskStatus status;

	public string code;

	public bool idea;

	public List<SubTask> subTasks;

	public List<TaskReward> rewards;

	public Task()
	{
		status = TaskStatus.NONE;
	}

	public bool IsSatisfied(bool recalc = false)
	{
		bool result = true;
		foreach (SubTask subTask in subTasks)
		{
			if (recalc)
			{
				subTask.RecalcValues();
			}
			if (subTask.valueCurrent < subTask.valueRequired && !DebugSettings.standard.InstinctAlwaysSatisfied())
			{
				result = false;
			}
		}
		return result;
	}

	public void Achieve(bool during_load = false)
	{
		foreach (TaskReward reward in rewards)
		{
			reward.Give(during_load);
		}
		if (idea)
		{
			Tech.Get(code).Unlock(during_load: false);
		}
	}

	public void SetStatus(TaskStatus _status)
	{
		status = _status;
	}

	public string GetStory()
	{
		string text = code + "_STORY";
		return Loc.GetInstinct(text, GetVars(text));
	}

	public string GetShort()
	{
		string text = code + "_SHORT";
		return Loc.GetInstinct(text, GetVars(text));
	}

	public string GetTip()
	{
		string text = code + "_TIP";
		return Loc.GetInstinct(text, GetVars(text));
	}

	public string[] GetVars(string code)
	{
		if (code == "INSTINCT1_0_SHORT")
		{
			return new string[4]
			{
				InputManager.GetDesc(InputAction.CamUp),
				InputManager.GetDesc(InputAction.CamLeft),
				InputManager.GetDesc(InputAction.CamDown),
				InputManager.GetDesc(InputAction.CamRight)
			};
		}
		return new string[0];
	}
}
