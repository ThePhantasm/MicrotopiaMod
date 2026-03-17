using System.Collections.Generic;
using UnityEngine;

public class Hologram : MonoBehaviour
{
	public Transform obParent;

	public float rotSpeed = 0.1f;

	[SerializeField]
	private List<HoloShapeLink> shapes = new List<HoloShapeLink>();

	private GameObject holoOb;

	private ClickableObject hoveringOb;

	private HologramShape currentShape;

	private PickupType currentShape_pickup;

	private AntCaste currentShape_ant;

	private bool firstTime = true;

	public void StartHologram(ClickableObject _ob)
	{
		hoveringOb = _ob;
		if (firstTime)
		{
			firstTime = false;
			ClearHologram();
		}
		UpdateHologram();
		obParent.transform.rotation = Quaternion.LookRotation(Toolkit.LookVector(base.transform.position, CamController.GetCamPos().TransformYPosition(base.transform)), Vector3.up);
	}

	public void UpdateHologram()
	{
		PickupType _pickup;
		AntCaste _ant;
		HologramShape hologramShape = hoveringOb.GetHologramShape(out _pickup, out _ant);
		if (hologramShape != currentShape || _pickup != currentShape_pickup || _ant != currentShape_ant)
		{
			currentShape = hologramShape;
			currentShape_pickup = _pickup;
			currentShape_ant = _ant;
			ClearHologram();
			switch (hologramShape)
			{
			case HologramShape.Pickup:
			{
				PickupData pickupData = PickupData.Get(_pickup);
				holoOb = Object.Instantiate(pickupData.prefab, obParent);
				holoOb.transform.localPosition = Vector3.zero;
				holoOb.transform.localRotation = Quaternion.identity;
				holoOb.transform.localScale *= 2f;
				Pickup component = holoOb.GetComponent<Pickup>();
				component.GetMesh();
				Object.Destroy(component);
				Collider[] componentsInChildren = holoOb.GetComponentsInChildren<Collider>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					Object.Destroy(componentsInChildren[i]);
				}
				break;
			}
			case HologramShape.Ant:
			{
				AntCasteData antCasteData = AntCasteData.Get(_ant);
				holoOb = Object.Instantiate(antCasteData.prefab, obParent);
				holoOb.transform.localPosition = Vector3.zero;
				holoOb.transform.localRotation = Quaternion.identity;
				holoOb.transform.localScale *= 1.5f;
				Object.Destroy(holoOb.GetComponent<Ant>());
				Collider[] componentsInChildren = holoOb.GetComponentsInChildren<Collider>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					Object.Destroy(componentsInChildren[i]);
				}
				ParticleSystem[] componentsInChildren2 = holoOb.GetComponentsInChildren<ParticleSystem>();
				for (int i = 0; i < componentsInChildren2.Length; i++)
				{
					Object.Destroy(componentsInChildren2[i]);
				}
				break;
			}
			default:
				foreach (HoloShapeLink shape in shapes)
				{
					if (shape.shape == hologramShape)
					{
						shape.ob.SetObActive(active: true);
					}
				}
				break;
			case HologramShape.None:
			case HologramShape.Building:
				break;
			}
		}
		obParent.transform.localScale = Vector3.one * hoveringOb.HologramSize();
		obParent.transform.Rotate(0f, Time.deltaTime * rotSpeed, 0f);
	}

	private void ClearHologram()
	{
		if (holoOb != null)
		{
			Object.Destroy(holoOb);
			holoOb = null;
		}
		foreach (HoloShapeLink shape in shapes)
		{
			shape.ob.SetObActive(active: false);
		}
	}
}
