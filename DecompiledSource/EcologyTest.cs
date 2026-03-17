using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class EcologyTest : TestBed
{
	private class PollutionSource
	{
		public GameObject ob;

		public Vector3 pos;

		public float pollution;

		public PollutionSource(GameObject ob, Vector3 pos, float pollution)
		{
			this.ob = ob;
			this.pos = pos;
			this.pollution = pollution;
		}
	}

	private enum EditMode
	{
		Pollution,
		Harvest
	}

	[SerializeField]
	private TMP_Text textTime;

	[SerializeField]
	private TMP_Text textPollution;

	[SerializeField]
	private TMP_Text textPlants;

	[SerializeField]
	private GameObject pfPollutionSource;

	[SerializeField]
	private float addPollution;

	private Ecology ecology;

	private List<PlantType> plantTypesReadyToInvade = new List<PlantType>();

	private bool paused;

	private float worldSpeed = 1f;

	private EditMode editMode;

	private List<PollutionSource> pollutionSources = new List<PollutionSource>();

	private bool inited;

	protected override IEnumerator CStart()
	{
		textPollution.text = "";
		textPlants.text = "";
		yield return StartCoroutine(base.CStart());
	}

	protected override IEnumerator CGenerate()
	{
		yield return StartCoroutine(base.CGenerate());
		ecology = new Ecology();
		ecology.Init(ground);
		int stuck = 0;
		ecology.Generate(GetGlobalPollution(), ref stuck);
		inited = true;
	}

	protected override void Process()
	{
		base.Process();
		if (!inited)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
		{
			worldSpeed *= 0.5f;
		}
		if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
		{
			worldSpeed *= 2f;
		}
		if (Input.GetKeyDown(KeyCode.Space))
		{
			paused = !paused;
		}
		if (Input.GetKeyDown(KeyCode.I) && plantTypesReadyToInvade.Count > 0)
		{
			PlantType plantType = plantTypesReadyToInvade[Random.Range(0, plantTypesReadyToInvade.Count)];
			ecology.AddNewSpecies(plantType);
			plantTypesReadyToInvade.Remove(plantType);
		}
		Vector3? screenPosAtZero = CamController.instance.GetScreenPosAtZero(Input.mousePosition);
		if (screenPosAtZero.HasValue)
		{
			switch (editMode)
			{
			case EditMode.Pollution:
				if (Input.GetMouseButtonDown(0))
				{
					SpawnPollutionSource(screenPosAtZero.Value);
				}
				if (Input.GetMouseButtonDown(1))
				{
					PollutionSource pollutionSource = FindClosestPollutionSource(screenPosAtZero.Value);
					if (pollutionSource != null)
					{
						pollutionSources.Remove(pollutionSource);
						Object.Destroy(pollutionSource.ob);
					}
				}
				break;
			case EditMode.Harvest:
				if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
				{
					Plant plant = ecology.FindClosestPlant(screenPosAtZero.Value);
					if (plant != null)
					{
						plant.SetState(PlantState.Dead);
					}
				}
				break;
			}
		}
		if (!paused)
		{
			ecology.Update(GetGlobalPollution(), Time.deltaTime * worldSpeed);
		}
		UpdateUI();
	}

	private void UpdateUI()
	{
		if (paused)
		{
			textTime.text = "PAUSED";
		}
		else
		{
			textTime.text = $"World speed: {worldSpeed * 100f: 0}%";
		}
		textPollution.text = $"Pollution: {GetGlobalPollution()}";
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Plant types:");
		ecology.AppendSpeciesAmountInfo(stringBuilder);
		if (plantTypesReadyToInvade.Count > 0)
		{
			stringBuilder.AppendLine("Ready to invade (press I):");
			foreach (PlantType item in plantTypesReadyToInvade)
			{
				stringBuilder.AppendLine($" - {item}");
			}
		}
		textPlants.text = stringBuilder.ToString();
	}

	private float GetGlobalPollution()
	{
		float num = 0f;
		foreach (PollutionSource pollutionSource in pollutionSources)
		{
			num += pollutionSource.pollution;
		}
		return num;
	}

	private PollutionSource FindClosestPollutionSource(Vector3 pos)
	{
		float num = float.MaxValue;
		PollutionSource result = null;
		foreach (PollutionSource pollutionSource in pollutionSources)
		{
			float sqrMagnitude = (pollutionSource.pos - pos).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = pollutionSource;
			}
		}
		return result;
	}

	private void SpawnPollutionSource(Vector3 pos)
	{
		GameObject gameObject = Object.Instantiate(pfPollutionSource);
		gameObject.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Random.value * 360f, 0f));
		pollutionSources.Add(new PollutionSource(gameObject, pos, addPollution));
	}
}
