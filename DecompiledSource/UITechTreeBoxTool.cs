using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[ExecuteInEditMode]
public class UITechTreeBoxTool : MonoBehaviour
{
	public bool fillBox;

	public UITechTreeTree uiTechTreeTree;

	private void Update()
	{
		if (!fillBox)
		{
			return;
		}
		fillBox = false;
		List<UITechTreeBox> listBoxes = DoFillBox();
		if (uiTechTreeTree != null)
		{
			uiTechTreeTree.listBoxes = listBoxes;
		}
		Platform.Select();
		XmlDocument xmlDoc = SheetReader.GetXmlDoc(Files.FodsTechTree());
		if (xmlDoc != null)
		{
			foreach (SheetRow item in SheetReader.ERead(xmlDoc, "TechTree"))
			{
				string text = item.GetString("Code");
				if (SheetRow.Skip(text))
				{
					continue;
				}
				foreach (UITechTreeBox listBox in uiTechTreeTree.listBoxes)
				{
					if (listBox.techCode != text)
					{
						continue;
					}
					listBox.requiredTechs.Clear();
					string str = item.GetString("REQUIRED_TECH");
					if (SheetRow.Skip(str))
					{
						continue;
					}
					foreach (string item2 in str.EListItems())
					{
						listBox.requiredTechs.Add(item2);
					}
				}
			}
		}
		uiTechTreeTree.CreateTechTreeLines();
		uiTechTreeTree.TechTreeUpdate(editor: true);
	}

	private List<UITechTreeBox> DoFillBox()
	{
		List<UITechTreeBox> list = new List<UITechTreeBox>(GetComponentsInChildren<UITechTreeBox>());
		foreach (UITechTreeBox item in list)
		{
			if (!(item == null))
			{
				item.name = item.techCode;
				item.shape = item.GetShape();
				item.shape.SetText(item.techCode);
			}
		}
		return list;
	}
}
