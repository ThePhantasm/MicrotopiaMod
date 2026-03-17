using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public static class Sequence
{
	public static List<SequenceStep> tutorialSequence = new List<SequenceStep>();

	private static int tutorialStep = 0;

	public static int TUTORIAL_RESEARCH_COST = -1;

	public static float TUTORIAL_RESEARCH_TIME = -1f;

	public static string TUTORIAL_RESEARCH_UNLOCK = "";

	public static List<SequenceStep> events = new List<SequenceStep>();

	public static List<string> past_events = new List<string>();

	public static bool Init()
	{
		tutorialSequence.Clear();
		XmlDocument xmlDoc = SheetReader.GetXmlDoc(Files.FodsSequences());
		if (xmlDoc == null)
		{
			return false;
		}
		int num = 0;
		foreach (SheetRow item in SheetReader.ERead(xmlDoc, "Tutorial"))
		{
			string text = item.GetString("code");
			if (!SheetRow.Skip(text))
			{
				SequenceStep sequenceStep = new SequenceStep(num);
				sequenceStep.code = text;
				sequenceStep.text = item.GetString("Text");
				sequenceStep.sequenceActions = SequenceAction.ParseList(item.GetString("Actions"));
				sequenceStep.sequenceChecks = SequenceCheck.ParseList(item.GetString("Checks"));
				string text2 = item.GetString("Ground");
				if (!string.IsNullOrEmpty(text2) && Toolkit.CheckEnum(text2, typeof(GroundGroup), "TUTORIAL SEQUENCE"))
				{
					sequenceStep.sequenceGround = (GroundGroup)Enum.Parse(typeof(GroundGroup), text2.ToUpper());
				}
				tutorialSequence.Add(sequenceStep);
				num++;
			}
		}
		num = 0;
		foreach (SheetRow item2 in SheetReader.ERead(xmlDoc, "Events"))
		{
			string text3 = item2.GetString("code");
			if (!SheetRow.Skip(text3))
			{
				SequenceStep sequenceStep2 = new SequenceStep(num);
				sequenceStep2.code = text3;
				sequenceStep2.text = item2.GetString("Text");
				sequenceStep2.sequenceActions = SequenceAction.ParseList(item2.GetString("Actions"));
				sequenceStep2.sequenceChecks = SequenceCheck.ParseList(item2.GetString("Checks"));
				string text4 = item2.GetString("Ground");
				if (!string.IsNullOrEmpty(text4) && Toolkit.CheckEnum(text4, typeof(GroundGroup), "EVENT SEQUENCE"))
				{
					sequenceStep2.sequenceGround = (GroundGroup)Enum.Parse(typeof(GroundGroup), text4.ToUpper());
				}
				events.Add(sequenceStep2);
				num++;
			}
		}
		return true;
	}

	public static void SequenceUpdate()
	{
		TutorialUpdate();
		EventUpdate();
	}

	public static void TutorialStart()
	{
		if (Tutorial())
		{
			SetTutorialStep(0);
		}
	}

	public static void TutorialUpdate()
	{
		if (Tutorial() && tutorialSequence[tutorialStep].CheckSatisfied())
		{
			SetTutorialStep(tutorialStep + 1);
		}
	}

	public static bool Tutorial()
	{
		return false;
	}

	public static void SetTutorialStep(int step)
	{
		tutorialStep = step;
		if (step < tutorialSequence.Count)
		{
			tutorialSequence[step].DoActions();
		}
	}

	public static void SkipToTutorialStep(string _code)
	{
		int num = 1;
		for (int i = 0; i < num; i++)
		{
			if (tutorialSequence[tutorialStep].code == _code)
			{
				break;
			}
			SetTutorialStep(tutorialStep + 1);
			if (num < tutorialSequence.Count)
			{
				num++;
			}
			else
			{
				Debug.LogWarning("No tutorial step found with code " + _code);
			}
		}
	}

	public static GroundGroup GetTutorialGround()
	{
		if (Tutorial())
		{
			return tutorialSequence[tutorialStep].sequenceGround;
		}
		return GroundGroup.NONE;
	}

	public static bool PastTutorialStep(string _step)
	{
		if (!Tutorial())
		{
			return true;
		}
		for (int i = 0; i <= tutorialStep; i++)
		{
			if (tutorialSequence[i].code == _step)
			{
				return true;
			}
		}
		return false;
	}

	public static bool CheckTutorialCode(string s)
	{
		foreach (SequenceStep item in tutorialSequence)
		{
			if (item.code == s)
			{
				return true;
			}
		}
		Debug.LogWarning(s + " not recognized as tutorial step");
		return false;
	}

	public static void EventUpdate()
	{
		foreach (SequenceStep @event in events)
		{
			if (!past_events.Contains(@event.code) && @event.CheckSatisfied())
			{
				@event.DoActions();
				past_events.Add(@event.code);
			}
		}
	}
}
