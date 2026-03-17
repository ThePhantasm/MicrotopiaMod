using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class SheetReader
{
	public static XmlDocument GetXmlDoc(string xml_file)
	{
		byte[] buffer;
		try
		{
			buffer = File.ReadAllBytes(xml_file);
		}
		catch (IOException ex)
		{
			Debug.LogError("GetXmlDoc " + xml_file + ": " + ex.Message);
			return null;
		}
		MemoryStream inStream = new MemoryStream(buffer);
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			xmlDocument.Load(inStream);
			return xmlDocument;
		}
		catch (Exception ex2)
		{
			Debug.LogError("Error reading " + xml_file + ":\r\n" + ex2.Message);
			return null;
		}
	}

	public static IEnumerable<SheetRow> ERead(XmlDocument xml_doc, int start_x = 0, int start_y = 0, int end_x = int.MaxValue, int end_y = int.MaxValue)
	{
		return ERead(xml_doc, "", start_x, start_y, end_x, end_y);
	}

	public static IEnumerable<SheetRow> ERead(XmlDocument xml_doc, string sheet, int start_x = 0, int start_y = 0, int end_x = int.MaxValue, int end_y = int.MaxValue)
	{
		XmlNode xmlNode = ((!(sheet == "")) ? FindNodeName(xml_doc, "table:table", "table:name", sheet) : FindNodeName(xml_doc, "table:table"));
		if (xmlNode == null)
		{
			string text = "";
			if (sheet != "")
			{
				text = " with property 'table:name' of '" + sheet + "'";
			}
			Debug.LogError("Couldn't find node 'table:table' in xml '" + xml_doc.Name + "'" + text);
			yield break;
		}
		int r = -1;
		string[] columns = null;
		int rows_empty = 0;
		foreach (XmlNode childNode in xmlNode.ChildNodes)
		{
			if (childNode.Name != "table:table-row")
			{
				continue;
			}
			r++;
			if (r < start_y)
			{
				continue;
			}
			if (r > end_y)
			{
				break;
			}
			int num = -1;
			if (r == start_y)
			{
				List<string> list = new List<string>();
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					num++;
					if (num < start_x)
					{
						continue;
					}
					if (num > end_x)
					{
						break;
					}
					XmlNode xmlNode3 = FindNodeName(childNode2, "text:p");
					string text2 = "";
					if (xmlNode3 == null)
					{
						if (end_x == int.MaxValue)
						{
							end_x = num - 1;
							break;
						}
						Debug.LogError("No column name found for column " + num);
					}
					else
					{
						text2 = xmlNode3.InnerText.Trim().ToLowerInvariant();
						if (text2 == "-")
						{
							text2 = "";
						}
					}
					if (list.Contains(text2) && text2 != "")
					{
						Debug.LogError("Column '" + text2 + "' already exists in xml '" + xml_doc.Name + "'");
						text2 = "";
					}
					list.Add(text2);
				}
				columns = list.ToArray();
				continue;
			}
			SheetRow sheetRow = new SheetRow();
			bool flag = true;
			foreach (XmlNode childNode3 in childNode.ChildNodes)
			{
				int result = 1;
				XmlNode xmlNode5 = childNode3.Attributes["table:number-columns-repeated"];
				if (xmlNode5 != null && !int.TryParse(xmlNode5.InnerText, out result))
				{
					result = 1;
				}
				for (int i = 0; i < result; i++)
				{
					num++;
					if (num < start_x)
					{
						continue;
					}
					if (num > end_x)
					{
						break;
					}
					XmlNode xmlNode6 = FindNodeName(childNode3, "text:p");
					string text3 = "";
					if (xmlNode6 != null)
					{
						text3 = xmlNode6.InnerText.Trim();
					}
					int num2 = num - start_x;
					if (columns[num2] != "")
					{
						if (text3 != "")
						{
							flag = false;
							rows_empty = 0;
							sheetRow.Add(columns[num2], text3);
						}
						else
						{
							sheetRow.Add(columns[num2], null);
						}
					}
				}
			}
			if (flag)
			{
				rows_empty++;
				if (rows_empty > 2)
				{
					yield break;
				}
			}
			else
			{
				yield return sheetRow;
			}
		}
	}

	private static XmlNode FindNodeName(XmlNode node, string node_name, string with_prop = "", string with_prop_val = "")
	{
		if (node.Name == node_name)
		{
			if (with_prop == "")
			{
				return node;
			}
			XmlNode xmlNode = node.Attributes[with_prop];
			if (xmlNode != null && xmlNode.InnerText == with_prop_val)
			{
				return node;
			}
		}
		foreach (XmlNode childNode in node.ChildNodes)
		{
			XmlNode xmlNode2 = FindNodeName(childNode, node_name, with_prop, with_prop_val);
			if (xmlNode2 != null)
			{
				return xmlNode2;
			}
		}
		return null;
	}
}
