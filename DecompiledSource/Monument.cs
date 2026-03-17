using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monument : Building
{
	[Header("Monument")]
	[SerializeField]
	private MonumentFunction function;

	[SerializeField]
	private List<string> biomesToReveal;

	[SerializeField]
	private MonumentAchievement achievement;

	private bool hasRevealed;

	[Space(10f)]
	[SerializeField]
	private bool onePerIsland;

	[SerializeField]
	private List<string> allowedBiomes = new List<string>();

	private bool doAnimation;

	private Coroutine cRevealIslandDelay;

	private string targetBiome = "";

	[SerializeField]
	private AudioLink audioActiveLoop;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(hasRevealed);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		if (save.version >= 61)
		{
			hasRevealed = save.ReadBool();
		}
		else
		{
			hasRevealed = false;
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		if ((function == MonumentFunction.REVEAL_ISLAND || function == MonumentFunction.REVEAL_ISLAND_CHOICE) && hasRevealed && anim != null)
		{
			anim.SetBool("DoAction", value: true);
		}
	}

	protected override void OnComplete()
	{
		switch (achievement)
		{
		case MonumentAchievement.THUMPER:
			Platform.current.GainAchievement(Achievement.THUMPER);
			break;
		case MonumentAchievement.PUFFER:
			Platform.current.GainAchievement(Achievement.PUFFER);
			break;
		}
		base.OnComplete();
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (!runWorld || currentStatus != BuildingStatus.COMPLETED)
		{
			return;
		}
		bool flag = false;
		if (function == MonumentFunction.START_NUPTIALFLIGHT)
		{
			flag = NuptialFlight.IsNuptialFlightActive();
		}
		if (doAnimation != flag)
		{
			doAnimation = flag;
			anim.SetBool(ClickableObject.paramDoAction, doAnimation);
			if (doAnimation)
			{
				StartLoopAudio(audioActiveLoop);
			}
			else
			{
				StopAudio();
			}
		}
	}

	private IEnumerator CRevealIslandDelay()
	{
		if (anim != null)
		{
			anim.SetBool("DoAction", value: true);
		}
		yield return new WaitForSeconds(3f);
		if (!hasRevealed)
		{
			if (targetBiome == "")
			{
				targetBiome = biomesToReveal[Random.Range(0, biomesToReveal.Count)];
			}
			GameManager.instance.AddBiome(targetBiome);
			hasRevealed = true;
		}
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		switch (function)
		{
		case MonumentFunction.START_NUPTIALFLIGHT:
			ui_hover.SetButtonWithText(delegate
			{
				NuptialFlight.StartFlight();
				Gameplay.instance.Select(null);
			}, clear_on_click: true, Loc.GetUI("BUILDING_USE_THUMPER"));
			break;
		case MonumentFunction.REVEAL_ISLAND:
			ui_hover.SetButtonWithText(delegate
			{
				if (!hasRevealed && cRevealIslandDelay == null)
				{
					cRevealIslandDelay = StartCoroutine(CRevealIslandDelay());
					Gameplay.instance.Select(null);
				}
			}, clear_on_click: true, Loc.GetUI("MONUMENT_SCAN"));
			break;
		}
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
		bool flag = false;
		string txt = "";
		switch (function)
		{
		case MonumentFunction.START_NUPTIALFLIGHT:
			txt = Loc.GetUI("BUILDING_USE_THUMPER");
			if (NuptialFlight.GetCurrentStage() != NuptialFlightStage.WARM_UP && NuptialFlight.GetCurrentStage() != NuptialFlightStage.ACTIVE && NuptialFlight.GetCurrentStage() != NuptialFlightStage.FLY_OFF)
			{
				flag = true;
			}
			break;
		case MonumentFunction.REVEAL_ISLAND:
			txt = Loc.GetUI("MONUMENT_SCAN");
			if (!hasRevealed && cRevealIslandDelay == null)
			{
				flag = true;
			}
			break;
		}
		ui_hover.UpdateButtonWithText(txt, flag);
	}

	public override UIClickType GetUiClickType_Intake()
	{
		if (function == MonumentFunction.REVEAL_ISLAND_CHOICE && !hasRevealed)
		{
			return UIClickType.ISLAND_SCANNER_CHOICE;
		}
		return UIClickType.BUILDING_SMALL;
	}

	public override void SetClickUi_Intake(UIClickLayout_Building ui_building)
	{
		base.SetClickUi_Intake(ui_building);
		if (function == MonumentFunction.REVEAL_ISLAND_CHOICE && !hasRevealed)
		{
			ui_building.SetInfo(Loc.GetUI("MONUMENT_SELECT_ISLAND"));
			ui_building.SetButton(UIClickButtonType.Blue, delegate
			{
				if (!hasRevealed && cRevealIslandDelay == null)
				{
					targetBiome = "BiomeBlue2";
					cRevealIslandDelay = StartCoroutine(CRevealIslandDelay());
					Gameplay.instance.Select(null);
				}
			}, InputAction.None);
			ui_building.UpdateButton(UIClickButtonType.Blue, enabled: true, Loc.GetObject("BIOME_BLUE"));
			ui_building.SetButton(UIClickButtonType.Desert, delegate
			{
				if (!hasRevealed && cRevealIslandDelay == null)
				{
					targetBiome = "BiomeScrapara";
					cRevealIslandDelay = StartCoroutine(CRevealIslandDelay());
					Gameplay.instance.Select(null);
				}
			}, InputAction.None);
			ui_building.UpdateButton(UIClickButtonType.Desert, enabled: true, Loc.GetObject("BIOME_DESERT"));
			ui_building.SetButton(UIClickButtonType.Jungle, delegate
			{
				if (!hasRevealed && cRevealIslandDelay == null)
				{
					targetBiome = "BiomeGreen";
					cRevealIslandDelay = StartCoroutine(CRevealIslandDelay());
					Gameplay.instance.Select(null);
				}
			}, InputAction.None);
			ui_building.UpdateButton(UIClickButtonType.Jungle, enabled: true, Loc.GetObject("BIOME_JUNGLE"));
			ui_building.SetButton(UIClickButtonType.Toxic, delegate
			{
				if (!hasRevealed && cRevealIslandDelay == null)
				{
					targetBiome = "BiomeToxicwaste";
					cRevealIslandDelay = StartCoroutine(CRevealIslandDelay());
					Gameplay.instance.Select(null);
				}
			}, InputAction.None);
			ui_building.UpdateButton(UIClickButtonType.Toxic, enabled: true, Loc.GetObject("BIOME_TOXIC"));
			ui_building.SetButton(UIClickButtonType.Concrete, delegate
			{
				if (!hasRevealed && cRevealIslandDelay == null)
				{
					targetBiome = "BiomeConcrete";
					cRevealIslandDelay = StartCoroutine(CRevealIslandDelay());
					Gameplay.instance.Select(null);
				}
			}, InputAction.None);
			ui_building.UpdateButton(UIClickButtonType.Concrete, enabled: true, Loc.GetObject("BIOME_CONCRETE"));
			return;
		}
		switch (function)
		{
		case MonumentFunction.START_NUPTIALFLIGHT:
			ui_building.SetButton(UIClickButtonType.Generic1, delegate
			{
				NuptialFlight.StartFlight();
				Gameplay.instance.Select(null);
			}, InputAction.None);
			break;
		case MonumentFunction.REVEAL_ISLAND:
			ui_building.SetButton(UIClickButtonType.Generic1, delegate
			{
				if (!hasRevealed && cRevealIslandDelay == null)
				{
					cRevealIslandDelay = StartCoroutine(CRevealIslandDelay());
					Gameplay.instance.Select(null);
				}
			}, InputAction.None);
			break;
		}
	}

	public override void UpdateClickUi_Intake(UIClickLayout ui_click)
	{
		base.UpdateClickUi_Intake(ui_click);
		if (function == MonumentFunction.REVEAL_ISLAND_CHOICE && !hasRevealed)
		{
			return;
		}
		bool flag = false;
		string txt = "";
		switch (function)
		{
		case MonumentFunction.START_NUPTIALFLIGHT:
			txt = Loc.GetUI("BUILDING_USE_THUMPER");
			if (NuptialFlight.GetCurrentStage() != NuptialFlightStage.WARM_UP && NuptialFlight.GetCurrentStage() != NuptialFlightStage.ACTIVE && NuptialFlight.GetCurrentStage() != NuptialFlightStage.FLY_OFF)
			{
				flag = true;
			}
			break;
		case MonumentFunction.REVEAL_ISLAND:
			txt = Loc.GetUI("MONUMENT_SCAN");
			if (!hasRevealed && cRevealIslandDelay == null)
			{
				flag = true;
			}
			break;
		}
		ui_click.UpdateButton(UIClickButtonType.Generic1, flag, txt);
	}

	public override bool CanBeBuildOnGround(Ground _ground, bool is_relocating, ref string error)
	{
		if (!is_relocating && onePerIsland && _ground.GetBuildingCount(data.code) > 0)
		{
			error = Loc.GetUI("BUILDING_ERROR_ONEPERISLAND");
			return false;
		}
		if (allowedBiomes.Count == 0)
		{
			return true;
		}
		foreach (string allowedBiome in allowedBiomes)
		{
			if (_ground.biomeAddress.Contains(allowedBiome) || _ground.biomeAddress == "Biomes/" + allowedBiome)
			{
				return true;
			}
		}
		if (allowedBiomes.Count > 1)
		{
			error = Loc.GetUI("BUILDING_ERROR_ISLAND");
			return false;
		}
		switch (allowedBiomes[0])
		{
		case "BiomeBlue2_start":
			error = Loc.GetUI("BUILDING_ERROR_START");
			break;
		case "BiomeBlue2":
			error = Loc.GetUI("BUILDING_ERROR_BLUE");
			break;
		case "BiomeScrapara":
			error = Loc.GetUI("BUILDING_ERROR_DESERT");
			break;
		case "BiomeGreen":
			error = Loc.GetUI("BUILDING_ERROR_JUNGLE");
			break;
		case "BiomeToxicwaste":
			error = Loc.GetUI("BUILDING_ERROR_TOXIC");
			break;
		case "BiomeConcrete":
			error = Loc.GetUI("BUILDING_ERROR_CONCRETE");
			break;
		default:
			Debug.LogError(base.name + ": Don't know biome adress " + allowedBiomes[0]);
			break;
		}
		return false;
	}
}
