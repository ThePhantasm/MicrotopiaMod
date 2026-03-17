public class BlueprintTrail
{
	private Blueprint blueprint;

	public int splitIdStart;

	public int splitIdEnd;

	public TrailType trailType;

	private BlueprintData gateData;

	public BlueprintTrail(Blueprint _blueprint, int split_id_start, int split_id_end, TrailType trail_type)
	{
		blueprint = _blueprint;
		splitIdStart = split_id_start;
		splitIdEnd = split_id_end;
		trailType = trail_type;
		gateData = new BlueprintData(blueprint);
	}

	public BlueprintTrail(Blueprint _blueprint, Save from_save)
	{
		blueprint = _blueprint;
		Read(from_save);
	}

	public void StoreGateData(TrailGate trail_gate)
	{
		if (!(trail_gate == null))
		{
			gateData.Store(trail_gate);
		}
	}

	public void RetrieveData(Trail trail)
	{
		TrailGate trailGate = trail.trailGate;
		if (!(trailGate == null) && gateData != null && !gateData.IsEmpty())
		{
			gateData.Retrieve(trailGate);
		}
	}

	public void Write(Save save)
	{
		save.Write(splitIdStart);
		save.Write(splitIdEnd);
		save.Write((int)trailType);
		gateData.SaveToFile(save);
	}

	private void Read(Save save)
	{
		splitIdStart = save.ReadInt();
		splitIdEnd = save.ReadInt();
		trailType = (TrailType)save.ReadInt();
		gateData = new BlueprintData(blueprint);
		gateData.LoadFromFile(save);
	}
}
