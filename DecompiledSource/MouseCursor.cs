using System.Collections.Generic;
using UnityEngine;

public class MouseCursor : MonoBehaviour
{
	[SerializeField]
	private GameObject obSphere;

	[SerializeField]
	private GameObject obCylinder;

	[SerializeField]
	private GameObject obCylinderBig;

	[SerializeField]
	private GameObject obSquare;

	[SerializeField]
	private GameObject obPyramid;

	[SerializeField]
	private GameObject obPencil;

	[SerializeField]
	private GameObject obEraser;

	[SerializeField]
	private GameObject obBlockEraser;

	[SerializeField]
	private List<Renderer> rends = new List<Renderer>();

	[SerializeField]
	private Animator animPencil;

	[SerializeField]
	private Animator animEraser;

	[SerializeField]
	private Animator animBlockEraser;

	[SerializeField]
	private Vector2 bodySizeRange = new Vector2(0.5f, 5f);

	[SerializeField]
	private Vector2 sphereSizeRange = new Vector2(0.5f, 5f);

	public void Clear()
	{
		SetFootMesh(MouseCursorFootMesh.NONE);
		SetBodyMesh(MouseCursorBodyMesh.NONE);
	}

	public void Update3DCursor(Vector3 pos)
	{
		base.transform.position = pos;
		foreach (GameObject item in new List<GameObject> { obPencil, obEraser, obBlockEraser, obSquare })
		{
			if (item.activeSelf)
			{
				item.transform.rotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
				item.transform.localScale = Vector3.one * Mathf.Clamp(CamController.instance.GetZoomFactor(), bodySizeRange.x, bodySizeRange.y);
			}
		}
		obSphere.transform.localScale = Vector3.one * Mathf.Clamp(CamController.instance.GetZoomFactor(), sphereSizeRange.x, sphereSizeRange.y);
	}

	public void Click3DCursor()
	{
		if (obPencil.activeSelf)
		{
			animPencil.SetTrigger("Click");
		}
		if (obEraser.activeSelf)
		{
			animEraser.SetBool("ClickHold", value: true);
		}
		if (obBlockEraser.activeSelf)
		{
			animBlockEraser.SetBool("ClickHold", value: true);
		}
	}

	public void ClickRelease3DCursor()
	{
		if (obEraser.activeSelf)
		{
			animEraser.SetBool("ClickHold", value: false);
		}
		if (obBlockEraser.activeSelf)
		{
			animBlockEraser.SetBool("ClickHold", value: false);
		}
	}

	public void SetFootMesh(MouseCursorFootMesh _cursor)
	{
		obSphere.SetObActive(_cursor == MouseCursorFootMesh.SPHERE);
		obCylinder.SetObActive(_cursor == MouseCursorFootMesh.CYLINDER);
		obCylinderBig.SetObActive(_cursor == MouseCursorFootMesh.CYLINDER_BIG);
		obSquare.SetObActive(_cursor == MouseCursorFootMesh.SQUARE);
	}

	public void SetBodyMesh(MouseCursorBodyMesh _mesh)
	{
		obPyramid.SetObActive(_mesh == MouseCursorBodyMesh.PYRAMID);
		obPencil.SetObActive(_mesh == MouseCursorBodyMesh.PENCIL);
		obEraser.SetObActive(_mesh == MouseCursorBodyMesh.ERASER);
		obBlockEraser.SetObActive(_mesh == MouseCursorBodyMesh.BLOCK_ERASER);
	}

	public void SetMaterial(Material mat)
	{
		foreach (Renderer rend in rends)
		{
			rend.sharedMaterial = mat;
		}
	}

	public bool IsVisible()
	{
		bool result = false;
		foreach (GameObject item in new List<GameObject> { obSphere, obCylinder, obCylinderBig, obSquare, obPyramid, obPencil, obEraser, obBlockEraser })
		{
			if (item.activeSelf)
			{
				result = true;
			}
		}
		return result;
	}
}
