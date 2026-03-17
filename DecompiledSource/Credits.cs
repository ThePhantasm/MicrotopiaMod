using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Credits : KoroutineBehaviour
{
	private class SpawnInfo
	{
		public float distToNext;

		public GameObject baseAntOb;

		public string str;

		public float width;

		public float speedVar;

		public SpawnInfo(GameObject _ob, string _str, float _dist, float _width = 0f, float _speed_var = 0f)
		{
			baseAntOb = _ob;
			str = _str;
			distToNext = _dist;
			width = _width;
			speedVar = _speed_var;
		}
	}

	public Biome biome;

	public int groundIndex;

	public int seed;

	public GameObject obSentry;

	public GameObject obAntHazmat;

	public GameObject obAntRegularT1;

	public GameObject obAntRegularT1High;

	public GameObject obAntSwarm;

	public GameObject obGyne;

	public GameObject obAntSmallT1;

	public GameObject obPrincess;

	public Transform tfStart;

	public Transform tfEnd;

	public float speed;

	public float distBetweenRoles = 10f;

	public float distBetweenRoleAndName = 20f;

	public float distBetweenLines = 50f;

	public float distBetweenSwarm = 4f;

	public float swarmWidth = 20f;

	public float waitAfterEnd = 10f;

	private string curSection;

	private List<SpawnInfo> spawnInfos;

	private bool creditsActive;

	private float nextSpawnTime;

	private List<CreditAnt> creditAnts;

	private void Start()
	{
		StartKoroutine(KInit());
	}

	private IEnumerator KInit()
	{
		KoroutineId kid = SetFinalizer();
		try
		{
			UIGlobal.instance.GoBlack(black: true, 0f);
			Ground ground = Ground.Create(biome, Vector3.zero, Quaternion.identity, groundIndex);
			yield return StartKoroutine(ground.KFill(null, seed, 0, biome.spawnUnlocker));
			yield return StartKoroutine(kid, ground.KGenerateEcology());
			Toolkit.ResetRandomState();
			yield return null;
			AudioManager.instance.Init();
			UIGlobal.instance.GoBlack(black: false, 0.5f);
			StartCredits();
		}
		finally
		{
			StopKoroutine(kid);
		}
	}

	private void StartCredits()
	{
		AudioManager.PlayMusic(MusicType.Credits);
		spawnInfos = new List<SpawnInfo>();
		AddHeader("CORDYCEPS_TITLE", obGyne);
		curSection = "CORDYCEPS";
		AddLines(4, 1, obAntSmallT1, obSentry);
		spawnInfos[^1].distToNext += distBetweenLines;
		AddHeader("GOBLINZ_TITLE", obPrincess);
		curSection = "GOBLINZ";
		AddLines(5);
		AddLine(obAntSmallT1, "GOBLINZ_5_0_0", distBetweenRoles);
		AddLine(obAntSmallT1, "GOBLINZ_5_0_1", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_5_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_6_0_0", distBetweenRoles);
		AddLine(obAntSmallT1, "GOBLINZ_6_0_1", distBetweenRoles);
		AddLine(obAntSmallT1, "GOBLINZ_6_0_2", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_6_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_7_0_0", distBetweenRoles);
		AddLine(obAntSmallT1, "GOBLINZ_7_0_1", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_7_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_8_0", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_8_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_9_0", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_9_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_10_0", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_10_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_11_0", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_11_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_12_0", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_12_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_13_0", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_13_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_14_0", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_14_1", distBetweenLines);
		AddLine(obAntSmallT1, "GOBLINZ_15_0", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "GOBLINZ_15_1", distBetweenLines);
		spawnInfos[^1].distToNext += distBetweenLines;
		AddHeader("GAMERA_TITLE", obPrincess);
		curSection = "GAMERA";
		List<string> list = new List<string>();
		for (int i = 0; i < 27; i++)
		{
			list.Add("GAMERA_" + i);
		}
		AddLineSwarm(list);
		spawnInfos[^1].distToNext += distBetweenLines;
		AddHeader("RIOTLOC_TITLE", obPrincess);
		curSection = "RIOTLOC";
		AddLine(obAntSmallT1, "RIOTLOC_S_0", distBetweenRoleAndName);
		AddLine(obAntRegularT1, "RIOTLOC_S_1", distBetweenLines);
		AddLines(4, 2);
		spawnInfos[^1].distToNext += distBetweenLines;
		AddHeader("TRANSPARKLES_TITLE", obPrincess);
		curSection = "TRANSPARKLES";
		AddLines(3, 2);
		spawnInfos[^1].distToNext += distBetweenLines;
		AddHeader("PLAYTESTERS_TITLE", obAntHazmat);
		List<string> list2 = new List<string>();
		for (int j = 0; j < 13; j++)
		{
			list2.Add("PLAYTESTERS_" + j);
		}
		AddLineSwarm(list2);
		nextSpawnTime = 0f;
		creditAnts = new List<CreditAnt>();
		creditsActive = true;
	}

	private void Update()
	{
		if (!creditsActive)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		for (int num = creditAnts.Count - 1; num >= 0; num--)
		{
			CreditAnt creditAnt = creditAnts[num];
			if (creditAnt.DoUpdate(deltaTime))
			{
				Object.Destroy(creditAnt.gameObject);
				creditAnts.RemoveAt(num);
			}
		}
		nextSpawnTime -= Time.deltaTime;
		if (nextSpawnTime < 0f)
		{
			if (spawnInfos.Count == 0)
			{
				ExitCredits();
				return;
			}
			SpawnInfo spawnInfo = spawnInfos[0];
			SpawnAnt(spawnInfo);
			spawnInfos.RemoveAt(0);
			if (spawnInfos.Count == 0)
			{
				nextSpawnTime = waitAfterEnd;
			}
			else
			{
				nextSpawnTime = spawnInfo.distToNext / speed;
			}
		}
		if (Input.anyKeyDown)
		{
			ExitCredits();
		}
	}

	private void AddHeader(string header_code, GameObject _ant)
	{
		spawnInfos.Add(new SpawnInfo(_ant, header_code, distBetweenLines));
	}

	private void AddLines(int n, int n_names = 1, GameObject ant_title = null, GameObject ant_name = null)
	{
		if (ant_title == null)
		{
			ant_title = obAntSmallT1;
		}
		if (ant_name == null)
		{
			ant_name = obAntRegularT1;
		}
		for (int i = 0; i < n; i++)
		{
			AddLine(ant_title, $"{curSection}_{i}_0", distBetweenRoleAndName);
			for (int j = 0; j < n_names; j++)
			{
				AddLine(ant_name, $"{curSection}_{i}_{j + 1}", distBetweenLines);
			}
		}
	}

	private void AddLine(GameObject ant, string code, float dist)
	{
		spawnInfos.Add(new SpawnInfo(ant, code, dist));
	}

	private void AddLineSwarm(List<string> name_codes)
	{
		for (int i = 0; i < name_codes.Count; i++)
		{
			spawnInfos.Add(new SpawnInfo(obAntSwarm, name_codes[i], distBetweenSwarm, swarmWidth, 0.2f));
		}
		spawnInfos[^1].distToNext = distBetweenLines;
	}

	private void SpawnAnt(SpawnInfo spawn_info)
	{
		Vector3 position = tfStart.position;
		Vector3 position2 = tfEnd.position;
		Vector3 normalized = (position2 - position).normalized;
		float magnitude = (position2 - position).magnitude;
		float num = (Random.value - 0.5f) * spawn_info.width;
		float num2 = speed * (1f + spawn_info.speedVar * Random.value);
		Vector3 pos = position - num * new Vector3(normalized.z, 0f, 0f - normalized.x);
		CreditAnt component = Object.Instantiate(spawn_info.baseAntOb).GetComponent<CreditAnt>();
		component.Init(spawn_info.str, pos, normalized, num2, magnitude);
		creditAnts.Add(component);
	}

	private void ExitCredits()
	{
		creditsActive = false;
		for (int num = creditAnts.Count - 1; num >= 0; num--)
		{
			creditAnts[num].Stop();
		}
		StartCoroutine(CExitCredits());
	}

	private IEnumerator CExitCredits()
	{
		UIGlobal.instance.GoBlack(black: true, 0.5f);
		yield return new WaitForSeconds(0.5f);
		GlobalGameState.GoToMainMenu();
	}
}
