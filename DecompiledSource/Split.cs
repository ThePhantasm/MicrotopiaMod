using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Split : TrailPart
{
	private static List<Split> toDissolveSplits = new List<Split>();

	private static List<Split> toUpdateBillboards = new List<Split>();

	private static List<Split> changedSplits = new List<Split>();

	[SerializeField]
	private Renderer rend;

	[SerializeField]
	private GameObject pointer;

	public List<Trail> connectedTrails = new List<Trail>();

	private bool isInBuilding;

	private int dividerI;

	private List<Trail> dividerTrails = new List<Trail>();

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(isInBuilding);
		save.Write(dividerI);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		isInBuilding = save.ReadBool();
		dividerI = save.ReadInt();
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		ResetMaterial();
		UpdateDividerTrails(during_load: true);
		pointer.SetObActive(active: false);
		UpdateBillboardSoon();
	}

	public void AddTrail(Trail trail)
	{
		if (!connectedTrails.Contains(trail))
		{
			connectedTrails.Add(trail);
			ResetMaterial();
		}
		UpdateBillboardSoon();
	}

	public void RemoveTrailBasic(Trail trail)
	{
		if (connectedTrails.Contains(trail))
		{
			connectedTrails.Remove(trail);
		}
		UpdateBillboardSoon();
	}

	public void RemoveTrail(Trail trail)
	{
		RemoveTrailBasic(trail);
		if (TrailCount() == 0)
		{
			Delete();
		}
		else
		{
			ResetMaterial();
			UpdateDividerTrails();
			DissolveSplitIfPossible();
		}
		UpdateBillboardSoon();
	}

	public void DeleteButKeepTrails()
	{
		connectedTrails.Clear();
		Delete();
	}

	protected override void DoDelete()
	{
		if (base.deleted)
		{
			return;
		}
		bool flag = false;
		int num = 1000;
		while (!flag || num-- < 0)
		{
			flag = true;
			foreach (Trail connectedTrail in connectedTrails)
			{
				if (!connectedTrail.IsBuilding())
				{
					flag = false;
					connectedTrail.Delete();
					break;
				}
			}
		}
		if (num < 0)
		{
			Debug.LogError("Split.Delete error");
		}
		GameManager.instance.RemoveSplit(this);
		Object.Destroy(base.gameObject);
		base.DoDelete();
	}

	public int TrailCount(bool placed = false)
	{
		if (placed)
		{
			int num = 0;
			{
				foreach (Trail connectedTrail in connectedTrails)
				{
					if (connectedTrail.IsPlaced())
					{
						num++;
					}
				}
				return num;
			}
		}
		return connectedTrails.Count;
	}

	public override void DoHighlight(TrailStatus _status, bool include_trails_for_splits = true, bool also_building = false)
	{
		if (_status == TrailStatus.HOVERING || _status == TrailStatus.HOVERING_ERROR)
		{
			SetMaterial(_status);
		}
		else
		{
			ResetMaterial();
		}
		if (!include_trails_for_splits)
		{
			return;
		}
		foreach (Trail connectedTrail in connectedTrails)
		{
			connectedTrail.DoHighlight(_status, include_trails_for_splits, also_building);
		}
	}

	public override IEnumerable<Trail> ETrails(TrailType of_type)
	{
		foreach (Trail connectedTrail in connectedTrails)
		{
			if (of_type == TrailType.NONE || connectedTrail.trailType == of_type)
			{
				yield return connectedTrail;
			}
		}
	}

	public IEnumerable<Trail> EPlacedTrails(Trail exclude = null)
	{
		foreach (Trail connectedTrail in connectedTrails)
		{
			if (connectedTrail != exclude && connectedTrail.IsPlaced())
			{
				yield return connectedTrail;
			}
		}
	}

	public IEnumerable<Trail> ENonLogicTrails(Trail exclude = null)
	{
		foreach (Trail connectedTrail in connectedTrails)
		{
			if (connectedTrail != exclude && !connectedTrail.IsLogic() && connectedTrail.IsPlaced())
			{
				yield return connectedTrail;
			}
		}
	}

	public IEnumerable<(Trail, Split)> EOtherSplits()
	{
		foreach (Trail connectedTrail in connectedTrails)
		{
			yield return (connectedTrail, connectedTrail.GetOtherSplit(this));
		}
	}

	public void DissolveSplitIfPossible()
	{
		if (!toDissolveSplits.Contains(this))
		{
			toDissolveSplits.Add(this);
		}
	}

	public static void HandleDissolves()
	{
		if (toDissolveSplits.Count == 0)
		{
			return;
		}
		List<Split> list = new List<Split>(toDissolveSplits);
		toDissolveSplits.Clear();
		foreach (Split item in list)
		{
			if (item != null)
			{
				item.DoDissolveSplit();
			}
		}
	}

	public void DoDissolveSplit()
	{
		if (connectedTrails.Count != 2)
		{
			return;
		}
		Trail trail = connectedTrails[0];
		Trail trail2 = connectedTrails[1];
		if (trail.trailType != trail2.trailType || trail.splitEnd == trail2.splitEnd || trail.splitStart == trail2.splitStart || trail.IsLogic())
		{
			return;
		}
		Vector3 direction = trail.direction;
		Vector3 direction2 = trail2.direction;
		if (((direction.z == 0f && direction2.z == 0f) || Mathf.Abs(direction.x / direction.z - direction2.x / direction2.z) < 0.001f) && direction.x * direction2.x >= 0f)
		{
			Trail trail3 = connectedTrails[(!(trail.splitEnd == this)) ? 1 : 0];
			Trail trail4 = connectedTrails[(!(trail.splitStart == this)) ? 1 : 0];
			TrailGate trailGate = trail3.trailGate;
			if (trailGate == null && trail4.trailGate != null)
			{
				trailGate = trail4.trailGate;
			}
			TrailEditing.StartLostAntsCheck();
			Trail trail5 = GameManager.instance.NewTrail(trail3.trailType, trailGate);
			trail5.SetSplitStart(trail3.splitStart);
			trail5.SetSplitEnd(trail4.splitEnd);
			trail5.PlaceTrail(TrailStatus.PLACED, trail5.FindNearbyConnectables());
			Delete();
			TrailEditing.EndLostAntsCheck(trail5);
		}
	}

	public TrailType GetTrailType()
	{
		if (connectedTrails == null || connectedTrails.Count == 0)
		{
			return TrailType.HAULING;
		}
		return connectedTrails[0].trailType;
	}

	public override TrailType GetTrailPartTrailType(params TrailType[] _exclude)
	{
		if (_exclude == null)
		{
			return GetTrailType();
		}
		foreach (Trail connectedTrail in connectedTrails)
		{
			bool flag = true;
			for (int i = 0; i < _exclude.Length; i++)
			{
				if (connectedTrail.trailType == _exclude[i])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return connectedTrail.trailType;
			}
		}
		return TrailType.HAULING;
	}

	public override void ResetMaterial()
	{
		SetMaterial(GetTrailType());
	}

	public void SetInBuilding(bool target = true)
	{
		isInBuilding = target;
		ResetMaterial();
	}

	public bool IsInBuilding()
	{
		return isInBuilding;
	}

	public Building GetBuilding()
	{
		foreach (Trail connectedTrail in connectedTrails)
		{
			if (connectedTrail.building != null)
			{
				return connectedTrail.building;
			}
		}
		return null;
	}

	public bool DividerChoose(out Trail next_trail, bool update_count)
	{
		if (dividerTrails.Count == 0)
		{
			next_trail = null;
			return false;
		}
		next_trail = dividerTrails[dividerI];
		if (update_count)
		{
			for (int i = 0; i < dividerTrails.Count; i++)
			{
				dividerI++;
				if (dividerI >= dividerTrails.Count)
				{
					dividerI = 0;
				}
				if (!dividerTrails[dividerI].IsGate())
				{
					break;
				}
			}
			UpdatePointer(during_load: false);
		}
		return true;
	}

	public void UpdateDividerTrails(bool during_load = false)
	{
		dividerTrails.Clear();
		bool flag = true;
		foreach (Trail connectedTrail in connectedTrails)
		{
			if (!(connectedTrail.splitStart != this))
			{
				dividerTrails.Add(connectedTrail);
				if (connectedTrail.trailType == TrailType.DIVIDER)
				{
					flag = false;
				}
			}
		}
		if (flag)
		{
			dividerTrails.Clear();
		}
		if (dividerTrails.Count > 0)
		{
			Vector3 first_trail_direction = dividerTrails[0].direction;
			dividerTrails.Sort((Trail a, Trail b) => CalculateClockAngle(first_trail_direction, a.direction).CompareTo(CalculateClockAngle(first_trail_direction, b.direction)));
		}
		if (!during_load && dividerI >= dividerTrails.Count)
		{
			dividerI = 0;
		}
		UpdatePointer(during_load);
	}

	private float CalculateClockAngle(Vector3 dir1, Vector3 dir2)
	{
		float num = Vector3.Angle(dir1, dir2);
		if (Vector3.Cross(dir1, dir2).y >= 0f)
		{
			return num;
		}
		return 360f - num;
	}

	private void UpdatePointer(bool during_load)
	{
		if (dividerTrails.Count == 0)
		{
			pointer.SetObActive(active: false);
		}
		else if (dividerI < dividerTrails.Count)
		{
			Quaternion quaternion = Quaternion.LookRotation(dividerTrails[dividerI].direction, Vector3.up);
			if (pointer.SetObActive(active: true) || during_load)
			{
				pointer.transform.rotation = quaternion;
			}
			else
			{
				GameManager.instance.RotatePointer(pointer, quaternion);
			}
		}
	}

	private IEnumerator CShowDividerOrder(List<Trail> _trails)
	{
		for (int i = 0; i < _trails.Count; i++)
		{
			for (float t = 0f; t < 0.5f; t += Time.deltaTime)
			{
				Debug.DrawLine(_trails[i].posStart.TargetYPosition(1f), _trails[i].posEnd.TargetYPosition(1f), Color.red);
				yield return null;
			}
		}
	}

	public override void SetMaterial(TrailType _type, bool lit_up = true)
	{
		if (_type == TrailType.COMMAND || isInBuilding)
		{
			if (rend != null)
			{
				rend.SetObActive(active: false);
			}
			return;
		}
		if (rend != null)
		{
			rend.SetObActive(active: true);
		}
		base.SetMaterial(_type, lit_up);
	}

	public override void SetMaterial(Material mat)
	{
		if (rend != null)
		{
			rend.sharedMaterial = MaterialLibrary.GetTrailMaterial(rend.sharedMaterial, -10, mat.GetColor("_Color"), mat.GetColor("_EmissionColor"));
		}
	}

	public void UpdateBillboardSoon()
	{
		if (!toUpdateBillboards.Contains(this))
		{
			toUpdateBillboards.Add(this);
		}
	}

	public static void UpdateBillboards()
	{
		foreach (Split toUpdateBillboard in toUpdateBillboards)
		{
			if (toUpdateBillboard != null)
			{
				toUpdateBillboard.UpdateBillboard();
			}
		}
		toUpdateBillboards.Clear();
	}

	public override BillboardType GetCurrentBillboard(out string code_desc, out string txt_onBillboard, out Color col, out Transform parent)
	{
		parent = null;
		if (TrailCount() > 1)
		{
			int num = 0;
			int num2 = 0;
			foreach (Trail connectedTrail in connectedTrails)
			{
				if (connectedTrail.trailType != TrailType.COMMAND && connectedTrail.IsPlaced())
				{
					if (connectedTrail.splitStart == this)
					{
						num++;
					}
					if (connectedTrail.splitEnd == this)
					{
						num2++;
					}
				}
			}
			if (num == 0 && num2 > 1)
			{
				code_desc = "WARNING_OPPOSITETRAILS";
				txt_onBillboard = "";
				col = Color.red;
				return BillboardType.CROSS_SMALL;
			}
		}
		if (billboardListener != null)
		{
			billboardListener.GetCurrentBillboard(out var code_desc2, out var _, out var col2, out var parent2);
			if (code_desc2 == "BUILDING_FACTORYNEEDSCONNECTION")
			{
				code_desc = code_desc2;
				txt_onBillboard = "";
				col = col2;
				parent = parent2;
				return BillboardType.ARROW;
			}
		}
		return base.GetCurrentBillboard(out code_desc, out txt_onBillboard, out col, out parent);
	}

	public static void AddChangedSplit(Split split)
	{
		if (!changedSplits.Contains(split))
		{
			changedSplits.Add(split);
		}
	}

	public static void UpdateCounterGates()
	{
		List<Trail> trailsToCheck = TrailGate_Counter.trailsToCheck;
		trailsToCheck.Clear();
		if (changedSplits.Count <= 0)
		{
			return;
		}
		foreach (Split changedSplit in changedSplits)
		{
			foreach (Trail connectedTrail in changedSplit.connectedTrails)
			{
				if (!trailsToCheck.Contains(connectedTrail))
				{
					trailsToCheck.Add(connectedTrail);
				}
			}
		}
		changedSplits.Clear();
	}

	public bool HasTrailWithLinkId()
	{
		if (connectedTrails.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < connectedTrails.Count; i++)
		{
			if (connectedTrails[i].linkId != 0)
			{
				return true;
			}
		}
		return false;
	}
}
