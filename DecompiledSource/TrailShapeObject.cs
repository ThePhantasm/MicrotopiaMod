using System;
using UnityEngine;

[Serializable]
public class TrailShapeObject
{
	public string name;

	public TrailShape shape;

	public GameObject ob;

	public LineRenderer[] lrs;

	public Renderer[] rends;

	public Renderer[] rendsShaded;

	public bool inverted;

	public float offsetStart;

	public float offsetEnd;

	public bool useLineMesh;

	public bool withArrow;

	public float lineWidth = 4f;

	public float tileFactor = 0.5f;

	public Material baseMaterial;

	public void SetLine(Vector3 start, Vector3 end)
	{
		for (int i = 0; i < lrs.Length; i++)
		{
			if (offsetStart != 0f || offsetEnd != 0f)
			{
				Vector3 vector = Toolkit.LookVectorNormalized(start, end);
				start += vector * offsetStart;
				end += vector * offsetEnd;
			}
			lrs[i].SetPosition(inverted ? 1 : 0, start.TargetYPosition(start.y + 0.75f - 0.01f * (float)i));
			lrs[i].SetPosition((!inverted) ? 1 : 0, end.TargetYPosition(end.y + 0.75f - 0.01f * (float)i));
		}
	}

	public void Init(Renderer quad_line, Renderer quad_arrow, bool invisible)
	{
		if (useLineMesh)
		{
			quad_line.enabled = !invisible;
			quad_arrow.enabled = !invisible && withArrow;
			float num = (inverted ? (-1f) : 1f);
			float num2 = lineWidth;
			Vector3 localScale = quad_line.transform.localScale;
			if (localScale.x != num || localScale.y != num2)
			{
				quad_line.transform.localScale = new Vector3(num, num2, localScale.z);
				quad_arrow.transform.localScale = quad_arrow.transform.localScale.SetX(num);
			}
		}
		else
		{
			if (quad_line != null)
			{
				quad_line.enabled = false;
				quad_arrow.enabled = false;
			}
			if (ob != null)
			{
				ob.SetObActive(!invisible);
			}
		}
	}

	public void SetMaterial(Material mat, Renderer quad_line, Renderer quad_arrow, float offset)
	{
		Color color = mat.GetColor("_Color");
		Color color2 = mat.GetColor("_EmissionColor");
		int num = (int)shape * 10;
		if (useLineMesh)
		{
			num += 10000;
			quad_line.sharedMaterial = MaterialLibrary.GetTrailMaterial(baseMaterial, num, color, color2, offset);
			if (withArrow)
			{
				quad_arrow.sharedMaterial = MaterialLibrary.GetTrailMaterial(quad_arrow.sharedMaterial, num + 1, color, color2, offset);
			}
			return;
		}
		for (int i = 0; i < rends.Length; i++)
		{
			if (rends[i].gameObject.activeSelf)
			{
				rends[i].sharedMaterial = mat;
			}
		}
		for (int j = 0; j < rendsShaded.Length; j++)
		{
			rendsShaded[j].sharedMaterial = MaterialLibrary.GetTrailMaterial(rendsShaded[j].sharedMaterial, num, color, color2, offset);
			num++;
		}
	}
}
