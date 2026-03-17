using System;
using System.Collections.Generic;
using UnityEngine;

public static class BlueprintManager
{
	private static List<Blueprint> blueprints = new List<Blueprint>();

	private static List<string> blueprintsInBar = new List<string>();

	private const int MAX_BLUEPRINTS_IN_BAR = 12;

	public static bool RefreshBlueprints()
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string item in Files.ELocalBlueprintCodes())
		{
			if (GetBlueprint(item) == null)
			{
				Blueprint blueprint = Blueprint.LoadLocalFromCode(item, only_header: true);
				if (blueprint == null)
				{
					continue;
				}
				blueprints.Add(blueprint);
			}
			hashSet.Add(item);
		}
		foreach (string item2 in Platform.current.ESubscribedBlueprintPaths())
		{
			string blueprintCodeFromPath = Files.GetBlueprintCodeFromPath(item2);
			if (blueprintCodeFromPath == null)
			{
				continue;
			}
			if (GetBlueprint(blueprintCodeFromPath) == null)
			{
				Blueprint blueprint2 = Blueprint.LoadFromPath(item2, blueprintCodeFromPath, only_header: true);
				if (blueprint2 == null)
				{
					continue;
				}
				blueprints.Add(blueprint2);
			}
			hashSet.Add(blueprintCodeFromPath);
		}
		for (int num = blueprints.Count - 1; num >= 0; num--)
		{
			if (!hashSet.Contains(blueprints[num].code))
			{
				blueprints.RemoveAt(num);
			}
		}
		return true;
	}

	public static void AddBlueprint(Blueprint blueprint, bool add_to_bar)
	{
		blueprints.Add(blueprint);
		if (add_to_bar)
		{
			SetInBar(blueprint.code, in_bar: true);
		}
	}

	public static IEnumerable<string> EBlueprintCodes()
	{
		foreach (Blueprint blueprint in blueprints)
		{
			yield return blueprint.code;
		}
	}

	public static void Clear()
	{
		foreach (Blueprint blueprint in blueprints)
		{
			blueprint.ClearTexture();
			blueprint.completelyLoaded = false;
		}
		blueprintsInBar.Clear();
	}

	public static IEnumerable<Blueprint> EBlueprintsInBar()
	{
		foreach (string item in blueprintsInBar)
		{
			Blueprint blueprint = GetBlueprint(item, need_complete: true);
			if (blueprint != null)
			{
				yield return blueprint;
			}
		}
	}

	public static Blueprint GetBlueprint(string code, bool need_complete = false)
	{
		int i;
		for (i = 0; i < blueprints.Count && !(blueprints[i].code == code); i++)
		{
		}
		Blueprint blueprint = ((i == blueprints.Count) ? null : blueprints[i]);
		if (blueprint != null && need_complete && !blueprint.completelyLoaded)
		{
			blueprint = Blueprint.LoadFromPath(blueprint.localPath, code, only_header: false);
			if (blueprint == null)
			{
				blueprints.RemoveAt(i);
			}
			else
			{
				blueprints[i] = blueprint;
			}
		}
		return blueprint;
	}

	public static void Write(Save save)
	{
		save.Write(blueprintsInBar.Count);
		foreach (string item in blueprintsInBar)
		{
			save.Write(item);
		}
	}

	public static void Read(Save save)
	{
		blueprintsInBar.Clear();
		if (save.version <= 75)
		{
			return;
		}
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string code = save.ReadString();
			if (GetBlueprint(code) != null)
			{
				SetInBar(code, in_bar: true);
			}
		}
	}

	public static Blueprint CreateBlueprint(List<FloorTile> floor, bool temporary = false)
	{
		List<Building> buildings = new List<Building>();
		List<Split> splits = new List<Split>();
		List<Trail> floating_trails = new List<Trail>();
		Vector3 center = floor.GetCenter();
		int num = 0;
		float num2 = float.MaxValue;
		for (int i = 0; i < floor.Count; i++)
		{
			float sqrMagnitude = (floor[i].transform.position - center).sqrMagnitude;
			if (sqrMagnitude < num2)
			{
				num2 = sqrMagnitude;
				num = i;
			}
		}
		int index = num;
		FloorTile floorTile = floor[num];
		FloorTile floorTile2 = floor[0];
		FloorTile floorTile3 = (floor[0] = floorTile);
		floorTile3 = (floor[index] = floorTile2);
		center.y = floor[0].transform.position.y;
		foreach (FloorTile item in floor)
		{
			buildings.Add(item);
			item.GatherContent(ref buildings, ref splits);
		}
		foreach (FloorTile item2 in floor)
		{
			item2.GatherFloatingTrails(splits, ref floating_trails);
		}
		for (int num3 = buildings.Count - 1; num3 >= 0; num3--)
		{
			Building building = buildings[num3];
			if (building.data.maxBuildCount > 0 || building is Bridge)
			{
				buildings.RemoveAt(num3);
			}
			else
			{
				foreach (BuildingAttachPoint buildingAttachPoint in building.buildingAttachPoints)
				{
					if (buildingAttachPoint.HasAttachment(out var att) && !buildings.Contains(att))
					{
						buildings.Add(att);
					}
				}
			}
		}
		for (int num4 = splits.Count - 1; num4 >= 0; num4--)
		{
			Split split = splits[num4];
			bool flag = false;
			if (split.IsInBuilding())
			{
				flag = true;
			}
			else if (split.GetTrailType() == TrailType.COMMAND)
			{
				flag = true;
			}
			else
			{
				int num5 = 0;
				foreach (Trail connectedTrail in split.connectedTrails)
				{
					if (!connectedTrail.IsAction() && !connectedTrail.IsCommandTrail() && !connectedTrail.IsBuilding() && connectedTrail.IsPlaced())
					{
						num5++;
					}
				}
				if (num5 == 0)
				{
					flag = true;
				}
			}
			if (flag)
			{
				splits.RemoveAt(num4);
			}
		}
		Blueprint blueprint = new Blueprint(buildings[0], center);
		foreach (Building item3 in buildings)
		{
			blueprint.AddBuilding(item3);
		}
		foreach (BlueprintBuilding building2 in blueprint.buildings)
		{
			building2.StoreData();
		}
		foreach (Split item4 in splits)
		{
			blueprint.AddSplit(item4);
		}
		DateTime now = DateTime.Now;
		string text = $"{now.Year % 100}{now.Month:00}{now.Day:00}_{now.Hour:00}{now.Minute:00}{now.Second:00}";
		blueprint.code = $"{text}_{UnityEngine.Random.Range(1000, 9999)}";
		blueprint.creationDate = DateTime.Now;
		blueprint.name = text;
		blueprint.localPath = Files.LocalBlueprintPath(blueprint.code);
		List<Trail> list = new List<Trail>();
		Dictionary<Trail, (Vector3, Vector3)> dictionary = new Dictionary<Trail, (Vector3, Vector3)>();
		for (int j = 0; j < splits.Count; j++)
		{
			Split split2 = splits[j];
			foreach (Trail connectedTrail2 in split2.connectedTrails)
			{
				if (connectedTrail2.IsInBuilding(out var owner))
				{
					blueprint.splits[j].LinkToBuildingSplit(owner);
				}
				if (connectedTrail2.IsAction() || !connectedTrail2.IsPlaced() || connectedTrail2.IsBuilding() || list.Contains(connectedTrail2))
				{
					continue;
				}
				list.Add(connectedTrail2);
				Split otherSplit = connectedTrail2.GetOtherSplit(split2);
				if (otherSplit.IsInBuilding())
				{
					continue;
				}
				bool flag2 = connectedTrail2.splitStart == split2;
				int num6 = j;
				int num7 = splits.IndexOf(otherSplit);
				TrailType trail_type = connectedTrail2.trailType;
				if (num7 < 0)
				{
					Vector3 vector = FloorTile.FindEdgePos(floor, split2.transform.position, otherSplit.transform.position, 1f);
					if ((split2.transform.position - vector).sqrMagnitude < 2.25f)
					{
						continue;
					}
					num7 = blueprint.AddSplit(vector);
					dictionary[connectedTrail2] = (split2.transform.position, vector);
					if (connectedTrail2.IsLogic() && !flag2)
					{
						trail_type = TrailType.HAULING;
					}
				}
				if (!flag2)
				{
					int num8 = num7;
					index = num6;
					num6 = num8;
					num7 = index;
				}
				BlueprintTrail blueprintTrail = blueprint.AddTrail(num6, num7, trail_type);
				TrailGate trailGate = connectedTrail2.trailGate;
				if (trailGate != null)
				{
					blueprintTrail.StoreGateData(trailGate);
				}
			}
		}
		foreach (Trail item5 in floating_trails)
		{
			Vector3 position = item5.splitStart.transform.position;
			Vector3 position2 = item5.splitEnd.transform.position;
			Vector3 vector2 = FloorTile.FindEdgePos(floor, position, position2, 1f);
			Vector3 vector3 = FloorTile.FindEdgePos(floor, position2, position, 1f);
			if (!((vector3 - vector2).sqrMagnitude < 2.25f))
			{
				int split_id_start = blueprint.AddSplit(vector3);
				int split_id_end = blueprint.AddSplit(vector2);
				dictionary[item5] = (vector3, vector2);
				list.Add(item5);
				TrailType trail_type2 = (item5.IsLogic() ? TrailType.HAULING : item5.trailType);
				blueprint.AddTrail(split_id_start, split_id_end, trail_type2);
			}
		}
		if (!temporary)
		{
			List<GameObject> list2 = new List<GameObject>();
			foreach (Building item6 in buildings)
			{
				list2.Add(item6.gameObject);
			}
			foreach (Split item7 in splits)
			{
				list2.Add(item7.gameObject);
			}
			foreach (Trail item8 in list)
			{
				if (dictionary.TryGetValue(item8, out var value))
				{
					dictionary[item8] = (item8.posStart, item8.posEnd);
					item8.SetStartEndPos(value.Item1, value.Item2, only_visual: true);
				}
				list2.Add(item8.gameObject);
			}
			Texture2D icon = CaptureIcon(floor, list2);
			blueprint.SetIcon(icon);
			blueprint.completelyLoaded = true;
			foreach (KeyValuePair<Trail, (Vector3, Vector3)> item9 in dictionary)
			{
				item9.Key.SetStartEndPos(item9.Value.Item1, item9.Value.Item2, only_visual: true);
			}
		}
		return blueprint;
	}

	public static bool GetInBar(string code)
	{
		return blueprintsInBar.Contains(code);
	}

	public static bool BarFull()
	{
		return blueprintsInBar.Count >= 12;
	}

	public static void SetInBar(string code, bool in_bar)
	{
		if (in_bar)
		{
			if (!blueprintsInBar.Contains(code) && !BarFull())
			{
				blueprintsInBar.Add(code);
			}
		}
		else
		{
			blueprintsInBar.Remove(code);
		}
	}

	private static Texture2D CaptureIcon(List<FloorTile> floor, List<GameObject> show_obs)
	{
		int num = 1024;
		List<(GameObject, int)> list = new List<(GameObject, int)>();
		int num2 = 2;
		foreach (GameObject show_ob in show_obs)
		{
			Renderer[] componentsInChildren = show_ob.GetComponentsInChildren<Renderer>(includeInactive: false);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				GameObject gameObject = componentsInChildren[i].gameObject;
				list.Add((gameObject, gameObject.layer));
				gameObject.layer = num2;
			}
		}
		Transform transform = floor[0].transform;
		float num3 = transform.rotation.eulerAngles.y;
		float y = CamController.instance.transform.rotation.eulerAngles.y;
		for (int j = 0; j < 4; j++)
		{
			float num4 = Mathf.Abs(num3 - y);
			if (num4 > 180f)
			{
				num4 = 360f - num4;
			}
			if (num4 < 45f)
			{
				break;
			}
			num3 += 90f;
			if (num3 > 360f)
			{
				num3 -= 360f;
			}
		}
		MinMax minMax = default(MinMax);
		MinMax minMax2 = default(MinMax);
		bool flag = true;
		foreach (FloorTile item3 in floor)
		{
			Vector3 vector = transform.InverseTransformPoint(item3.transform.position);
			if (flag)
			{
				minMax = new MinMax(vector.x);
				minMax2 = new MinMax(vector.z);
				flag = false;
			}
			else
			{
				minMax = minMax.Include(vector.x);
				minMax2 = minMax2.Include(vector.z);
			}
		}
		Transform transform2 = UnityEngine.Object.Instantiate(AssetLinks.standard.prefabBlueprintCamera).transform;
		Vector3 vector2 = new Vector3(minMax.Lerp(0.5f), 0f, minMax2.Lerp(0.5f));
		vector2 = transform.TransformVector(vector2);
		transform2.SetPositionAndRotation(new Vector3(transform.position.x + vector2.x, 10f, transform.position.z + vector2.z), Quaternion.Euler(90f, num3, 0f));
		Camera component = transform2.GetComponent<Camera>();
		component.orthographicSize = (Mathf.Max(minMax.GetLength(), minMax2.GetLength()) + floor[0].tileSize) * 0.5f + 5f;
		component.cullingMask = 1 << num2;
		RenderTexture renderTexture = (component.targetTexture = new RenderTexture(num, num, 24, RenderTextureFormat.ARGB32));
		component.Render();
		Texture2D tex = new Texture2D(num, num, TextureFormat.RGBA32, mipChain: false);
		RenderTexture.active = renderTexture;
		tex.ReadPixels(component.pixelRect, 0, 0);
		tex.Apply();
		RenderTexture.active = null;
		float contrast = 3f;
		int brightness = 170;
		Toolkit.AdjustContrastBrightness(ref tex, contrast, brightness);
		renderTexture.Release();
		UnityEngine.Object.Destroy(renderTexture);
		UnityEngine.Object.Destroy(component.gameObject);
		foreach (var item4 in list)
		{
			GameObject item = item4.Item1;
			int item2 = item4.Item2;
			item.layer = item2;
		}
		return tex;
	}
}
