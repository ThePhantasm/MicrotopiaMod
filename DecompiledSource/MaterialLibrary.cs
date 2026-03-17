using System.Collections.Generic;
using UnityEngine;

public static class MaterialLibrary
{
	private static Dictionary<(int, Color, Color, float), Material> assignedTrailMaterials = new Dictionary<(int, Color, Color, float), Material>();

	private static Dictionary<int, Material> origTrailMaterials = new Dictionary<int, Material>();

	private static Dictionary<(int, Material), Material> blinkMaterials = new Dictionary<(int, Material), Material>();

	private static Dictionary<float, Mesh> quadMeshes = new Dictionary<float, Mesh>();

	public static Material GetTrailMaterial(Material base_material, int rend_nr, Color col, Color em, float offset = float.MinValue)
	{
		(int, Color, Color, float) key = (rend_nr, col, em, offset);
		if (!assignedTrailMaterials.TryGetValue(key, out var value))
		{
			if (!origTrailMaterials.TryGetValue(rend_nr, out var value2))
			{
				value2 = base_material;
				origTrailMaterials[rend_nr] = value2;
			}
			value = new Material(value2);
			value.SetColor("_Color", col);
			value.SetColor("_EmissionColor", em);
			if (offset != float.MinValue)
			{
				value.SetFloat("_Offset", offset);
			}
			assignedTrailMaterials[key] = value;
		}
		return value;
	}

	public static Material GetBlinkMaterial(Material base_material, int rnd, float speed)
	{
		(int, Material) key = (rnd, base_material);
		if (!blinkMaterials.TryGetValue(key, out var value))
		{
			value = new Material(base_material);
			value.SetFloat("_Eye_Blink_Speed", speed);
			blinkMaterials[key] = value;
		}
		return value;
	}

	public static Mesh GetQuadMesh(float tiling)
	{
		if (!quadMeshes.TryGetValue(tiling, out var value))
		{
			value = GetNewQuadMesh(tiling);
			quadMeshes[tiling] = value;
		}
		return value;
	}

	public static Mesh GetNewQuadMesh(float tiling)
	{
		Mesh mesh = new Mesh();
		mesh.vertices = new Vector3[4]
		{
			new Vector3(-0.5f, -0.5f, 0f),
			new Vector3(0.5f, -0.5f, 0f),
			new Vector3(-0.5f, 0.5f, 0f),
			new Vector3(0.5f, 0.5f, 0f)
		};
		mesh.triangles = new int[6] { 0, 3, 1, 3, 0, 2 };
		mesh.uv = GetQuadUv(tiling);
		return mesh;
	}

	public static Vector2[] GetQuadUv(float tiling)
	{
		return new Vector2[4]
		{
			new Vector2(0f, 0f),
			new Vector2(tiling, 0f),
			new Vector2(0f, 1f),
			new Vector2(tiling, 1f)
		};
	}
}
