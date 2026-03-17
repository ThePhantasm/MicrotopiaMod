using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public static class Instinct
{
	public static List<Task> tasks = new List<Task>();

	private static Dictionary<string, List<string>> dicInstinctOrders = new Dictionary<string, List<string>>();

	public static void Write(Save save)
	{
		List<Task> list = new List<Task>();
		foreach (Task task in tasks)
		{
			if (task.status != TaskStatus.NONE)
			{
				list.Add(task);
			}
		}
		save.Write(list.Count);
		foreach (Task item in list)
		{
			save.Write(item.code);
			save.Write((int)item.status);
		}
	}

	public static void Read(Save save)
	{
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string code = save.ReadString();
			TaskStatus taskStatus = (TaskStatus)save.ReadInt();
			Task task = Get(code);
			if (task != null)
			{
				switch (taskStatus)
				{
				case TaskStatus.COMPLETED:
					task.Achieve(during_load: true);
					task.SetStatus(TaskStatus.COMPLETED);
					break;
				case TaskStatus.CURRENT:
					task.SetStatus(TaskStatus.CURRENT);
					break;
				}
			}
		}
		foreach (string item in GetInstinctOrder())
		{
			Task task2 = Get(item);
			if (task2.status == TaskStatus.CURRENT)
			{
				task2.SetStatus(TaskStatus.NONE);
			}
		}
		SetFirstUncompletedInstinct();
	}

	public static bool Init()
	{
		XmlDocument xmlDoc = SheetReader.GetXmlDoc(Files.FodsInstinct());
		if (xmlDoc == null)
		{
			return false;
		}
		tasks.Clear();
		foreach (SheetRow item in SheetReader.ERead(xmlDoc, "Instinct"))
		{
			string text = item.GetString("Code");
			if (!SheetRow.Skip(text))
			{
				Task task = new Task();
				task.code = text;
				task.idea = item.GetBool("Idea");
				task.rewards = TaskReward.ParseList(item.GetString("Reward"));
				task.subTasks = SubTask.ParseList(item.GetString("Success"));
				tasks.Add(task);
			}
		}
		dicInstinctOrders.Clear();
		List<string> list = new List<string> { "REGULAR", "DEMO", "SKIP_INTRO" };
		foreach (string item2 in list)
		{
			dicInstinctOrders.Add(item2, new List<string>());
		}
		foreach (SheetRow item3 in SheetReader.ERead(xmlDoc, "Instinct_Order"))
		{
			foreach (string item4 in list)
			{
				string text2 = item3.GetString(item4);
				if (SheetRow.Skip(text2))
				{
					break;
				}
				dicInstinctOrders[item4].Add(text2);
			}
		}
		return true;
	}

	private static List<string> GetInstinctOrder()
	{
		if (DebugSettings.standard.demo)
		{
			return dicInstinctOrders["DEMO"];
		}
		if (WorldSettings.quickInstinct)
		{
			return dicInstinctOrders["SKIP_INTRO"];
		}
		return dicInstinctOrders["REGULAR"];
	}

	public static void Clear()
	{
		foreach (Task task in tasks)
		{
			task.SetStatus(TaskStatus.NONE);
		}
	}

	public static Task Get(string code)
	{
		foreach (Task task in tasks)
		{
			if (task.code == code)
			{
				return task;
			}
		}
		Debug.LogError("Couldn't find task with code " + code);
		return null;
	}

	public static List<Task> GetCurrentTasks()
	{
		List<Task> list = new List<Task>();
		foreach (Task task in tasks)
		{
			if (task.status == TaskStatus.CURRENT)
			{
				list.Add(task);
			}
		}
		return list;
	}

	public static void SetFirstUncompletedInstinct()
	{
		List<string> instinctOrder = GetInstinctOrder();
		for (int i = 0; i < instinctOrder.Count; i++)
		{
			Task task = Get(instinctOrder[i]);
			if (task.status != TaskStatus.COMPLETED)
			{
				task.SetStatus(TaskStatus.CURRENT);
				break;
			}
		}
	}

	public static int GetInstinctNumber(string code)
	{
		List<string> instinctOrder = GetInstinctOrder();
		for (int i = 0; i < instinctOrder.Count; i++)
		{
			if (code == instinctOrder[i])
			{
				return i;
			}
		}
		return -1;
	}
}
