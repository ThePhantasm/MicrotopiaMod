using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighlightPlus;

[ExecuteInEditMode]
public class HighlightSeeThroughOccluder : MonoBehaviour
{
	public DetectionMethod detectionMethod;

	[NonSerialized]
	public MeshData[] meshData;

	private List<Renderer> rr;

	private void OnEnable()
	{
		if (base.gameObject.activeInHierarchy)
		{
			Init();
		}
	}

	private void Init()
	{
		if (detectionMethod == DetectionMethod.RayCast)
		{
			HighlightEffect.RegisterOccluder(this);
			return;
		}
		if (rr == null)
		{
			rr = new List<Renderer>();
		}
		else
		{
			rr.Clear();
		}
		GetComponentsInChildren(rr);
		int count = rr.Count;
		meshData = new MeshData[count];
		for (int i = 0; i < count; i++)
		{
			meshData[i].renderer = rr[i];
			meshData[i].subMeshCount = 1;
			if (rr[i] is MeshRenderer)
			{
				MeshFilter component = rr[i].GetComponent<MeshFilter>();
				if (component != null && component.sharedMesh != null)
				{
					meshData[i].subMeshCount = component.sharedMesh.subMeshCount;
				}
			}
			else if (rr[i] is SkinnedMeshRenderer)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)rr[i];
				meshData[i].subMeshCount = skinnedMeshRenderer.sharedMesh.subMeshCount;
			}
		}
		if (count > 0)
		{
			HighlightEffect.RegisterOccluder(this);
		}
	}

	private void OnDisable()
	{
		HighlightEffect.UnregisterOccluder(this);
	}
}
