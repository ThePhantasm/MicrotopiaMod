using System.Collections.Generic;
using UnityEngine;

public class Bridge : Building
{
	[Header("Bridge")]
	[SerializeField]
	protected Transform connectPoint;

	[SerializeField]
	protected GameObject startSection;

	[SerializeField]
	protected GameObject endSection;

	[SerializeField]
	protected GameObject pieceSection;

	[SerializeField]
	protected GameObject[] startEnd;

	[SerializeField]
	protected float pieceLength = 20f;

	[SerializeField]
	protected int maxLength = 12;

	private bool placingOtherEnd;

	private List<GameObject> spawnedPieces = new List<GameObject>();

	private GameObject otherEndHover;

	private int nPieces;

	private Vector3 midPos;

	private Ground otherGround;

	public override void Init(bool during_load = false)
	{
		endSection.SetObActive(active: false);
		pieceSection.SetObActive(active: false);
		if (during_load)
		{
			Recreate();
		}
		base.Init(during_load);
	}

	private void Recreate()
	{
		CreateBridge();
		UpdateTopPoint();
		otherGround = GetGroundOtherEnd();
	}

	public override void Write(Save save)
	{
		base.Write(save);
		WriteConfig(save);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		ReadConfig(save);
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		save.Write(nPieces);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		nPieces = save.ReadInt();
		if (save.GetSaveType() == SaveType.Blueprint)
		{
			Recreate();
		}
	}

	protected override void SetHoverBottomButtons_old(UIHoverClickOb ui_hover)
	{
		ui_hover.SetBottomButtons(OnClickDelete, null, null);
	}

	public void SetPlacingOtherEnd(bool placing_other_end)
	{
		placingOtherEnd = placing_other_end;
		if (placingOtherEnd)
		{
			otherEndHover = hoverMesh.AddMesh(endSection);
			otherEndHover.SetObActive(active: true);
			return;
		}
		if (otherEndHover != null)
		{
			Object.Destroy(otherEndHover);
			otherEndHover = null;
		}
		foreach (GameObject spawnedPiece in spawnedPieces)
		{
			Object.Destroy(spawnedPiece);
		}
		spawnedPieces.Clear();
	}

	private Vector3 GetLocalDir()
	{
		return connectPoint.localPosition.SetY(0f).normalized;
	}

	private Vector3 GetDir()
	{
		return base.transform.TransformDirection(connectPoint.localPosition).SetY(0f).normalized;
	}

	public bool UpdateOtherEnd(Vector3 mouse_pos)
	{
		float magnitude = (mouse_pos - connectPoint.position.SetY(0f)).magnitude;
		nPieces = Mathf.FloorToInt(magnitude / pieceLength);
		nPieces = Mathf.Clamp(nPieces, 0, maxLength);
		for (int i = spawnedPieces.Count; i < nPieces; i++)
		{
			GameObject gameObject = hoverMesh.AddMesh(pieceSection);
			gameObject.SetObActive(active: true);
			spawnedPieces.Add(gameObject);
		}
		for (int num = spawnedPieces.Count - 1; num >= nPieces; num--)
		{
			Object.Destroy(spawnedPieces[num]);
			spawnedPieces.RemoveAt(num);
		}
		Vector3 localDir = GetLocalDir();
		for (int j = 0; j < nPieces; j++)
		{
			spawnedPieces[j].transform.localPosition = localDir * (pieceLength * (float)j);
		}
		otherEndHover.transform.localPosition = localDir * (pieceLength * (float)nPieces);
		otherGround = GetGroundOtherEnd();
		return otherGround != null;
	}

	private Ground GetGroundOtherEnd()
	{
		return Toolkit.GetGround(base.transform.position + GetDir() * ((float)nPieces * pieceLength + connectPoint.localPosition.SetY(0f).magnitude * 3f));
	}

	public override void PlaceBuilding()
	{
		SetPlacingOtherEnd(placing_other_end: false);
		CreateBridge();
		UpdateTopPoint();
		base.PlaceBuilding();
	}

	private void UpdateTopPoint()
	{
		topPoint.transform.position = midPos.TargetYPosition(topPoint.transform.position.y);
	}

	private void CreateBridge()
	{
		endSection.SetObActive(active: true);
		for (int i = 0; i < nPieces; i++)
		{
			GameObject gameObject = Object.Instantiate(pieceSection, base.transform);
			gameObject.SetObActive(active: true);
			spawnedPieces.Add(gameObject);
		}
		Vector3 localDir = GetLocalDir();
		for (int j = 0; j < nPieces; j++)
		{
			spawnedPieces[j].transform.localPosition = localDir * (pieceLength * (float)j);
		}
		endSection.transform.localPosition = localDir * (pieceLength * (float)nPieces);
		costs = new List<PickupCost>();
		foreach (PickupCost baseCost in data.baseCosts)
		{
			PickupCost pickupCost = new PickupCost(baseCost);
			pickupCost.intValue *= nPieces + 2;
			costs.Add(pickupCost);
		}
		midPos = connectPoint.position + GetDir() * ((float)nPieces * pieceLength * 0.5f);
	}

