using System.Collections.Generic;
using UnityEngine;

public class TrailGate_Sensors : TrailGate
{
	private List<TrailGateSensor> sensors = new List<TrailGateSensor>();

	public override TrailType GetTrailType()
	{
		return TrailType.GATE_SENSORS;
	}

	public override void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default)
	{
		TrailGate_Sensors obj = other as TrailGate_Sensors;
		sensors.Clear();
		foreach (TrailGateSensor sensor in obj.sensors)
		{
			sensors.Add(new TrailGateSensor(sensor));
		}
	}

	public override void Write(Save save)
	{
		save.Write(sensors.Count);
		foreach (TrailGateSensor sensor in sensors)
		{
			sensor.Write(save);
		}
	}

	public override void Read(Save save)
	{
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			sensors.Add(new TrailGateSensor(save));
		}
	}

	public override void WriteConfig(ISaveContainer save)
	{
		Debug.LogError("WriteConfig not set for TrailGate_Sensors");
	}

	public override void ReadConfig(ISaveContainer save)
	{
		Debug.LogError("ReadConfig not set for TrailGate_Sensors");
	}

	public override void Init(bool during_load = false)
	{
		if (!during_load)
		{
			TrailGateSensor item = new TrailGateSensor(SensorType.IS_CARRYING_PICKUP);
			sensors.Add(item);
		}
	}

	public override bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		if (_ant == null)
		{
			return true;
		}
		bool result = true;
		foreach (TrailGateSensor sensor in sensors)
		{
			if (!sensor.IsSatisfied(_ant, final))
			{
				result = false;
				break;
			}
		}
		return result;
	}

	public override void UpdateVisual(float dt)
	{
	}

	public override void SetHoverUI(UIHoverClickOb ui_hover)
	{
		ui_hover.SetTitle(Loc.GetObject("TRAIL_GATE"));
		ui_hover.SetSensors(sensors);
	}

	public override void UpdateHoverUI(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI(ui_hover);
		ui_hover.UpdateSensors();
	}
}
