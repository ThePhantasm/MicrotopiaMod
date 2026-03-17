using System;

public class Playthrough
{
	public Guid guid { get; private set; }

	public DateTime dtStarted { get; private set; }

	public int gynesFlown { get; private set; }

	public Playthrough(bool debug)
	{
		guid = (debug ? Guid.Empty : Guid.NewGuid());
		dtStarted = DateTime.Now;
		gynesFlown = 0;
	}

	public Playthrough(Save save)
	{
		guid = save.ReadGuid();
		dtStarted = save.ReadDateTime();
		gynesFlown = save.ReadInt();
	}

	public void Write(Save save)
	{
		save.Write(guid);
		save.Write(dtStarted);
		save.Write(gynesFlown);
	}

	public bool UpdateGynesFlown(int flown)
	{
		if (flown > gynesFlown)
		{
			gynesFlown = flown;
			return true;
		}
		return false;
	}
}