	protected override void PlaceBuildingTrails()
	{
		List<BuildingTrail> list = GetBuildingTrails();
		List<Trail> list2 = new List<Trail>();
		Vector3 dir = GetDir();
		bool flag = false;
		foreach (BuildingTrail item in list)
		{
			if (item.splitPoints.Count < 2 || item.splitProperties.Count > 0)
			{
				Debug.LogError("Bridge needs building trails of at least two points (from entrance point to top of starting piece) and no split properties");
				continue;
			}
			Split split = null;
			int count = item.splitPoints.Count;
			for (int i = 0; i < count * 2; i++)
			{
				Vector3 position;
				bool flag2;
				if (i < count)
				{
					position = item.splitPoints[i].position;
					flag2 = flag;
				}
				else
				{
					position = item.splitPoints[count * 2 - 1 - i].position;
					flag2 = !flag;
				}
				if (flag2)
				{
					position += dir * ((midPos - position).SetY(0f).magnitude * 2f);
				}
				if (i == 0)
				{
					split = GameManager.instance.NewSplit(position);
				}
				else
				{
					Trail trail = GameManager.instance.NewTrail_Building((i == 1) ? TrailType.IN_BUILDING_GATE : TrailType.IN_BUILDING, item.invisible);
					trail.SetSplitStart(split);
					split = trail.NewEndSplit(position);
					trail.SetBuilding(this);
					trail.PlaceTrail(TrailStatus.PLACED_IN_BUILDING);
					list2.Add(trail);
				}
				if (i > 0 && i < count * 2 - 1)
				{
					split.SetInBuilding();
				}
			}
			flag = !flag;
		}
		listSpawnedTrails.Add(list2);
	}

	protected override void DropPickupOnDemolish(PickupType pickup_type)
	{
		Pickup pickup = GameManager.instance.SpawnPickup(pickup_type);
		bool flag = Random.value < 0.5f;
		Vector3 vector = (flag ? base.transform.position : endSection.transform.position);
		pickup.transform.SetPositionAndRotation(vector + new Vector3(Random.insideUnitCircle.x * GetRadius(), 0f, Random.insideUnitCircle.y * GetRadius()), Toolkit.RandomYRotation());
		pickup.SetStatus(PickupStatus.ON_GROUND);
		if (!GameManager.instance.TryExchangePickupToInventory(flag ? ground : otherGround, vector, pickup))
		{
			flag = !flag;
			vector = (flag ? base.transform.position : endSection.transform.position);
			GameManager.instance.TryExchangePickupToInventory(flag ? ground : otherGround, vector, pickup);
		}
		if (!Player.crossIslandBuilding)
		{
			return;
		}
		foreach (Ground item in GameManager.instance.EGrounds())
		{
			if ((!flag || !(item == ground)) && !(item == otherGround))
			{
				GameManager.instance.TryExchangePickupToInventory(item, base.transform.position, pickup);
			}
		}
	}

	public override Vector3 GetInsertPos(Pickup pickup = null)
	{
		return midPos;
	}

	public Ground GetOtherGround()
	{
		return otherGround;
	}

	protected override void SetBuildMesh(bool build_active)
	{
		List<GameObject> list = new List<GameObject> { startSection, endSection };
		foreach (GameObject spawnedPiece in spawnedPieces)
		{
			list.Add(spawnedPiece);
		}
		if (build_active)
		{
			meshBuild = new GameObject("Build Mesh");
			meshBuild.transform.SetParent(meshBase.transform.parent);
			meshBuild.transform.localPosition = Vector3.zero;
			foreach (GameObject item in list)
			{
				Object.Instantiate(item, meshBuild.transform, worldPositionStays: true);
			}
		}
		else
		{
			meshBuild.SetObActive(active: false);
		}
		foreach (GameObject item2 in list)
		{
			item2.SetObActive(!build_active);
		}
	}

	protected override void DoDelete()
	{
		List<Ant> list = new List<Ant>();
		foreach (List<Trail> listSpawnedTrail in listSpawnedTrails)
		{
			foreach (Trail item in listSpawnedTrail)
			{
				foreach (Ant currentAnt in item.currentAnts)
				{
					list.Add(currentAnt);
				}
			}
		}
		foreach (Ant item2 in list)
		{
			Vector3 vector = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f));
			item2.StartLaunch(vector * Random.Range(10f, 40f), LaunchCause.LOST_FLOOR);
		}
		base.DoDelete();
	}

	public override UIClickType GetUiClickType_Intake()
	{
		return UIClickType.BUILDING_SMALL;
	}

	public override bool CanRelocate()
	{
		return false;
	}
}
