using System.Collections.Generic;
using UnityEngine;

public class AssignLine : MonoBehaviour
{
	public List<AssignLineStyle> styles = new List<AssignLineStyle>();

	private AssignType currentType;

	private AssignLineStatus currentStatus;

	private float textureOffset;

	private static bool materialsInited;

	private static List<List<Material>> materials;

	public void Init()
	{
		if (!materialsInited)
		{
			materials = new List<List<Material>>();
			foreach (AssignLineStyle style in styles)
			{
				List<Material> list = new List<Material>();
				LineRenderer[] lrs = style.lrs;
				for (int i = 0; i < lrs.Length; i++)
				{
					Material item = Object.Instantiate(lrs[i].sharedMaterial);
					list.Add(item);
				}
				materials.Add(list);
			}
			materialsInited = true;
		}
		for (int j = 0; j < styles.Count; j++)
		{
			for (int k = 0; k < styles[j].lrs.Length; k++)
			{
				styles[j].lrs[k].sharedMaterial = materials[j][k];
			}
		}
	}

	public void SetLine(Vector3 start, Vector3 end, AssignType assign_type, AssignLineStatus status)
	{
		base.transform.SetPositionAndRotation(start, Quaternion.LookRotation(Toolkit.LookVector(start, end.TargetYPosition(start.y)), Vector3.up));
		currentType = assign_type;
		currentStatus = status;
		foreach (AssignLineStyle style in styles)
		{
			if (!style.types.Contains(currentType) || style.status != currentStatus)
			{
				style.ob.SetObActive(active: false);
				continue;
			}
			style.ob.SetObActive(active: true);
			float f = Vector3.Distance(start, end);
			LineRenderer[] lrs = style.lrs;
			foreach (LineRenderer lineRenderer in lrs)
			{
				if (style.arc)
				{
					lineRenderer.positionCount = Mathf.Clamp(Mathf.RoundToInt(f), 10, 200);
					for (int num = lineRenderer.positionCount - 1; num > -1; num--)
					{
						Vector3 pointInFlightArc = FlightPad.GetPointInFlightArc(end, start, (float)num / ((float)lineRenderer.positionCount - 1f));
						lineRenderer.SetPosition(num, pointInFlightArc);
					}
				}
				else
				{
					lineRenderer.positionCount = 2;
					lineRenderer.SetPosition(1, start);
					lineRenderer.SetPosition(0, end);
				}
			}
		}
	}

	public void UpdateLine()
	{
		foreach (AssignLineStyle style in styles)
		{
			if (!style.types.Contains(currentType) || style.status != currentStatus || style.animationStyle == AnimationStyle.NONE)
			{
				continue;
			}
			LineRenderer[] lrs = style.lrs;
			foreach (LineRenderer lineRenderer in lrs)
			{
				switch (style.animationStyle)
				{
				case AnimationStyle.FORWARD:
					textureOffset += Time.deltaTime * 0.5f;
					if (textureOffset > 1f)
					{
						textureOffset = -1f;
					}
					lineRenderer.sharedMaterial.SetTextureOffset("_BaseMap", new Vector2(textureOffset, 0f));
					break;
				case AnimationStyle.BACKWARD:
					textureOffset -= Time.deltaTime * 0.5f;
					if (textureOffset < -1f)
					{
						textureOffset = 1f;
					}
					lineRenderer.sharedMaterial.SetTextureOffset("_BaseMap", new Vector2(textureOffset, 0f));
					break;
				}
			}
		}
	}
}
