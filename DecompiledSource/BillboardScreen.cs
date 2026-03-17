using System;
using TMPro;
using UnityEngine;

[Serializable]
public class BillboardScreen
{
	public string name;

	public BillboardType type;

	public GameObject ob;

	public TextMeshProUGUI lbBillboard;

	public bool bobbing = true;
}
