using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ProcMesh
{
	private List<Vector3> vertices;

	private List<int> triangles;

	private List<Color> colors;

	public ProcMesh()
	{
		vertices = new List<Vector3>();
		triangles = new List<int>();
		colors = new List<Color>();
	}

	public int AddVertex(Vector3 vx, Color c)
	{
		vertices.Add(vx);
		colors.Add(c);
		return vertices.Count - 1;
	}

	public int AddTriangleV(int v1, int v2, int v3)
	{
		triangles.Add(v1);
		triangles.Add(v2);
		triangles.Add(v3);
		return triangles.Count / 3 - 1;
	}

	public void AddRectV(int v1, int v2, int v3, int v4)
	{
		AddTriangleV(v1, v2, v3);
		AddTriangleV(v3, v4, v1);
	}

	public void AddRect(Vector3 lb, Vector3 rb, Vector3 ro, Vector3 lo, Color col)
	{
		AddRectV(AddVertex(lb, col), AddVertex(rb, col), AddVertex(ro, col), AddVertex(lo, col));
	}

	public GameObject GenerateInNewObject(string name, Vector3 pos, Material material)
	{
		GameObject gameObject = new GameObject(name);
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		gameObject.transform.position = pos;
		meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
		meshRenderer.receiveShadows = false;
		meshRenderer.sharedMaterials = new Material[1] { material };
		Mesh mesh = meshFilter.mesh;
		mesh.vertices = vertices.ToArray();
		mesh.colors = colors.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateBounds();
		mesh.Optimize();
		return gameObject;
	}
}
