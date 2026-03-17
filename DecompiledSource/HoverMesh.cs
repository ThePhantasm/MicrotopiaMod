using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HoverMesh : MonoBehaviour
{
	private Renderer[] rends;

	private List<Collider> hitCols = new List<Collider>();

	private Color colValid = Color.green;

	private Color colUnvalid = Color.red;

	private Material matHoverMesh;

	private Material trailErrorMaterial;

	private List<(Transform, Transform, Renderer, Material)> hoverTrails;

	private HashSet<int> trailErrors;

	private HashSet<int> trailErrorsPrev;

	public static HoverMesh CreateFrom(GameObject original, GameObject hover_collider_parent, Material mat)
	{
		HoverMesh hoverMesh = Object.Instantiate(original, original.transform.parent).AddComponent<HoverMesh>();
		hoverMesh.Init(hover_collider_parent, mat);
		return hoverMesh;
	}

	public void Init(GameObject hover_collider_parent, Material mat)
	{
		Rigidbody rigidbody = base.gameObject.AddComponent<Rigidbody>();
		rigidbody.isKinematic = true;
		rigidbody.useGravity = false;
		if (mat != null)
		{
			matHoverMesh = new Material(mat);
			colValid = matHoverMesh.color;
		}
		else
		{
			matHoverMesh = new Material(Resources.Load<Material>("Materials/HoverMesh"));
		}
		ProcessRenderers(base.gameObject);
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>(includeInactive: true);
		Collider[] array;
		if (hover_collider_parent != null)
		{
			array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				Object.Destroy(array[i]);
			}
			GameObject obj = Object.Instantiate(hover_collider_parent, base.transform);
			Vector3 localScale = obj.transform.localScale;
			Vector3 localScale2 = base.transform.localScale;
			obj.transform.localScale = new Vector3(localScale.x / localScale2.x, localScale.y / localScale2.y, localScale.z / localScale2.z);
			componentsInChildren = obj.GetComponentsInChildren<Collider>();
			array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
		}
		array = componentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].isTrigger = true;
		}
		Vector3 localPosition = base.transform.localPosition;
		localPosition.y += 0.01f;
		base.transform.localPosition = localPosition;
		colValid.a = 0.5f;
		colUnvalid.a = 0.5f;
	}

	private void ProcessRenderers(GameObject in_ob)
	{
		rends = in_ob.GetComponentsInChildren<Renderer>();
		Renderer[] array = rends;
		foreach (Renderer renderer in array)
		{
			if (renderer.gameObject.layer == 22)
			{
				renderer.gameObject.SetObActive(active: false);
			}
			else
			{
				renderer.sharedMaterial = matHoverMesh;
			}
		}
	}

	public GameObject AddMesh(GameObject ob)
	{
		GameObject gameObject = Object.Instantiate(ob, base.transform);
		ProcessRenderers(gameObject);
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].isTrigger = true;
		}
		return gameObject;
	}

	private void OnEnable()
	{
		hitCols.Clear();
	}

	private void OnTriggerStay(Collider other)
	{
		if (!hitCols.Contains(other))
		{
			hitCols.Add(other);
		}
	}

	public IEnumerable<Collider> EOverlaps()
	{
		foreach (Collider hitCol in hitCols)
		{
			if (hitCol != null)
			{
				yield return hitCol;
			}
		}
	}

	public void ResetOverlap()
	{
		hitCols.Clear();
	}

	public void Highlight(bool valid)
	{
		matHoverMesh.SetColor("_BaseColor", valid ? colValid : colUnvalid);
	}

	public void SetVisible(bool target)
	{
		Renderer[] array = rends;
		foreach (Renderer renderer in array)
		{
			if (!(renderer == null))
			{
				renderer.enabled = target;
			}
		}
	}

	private void OnDestroy()
	{
		if (matHoverMesh != null)
		{
			Object.Destroy(matHoverMesh);
		}
	}

	public void AddTrails(List<(Vector3, Vector3, TrailType)> trail_data, Building rel_to_building, Vector3 offset)
	{
		hoverTrails = new List<(Transform, Transform, Renderer, Material)>();
		trailErrors = new HashSet<int>();
		trailErrorsPrev = new HashSet<int>();
		foreach (var trail_datum in trail_data)
		{
			Vector3 item = trail_datum.Item1;
			Vector3 item2 = trail_datum.Item2;
			TrailType item3 = trail_datum.Item3;
			Trail trail = GameManager.instance.NewTrail_VisualOnly(item3);
			Renderer baseRenderer = trail.GetBaseRenderer();
			trail.SetMaterial(item3, lit_up: false);
			trail.SetTrailShapeDirect(TrailShape.BLUEPRINT);
			if (rel_to_building != null)
			{
				trail.SetStartEndPos(rel_to_building.transform.TransformPoint(item) + offset, rel_to_building.transform.TransformPoint(item2) + offset, only_visual: true);
			}
			else
			{
				trail.SetStartEndPos(item + offset, item2 + offset, only_visual: true);
			}
			Transform transform = trail.transform;
			Object.Destroy(trail.col);
			Object.Destroy(trail);
			transform.SetParent(base.transform);
			hoverTrails.Add((transform, baseRenderer.transform.parent, baseRenderer, baseRenderer.sharedMaterial));
		}
	}

	public bool HasTrails()
	{
		return hoverTrails != null;
	}

	public IEnumerable<(Vector3, Vector3, int)> ETrails()
	{
		if (hoverTrails == null)
		{
			yield break;
		}
		int i = 0;
		foreach (var hoverTrail in hoverTrails)
		{
			Transform item = hoverTrail.Item1;
			Transform item2 = hoverTrail.Item2;
			Vector3 position = item.position;
			Vector3 position2 = item2.position;
			yield return (position, position2 + position2 - position, i++);
		}
	}

	public void ResetTrailErrors()
	{
		HashSet<int> hashSet = trailErrorsPrev;
		HashSet<int> hashSet2 = trailErrors;
		trailErrors = hashSet;
		trailErrorsPrev = hashSet2;
		trailErrors.Clear();
	}

	public void SetTrailError(int i)
	{
		if (!trailErrorsPrev.Contains(i))
		{
			ShowTrailError(i, show: true);
		}
		trailErrors.Add(i);
	}

	public void ShowTrailErrors()
	{
		foreach (int item in trailErrorsPrev)
		{
			if (!trailErrors.Contains(item))
			{
				ShowTrailError(item, show: false);
			}
		}
	}

	private void ShowTrailError(int i, bool show)
	{
		(Transform, Transform, Renderer, Material) tuple = hoverTrails[i];
		Renderer item = tuple.Item3;
		Transform transform = item.transform;
		if (show)
		{
			transform.localScale = transform.localScale.SetY(0.8f);
			if (trailErrorMaterial == null)
			{
				trailErrorMaterial = AssetLinks.standard.GetTrailMaterial(TrailStatus.HOVERING_ERROR);
			}
			item.sharedMaterial = trailErrorMaterial;
		}
		else
		{
			transform.localScale = transform.localScale.SetY(4f);
			item.sharedMaterial = tuple.Item4;
		}
	}
}
