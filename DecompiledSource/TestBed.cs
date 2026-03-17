using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TestBed : KoroutineBehaviour
{
	public static TestBed instance;

	[Header("The biome to view; a new one can be added with Assets > Create > Microtopia > Biome (or copy an existing one)")]
	public Biome biome;

	[Tooltip("If > 0, biome generation uses this seed")]
	public int useSeed;

	[Tooltip("If >= 0, the ground prefab with this index is selected (from biome ground prefab list)")]
	public int groundIndex = -1;

	public TMP_Text textInfo;

	[NonSerialized]
	public Ground ground;

	protected bool blockInput;

	private bool initDone;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		StartCoroutine(CStart());
	}

	protected virtual IEnumerator CStart()
	{
		string text = "";
		if (biome == null)
		{
			text = "Fill a biome in Main and try again";
		}
		else if (biome.groundPrefabs == null || biome.groundPrefabs.Count == 0)
		{
			text = "Set a ground prefab in the biome and try again";
		}
		if (text != "")
		{
			textInfo.text = text;
			base.enabled = false;
			yield break;
		}
		textInfo.text = "Loading resources...";
		foreach (var item2 in biome.EDistributions())
		{
			Distribution item = item2.Item1;
			item.checksumPrev = item.Checksum();
		}
		Platform.Select();
		yield return StartKoroutine(PrefabData.KInit(this, for_test_bed: true));
		yield return StartCoroutine(DebugSettings.CInit());
		yield return StartCoroutine(GlobalValues.CInit());
		InputManager.Init();
		Generate();
	}

	protected void Generate()
	{
		if (ground != null)
		{
			ground.Delete();
		}
		StartCoroutine(CGenerate());
	}

	protected virtual IEnumerator CGenerate()
	{
		textInfo.text = "Generating...";
		blockInput = true;
		yield return null;
		int generation_seed = (int)((useSeed <= 0) ? DateTime.Now.Ticks : useSeed);
		ground = Ground.Create(biome, Vector3.zero, Toolkit.RandomYRotation(), groundIndex);
		yield return StartKoroutine(ground.KFill(null, generation_seed, 0, biome.spawnUnlocker));
		yield return null;
		blockInput = false;
		textInfo.text = "";
		initDone = true;
	}

	private void Update()
	{
		if (initDone)
		{
			Process();
		}
	}

	protected virtual void Process()
	{
		InputManager.InputUpdate();
		CamController.instance.CamUpdate();
	}

	public BiomeObject SpawnBiomeObject(GameObject prefab, string code, Vector3 pos, Quaternion rot, Transform parent, float size)
	{
		BiomeObject component = UnityEngine.Object.Instantiate(prefab, pos, rot, parent).GetComponent<BiomeObject>();
		component.code = code;
		component.spawnSize = size;
		component.Init();
		return component;
	}

	public Pickup SpawnPickup(GameObject prefab, PickupType type, Vector3 pos, Quaternion rot, Transform parent)
	{
		Pickup component = UnityEngine.Object.Instantiate(prefab, pos, rot, parent).GetComponent<Pickup>();
		component.Fill(type);
		component.GetMesh();
		return component;
	}
}
