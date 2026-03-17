using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BiomeViewer : TestBed
{
	private enum OverlayType
	{
		None,
		Distribution,
		AreaBlocks,
		MAX
	}

	public TMP_Text textSpawned;

	public RectTransform panelTextSpawned;

	public Toggle togShowFog;

	public Toggle togShowTextSpawned;

	public Material matOverlay;

	private Transform shownOverlayParent;

	private BiomeArea shownArea;

	private BiomeElement shownElement;

	private OverlayType overlayType;

	private bool distrOverlayCombined;

	private float checksumAreasPrev;

	private bool hideFog;

	protected override IEnumerator CStart()
	{
		textSpawned.SetText("");
		panelTextSpawned.SetObActive(active: false);
		togShowFog.SetObActive(active: false);
		togShowTextSpawned.SetObActive(active: false);
		togShowFog.SetIsOnWithoutNotify(value: true);
		if (biome != null)
		{
			checksumAreasPrev = GetAreasChecksum();
		}
		Lighting.instance.Init();
		yield return StartCoroutine(base.CStart());
		togShowFog.SetObActive(active: true);
		togShowTextSpawned.SetObActive(active: true);
		if (shownArea != null || shownElement != null)
		{
			ShowOverlay();
		}
		yield return null;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Generation seed: {ground.generationSeed}");
		GUIUtility.systemCopyBuffer = ground.generationSeed.ToString();
		foreach (BiomeArea area in biome.areas)
		{
			foreach (BiomeElement element in area.elements)
			{
				stringBuilder.AppendLine($" - {element.element}:  \t{((element.spawned != null) ? element.spawned.Count : 0)}");
			}
		}
		textSpawned.SetText(stringBuilder.ToString());
		UpdateInfo();
	}

	protected override IEnumerator CGenerate()
	{
		yield return StartCoroutine(base.CGenerate());
		yield return StartKoroutine(ground.KGenerateEcology());
		if (shownArea != null || shownElement != null)
		{
			ShowOverlay();
		}
		yield return null;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Generation seed: {ground.generationSeed}");
		GUIUtility.systemCopyBuffer = ground.generationSeed.ToString();
		foreach (BiomeArea area in biome.areas)
		{
			foreach (BiomeElement element in area.elements)
			{
				stringBuilder.AppendLine($" - {element.element}:  \t{element.spawned.Count}");
			}
		}
		textSpawned.SetText(stringBuilder.ToString());
		UpdateInfo();
	}

	private float GetAreasChecksum()
	{
		float num = 0f;
		foreach (BiomeArea area in biome.areas)
		{
			num += area.blockThreshold + area.disabled.Checksum() + area.showColor.Checksum();
		}
		return num;
	}

	private void UpdateInfo()
	{
		string text;
		switch (overlayType)
		{
		case OverlayType.None:
			text = "No overlay";
			break;
		case OverlayType.Distribution:
			text = "Distribution overlay" + (distrOverlayCombined ? " (combined)" : "") + ": ";
			text = ((!distrOverlayCombined) ? ((shownElement == null) ? ((shownArea == null) ? (text + "<none selected>") : (text + shownArea.name)) : (text + $"{shownElement.element} (in area {shownElement.area.name})")) : ((shownArea == null || shownElement == null) ? (text + "<none selected>") : (text + $"area {shownArea.name}, element {shownElement.element}")));
			break;
		case OverlayType.AreaBlocks:
			text = "Area block overlay";
			break;
		default:
			text = "??";
			break;
		}
		textInfo.text = "Space: regenerate, 1: change overlay" + ((overlayType == OverlayType.Distribution) ? ", 2: toggle combine" : "") + "\n" + text;
	}

	protected override void Process()
	{
		base.Process();
		if (blockInput)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Generate();
		}
		bool flag = false;
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			overlayType++;
			if (overlayType >= OverlayType.MAX)
			{
				overlayType = OverlayType.None;
			}
			flag = true;
		}
		if (overlayType == OverlayType.Distribution && Input.GetKeyDown(KeyCode.Alpha2))
		{
			distrOverlayCombined = !distrOverlayCombined;
			flag = true;
		}
		if (flag)
		{
			if ((shownArea == null) & (biome.areas.Count > 0))
			{
				SetArea(biome.areas[0]);
				SetElement(null);
			}
			if (distrOverlayCombined && shownElement == null && shownArea != null && shownArea.elements.Count > 0)
			{
				SetElement(shownArea.elements[0]);
			}
			UpdateInfo();
			ShowOverlay();
		}
		CheckChangedDistributions();
		CheckChangedLighting();
	}

	private void SetArea(BiomeArea area)
	{
		if (shownArea != area)
		{
			shownArea = area;
			UpdateInfo();
		}
	}

	private void SetElement(BiomeElement el)
	{
		if (shownElement != el)
		{
			shownElement = el;
			UpdateInfo();
		}
	}

	private void CheckChangedDistributions()
	{
		bool flag = false;
		foreach (var (distribution, biomeArea, biomeElement) in biome.EDistributions())
		{
			if (distribution.checksumPrev == distribution.Checksum() && !distribution.selectThis)
			{
				continue;
			}
			distribution.selectThis = false;
			switch (overlayType)
			{
			case OverlayType.Distribution:
				SetArea(biomeArea);
				if (distrOverlayCombined && biomeElement == null)
				{
					if (shownElement == null || shownElement.area != biomeArea)
					{
						SetElement((biomeArea.elements.Count == 0) ? null : biomeArea.elements[0]);
					}
				}
				else
				{
					SetElement(biomeElement);
				}
				flag = true;
				break;
			case OverlayType.AreaBlocks:
				flag = true;
				break;
			}
			distribution.checksumPrev = distribution.Checksum();
		}
		float areasChecksum = GetAreasChecksum();
		if (checksumAreasPrev != areasChecksum)
		{
			checksumAreasPrev = areasChecksum;
			if (overlayType == OverlayType.AreaBlocks)
			{
				flag = true;
			}
		}
		if (flag)
		{
			ShowOverlay();
		}
	}

	private void ClearOverlay()
	{
		if (shownOverlayParent != null)
		{
			Object.Destroy(shownOverlayParent.gameObject);
			shownOverlayParent = null;
		}
	}

	private void ShowOverlay()
	{
		ClearOverlay();
		switch (overlayType)
		{
		case OverlayType.None:
			return;
		case OverlayType.Distribution:
			if ((shownArea == null && shownElement == null) || (distrOverlayCombined && (shownArea == null || shownElement == null)))
			{
				return;
			}
			break;
		}
		int x = ground.gridSizeBiome.x;
		int y = ground.gridSizeBiome.y;
		Vector3 vector = ground.transform.position + ground.rectCorner;
		float num = 8f;
		float[,] array = new float[x, y];
		Color[,] array2 = new Color[x, y];
		Color color = new Color(0f, 0f, 0f, 0f);
		for (int i = 0; i < x; i++)
		{
			for (int j = 0; j < y; j++)
			{
				array2[i, j] = color;
			}
		}
		switch (overlayType)
		{
		case OverlayType.Distribution:
		{
			if (distrOverlayCombined)
			{
				shownElement.distribution.Fill(array, ground.surfaceFactor, keep_seed: true);
				float[,] array3 = new float[x, y];
				shownArea.distribution.Fill(array3, ground.surfaceFactor, keep_seed: true);
				Color col = new Color(1f, 1f, 0f);
				for (int m = 0; m < x; m++)
				{
					for (int n = 0; n < y; n++)
					{
						array2[m, n] = col.SetAlpha(array[m, n] * array3[m, n]);
					}
				}
				break;
			}
			Color col2;
			if (shownElement == null)
			{
				shownArea.distribution.Fill(array, ground.surfaceFactor, keep_seed: true);
				col2 = new Color(0.26f, 1f, 0.22f);
			}
			else
			{
				shownElement.distribution.Fill(array, ground.surfaceFactor, keep_seed: true);
				col2 = new Color(0f, 0.96f, 1f);
			}
			for (int num2 = 0; num2 < x; num2++)
			{
				for (int num3 = 0; num3 < y; num3++)
				{
					array2[num2, num3] = col2.SetAlpha(array[num2, num3]);
				}
			}
			break;
		}
		case OverlayType.AreaBlocks:
			foreach (BiomeArea area in biome.areas)
			{
				if (area.blockThreshold >= 1f)
				{
					continue;
				}
				area.distribution.Fill(array, ground.surfaceFactor, keep_seed: true);
				for (int k = 0; k < x; k++)
				{
					for (int l = 0; l < y; l++)
					{
						if (array[k, l] > area.blockThreshold && array2[k, l].a == 0f)
						{
							array2[k, l] = area.showColor;
						}
					}
				}
			}
			break;
		}
		shownOverlayParent = new GameObject("ShownDistrParent").transform;
		ProcMesh procMesh = new ProcMesh();
		Vector3 vector2 = ground.rectDir1 * num;
		Vector3 vector3 = ground.rectDir2 * num;
		for (int num4 = 0; num4 < x; num4++)
		{
			for (int num5 = 0; num5 < y; num5++)
			{
				Vector3 vector4 = vector + vector2 * num4 + vector3 * num5;
				procMesh.AddRect(vector4 + vector3, vector4 + vector2 + vector3, vector4 + vector2, vector4, array2[num4, num5]);
			}
		}
		procMesh.GenerateInNewObject("ProcMesh", new Vector3(0f, 0.2f, 0f), matOverlay).transform.parent = shownOverlayParent;
	}

	private void CheckChangedLighting()
	{
		if (biome.lighting.checksumPrev != biome.lighting.Checksum())
		{
			if (hideFog)
			{
				hideFog = false;
				togShowFog.SetIsOnWithoutNotify(value: true);
				UpdateInfo();
			}
			ShowLighting();
			biome.lighting.checksumPrev = biome.lighting.Checksum();
		}
	}

	private void ShowLighting()
	{
		Lighting.instance.Apply(biome.lighting, hideFog);
	}

	public void ToggleFog()
	{
		hideFog = !togShowFog.isOn;
		UpdateInfo();
		ShowLighting();
	}

	public void ToggleTextSpawned()
	{
		panelTextSpawned.SetObActive(togShowTextSpawned.isOn);
	}
}
