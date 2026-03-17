using System.Collections.Generic;
using UnityEngine;

public abstract class TrailPart : ClickableObject
{
	public static List<TrailPart> HIGHLIGHTED_TRAILPARTS = new List<TrailPart>();

	private List<Trail> commandTrailLinks;

	public virtual void DoHighlight(TrailStatus _status, bool include_trails_for_splits = true, bool also_building = false)
	{
	}

	public static void HighLight()
	{
		foreach (TrailPart hIGHLIGHTED_TRAILPART in HIGHLIGHTED_TRAILPARTS)
		{
			if (hIGHLIGHTED_TRAILPART != null)
			{
				hIGHLIGHTED_TRAILPART.DoHighlight(TrailStatus.NONE);
			}
		}
		HIGHLIGHTED_TRAILPARTS.Clear();
	}

	public static void HighLight(List<TrailPart> _parts, TrailStatus _status, bool include_trails_for_splits = true)
	{
		foreach (TrailPart hIGHLIGHTED_TRAILPART in HIGHLIGHTED_TRAILPARTS)
		{
			if (hIGHLIGHTED_TRAILPART != null && !_parts.Contains(hIGHLIGHTED_TRAILPART))
			{
				hIGHLIGHTED_TRAILPART.DoHighlight(TrailStatus.NONE, include_trails_for_splits);
			}
		}
		foreach (TrailPart _part in _parts)
		{
			_part.DoHighlight(_status);
		}
		HIGHLIGHTED_TRAILPARTS.Clear();
		HIGHLIGHTED_TRAILPARTS.AddRange(_parts);
	}

	public static void HighLight(ICollection<Trail> _trails, TrailStatus _status, bool also_building = false)
	{
		foreach (TrailPart hIGHLIGHTED_TRAILPART in HIGHLIGHTED_TRAILPARTS)
		{
			if (!(hIGHLIGHTED_TRAILPART == null))
			{
				Trail trail = hIGHLIGHTED_TRAILPART as Trail;
				if (trail == null || !_trails.Contains(trail))
				{
					hIGHLIGHTED_TRAILPART.DoHighlight(TrailStatus.NONE, include_trails_for_splits: true, also_building);
				}
			}
		}
		foreach (Trail _trail in _trails)
		{
			_trail.DoHighlight(_status, include_trails_for_splits: true, also_building);
		}
		HIGHLIGHTED_TRAILPARTS.Clear();
		HIGHLIGHTED_TRAILPARTS.AddRange(_trails);
	}

	public override bool IsClickable()
	{
		return false;
	}

	public abstract IEnumerable<Trail> ETrails(TrailType of_type);

	public abstract void ResetMaterial();

	public abstract TrailType GetTrailPartTrailType(params TrailType[] _exclude);

	public virtual void SetMaterial(TrailStatus _status, bool also_building = false)
	{
		Material trailMaterial = AssetLinks.standard.GetTrailMaterial(_status);
		SetMaterial(trailMaterial);
	}

	public virtual void SetMaterial(TrailType _type, bool lit_up = true)
	{
		Material trailMaterial = AssetLinks.standard.GetTrailMaterial(_type, lit_up);
		SetMaterial(trailMaterial);
	}

	public virtual void SetMaterial(Material mat)
	{
	}

	public void AddCommandTrailLink(Trail trail)
	{
		if (commandTrailLinks == null)
		{
			commandTrailLinks = new List<Trail>();
		}
		commandTrailLinks.Add(trail);
	}

	public IEnumerable<Trail> ECommandTrailLinks()
	{
		if (commandTrailLinks == null)
		{
			yield break;
		}
		for (int num = commandTrailLinks.Count - 1; num >= 0; num--)
		{
			if (commandTrailLinks[num] == null)
			{
				commandTrailLinks.RemoveAt(num);
			}
		}
		if (commandTrailLinks.Count == 0)
		{
			commandTrailLinks = null;
			yield break;
		}
		foreach (Trail commandTrailLink in commandTrailLinks)
		{
			yield return commandTrailLink;
		}
	}
}
