using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "AudioLinks", menuName = "Microtopia/AudioLinks", order = 0)]
public class AudioLinks : ScriptableObject
{
	public static AudioLinks standard;

	[Header("UI")]
	[SerializeField]
	[Tooltip("When UIBuild tab is clicked")]
	private AudioClip buildingMenuTabClick;

	[SerializeField]
	[Tooltip("When UIBuild button is clicked")]
	private AudioClip buildingMenuButtonClick;

	[SerializeField]
	[Tooltip("When an instinct item gets done (may happen again after getting undone)")]
	private AudioClip instinctItemDone;

	[SerializeField]
	[Tooltip("When the player clicks the instinct complete button")]
	private AudioClip instinctComplete;

	[SerializeField]
	[Tooltip("Using rightclick or Esc during trail drawing")]
	private AudioClip trailDeselect;

	[SerializeField]
	private AudioClip developShimmer;

	[SerializeField]
	private AudioClip developShimmerGyne;

	[SerializeField]
	private AudioClip techHover;

	[SerializeField]
	private AudioClip techClick1;

	[SerializeField]
	private AudioClip techClick2;

	[SerializeField]
	private AudioClip techClick3;

	[SerializeField]
	private AudioClip techLine;

	[SerializeField]
	private AudioClip techIdeaOn;

	[SerializeField]
	private AudioClip techIdeaOff;

	[SerializeField]
	private AudioClip menuButtonClick;

	[SerializeField]
	private AudioClip menuButtonHover;

	[SerializeField]
	private AudioClip islandAppearCamWoosh;

	[SerializeField]
	private AudioClip islandLoading;

	[SerializeField]
	private AudioClip mapModeActivate;

	[SerializeField]
	private AudioClip mapModeDeactivate;

	[SerializeField]
	private AudioClip trackedBuildingComplete;

	[Header("UI 3D")]
	[SerializeField]
	[Tooltip("Selecting ant with click or during rect select")]
	private AudioClip antSelect;

	[SerializeField]
	[Tooltip("Selecting building/queen with click")]
	private AudioClip buildingSelect;

	[SerializeField]
	[Tooltip("Selecting biome object with click")]
	private AudioClip biomeObjectSelect;

	[SerializeField]
	[Tooltip("During trail drawing, when action points appear or disappear")]
	private AudioClip trailActionPointAppear;

	[SerializeField]
	[Tooltip("During trail drawing, when action points appear or disappear")]
	private AudioClip trailActionPointDisappear;

	[SerializeField]
	[Tooltip("When clicking to place trail (closed = ending at trail so stop drawing)")]
	private AudioClip trailPlace;

	[SerializeField]
	[Tooltip("When clicking to place trail (closed = ending at trail so stop drawing)")]
	private AudioClip trailPlaceClosed;

	[SerializeField]
	[Tooltip("When deleting trails, each time a piece of trail gets deleted")]
	private AudioClip trailDelete;

	[SerializeField]
	[Tooltip("A short sound that gets repeated during trail drawing, faster when moving faster")]
	private AudioClip trailDrawing;

	[SerializeField]
	[Tooltip("During trail drawing, When going from 'no obstructions' to 'one or more obstructions'")]
	private AudioClip trailObstructed;

	[SerializeField]
	[Tooltip("'Trrrr' during mouse rotation")]
	private AudioClip buildingRotate;

	[SerializeField]
	[Tooltip("When placing a building")]
	private AudioClip buildingPlace;

	[SerializeField]
	[Tooltip("When deleting a building")]
	private AudioClip buildingDelete;

	[SerializeField]
	[Tooltip("When a building is complete")]
	private AudioClip buildingComplete;

	[SerializeField]
	private AudioClip copySettings;

	[SerializeField]
	private AudioClip pasteSettings;

	[Header("World")]
	[SerializeField]
	[Tooltip("When an ant gets a pickup, from ground or building")]
	private AudioClip antPickup;

	[SerializeField]
	[Tooltip("When an ant gets a pickup, from ground or building")]
	private AudioClip antPickupEnergyPod;

	[SerializeField]
	[Tooltip("When an ant gets a pickup, from ground or building")]
	private AudioClip antPickupLarva;

	[SerializeField]
	[Tooltip("When an ant gets a pickup, from ground or building")]
	private AudioClip antPickupIronRaw;

	[SerializeField]
	[Tooltip("When an ant gets a pickup, from ground or building")]
	private AudioClip antPickupIronBar;

	[SerializeField]
	[Tooltip("When an ant gets a pickup, from ground or building")]
	private AudioClip antPickupCopperRaw;

	[SerializeField]
	[Tooltip("When an ant gets a pickup, from ground or building")]
	private AudioClip antPickupCopperBar;

	[SerializeField]
	[Tooltip("When an ant gets a pickup, from ground or building")]
	private AudioClip antPickupFiber;

	[SerializeField]
	[Tooltip("When an ant gets a pickup by foraging/cutting/mining")]
	private AudioClip antForage;

	[SerializeField]
	[Tooltip("When an ant gets a pickup by foraging/cutting/mining")]
	private AudioClip antPlantCut;

	[SerializeField]
	[Tooltip("When an ant gets a pickup by foraging/cutting/mining")]
	private AudioClip antMineRetrieve;

	[SerializeField]
	[Tooltip("When an ant drops a pickup, on ground or in building")]
	private AudioClip antDrop;

	[SerializeField]
	[Tooltip("When an ant drops a pickup, on ground or in building")]
	private AudioClip antDropEnergyPod;

	[SerializeField]
	[Tooltip("When an ant drops a pickup, on ground or in building")]
	private AudioClip antDropLarva;

	[SerializeField]
	[Tooltip("When an ant drops a pickup, on ground or in building")]
	private AudioClip antDropIronRaw;

	[SerializeField]
	[Tooltip("When an ant drops a pickup, on ground or in building")]
	private AudioClip antDropIronBar;

	[SerializeField]
	[Tooltip("When an ant drops a pickup, on ground or in building")]
	private AudioClip antDropCopperRaw;

	[SerializeField]
	[Tooltip("When an ant drops a pickup, on ground or in building")]
	private AudioClip antDropCopperBar;

	[SerializeField]
	[Tooltip("When an ant drops a pickup, on ground or in building")]
	private AudioClip antDropFiber;

	[SerializeField]
	[Tooltip("When an ant hops from queen")]
	private AudioClip antHop;

	[SerializeField]
	[Tooltip("When an ant dies")]
	private AudioClip antDie;

	[Tooltip("Is looped during (drone) ant flight, from launch to land")]
	public AudioLink antFlyLoop;

	[Tooltip("Is looped during ant mining")]
	public AudioLink antMineLoop;

	public float antMineLoopDelay;

	public AudioLink antInventorExplode;

	[Tooltip("Single audio that gets played when queen flies in")]
	public AudioLink queenFlyIn;

	[SerializeField]
	[Tooltip("When queen eats energy")]
	private AudioClip queenEat;

	[SerializeField]
	[Tooltip("When queen spawns a larva")]
	private AudioClip queenLarvaSpawn;

	[Tooltip("Plays on pickups that fly from stockpile to building")]
	public AudioLink pickupWooshLoop;

	[SerializeField]
	private AudioClip pickupWooshArrive;

	[Header("Music")]
	[SerializeField]
	private AudioClip menuMusic;

	[SerializeField]
	private AudioClip creditsMusic;

	[SerializeField]
	private List<AudioClip> nuptialFlightIntros;

	[SerializeField]
	private List<AudioClip> nuptialFlightMusic;

	[SerializeField]
	private List<BiomeAudio> biomesAudio;

	[SerializeField]
	private List<AudioClip> mapMusic;

	[SerializeField]
	private AudioClip techtreeMusic;

	private int random_clip_index;

	public static IEnumerator CInit()
	{
		AsyncOperationHandle<AudioLinks> loading = Addressables.LoadAssetAsync<AudioLinks>("ScriptableObjects/AudioLinks");
		yield return loading;
		standard = loading.Result;
		standard.Init();
	}

	private void Init()
	{
	}

	public AudioClip GetClip(UISfx ui_sfx)
	{
		return ui_sfx switch
		{
			UISfx.BuildingMenuTabClick => buildingMenuTabClick, 
			UISfx.BuildingMenuButtonClick => buildingMenuButtonClick, 
			UISfx.InstinctItemDone => instinctItemDone, 
			UISfx.InstinctComplete => instinctComplete, 
			UISfx.TrailDeselect => trailDeselect, 
			UISfx.NuptialFlightIntro => nuptialFlightIntros[Random.Range(0, nuptialFlightIntros.Count)], 
			UISfx.NuptialFlightTrack => nuptialFlightMusic[Random.Range(0, nuptialFlightMusic.Count)], 
			UISfx.DevelopShimmer => developShimmer, 
			UISfx.DevelopShimmerGyne => developShimmerGyne, 
			UISfx.TechHover => techHover, 
			UISfx.TechClick1Or2 => SelectRandomClip(techClick1, techClick2), 
			UISfx.TechClick3 => techClick3, 
			UISfx.TechIdeaEnable => techIdeaOn, 
			UISfx.TechIdeaDisable => techIdeaOff, 
			UISfx.MenuButtonClick => menuButtonClick, 
			UISfx.MenuButtonHover => menuButtonHover, 
			UISfx.IslandAppearCamWoosh => islandAppearCamWoosh, 
			UISfx.IslandLoading => islandLoading, 
			UISfx.MapModeActivate => mapModeActivate, 
			UISfx.MapModeDeactivate => mapModeDeactivate, 
			UISfx.BuildingDelete => buildingDelete, 
			UISfx.TrackedBuildingComplete => trackedBuildingComplete, 
			_ => null, 
		};
	}

	private AudioClip SelectRandomClip(params AudioClip[] clips)
	{
		if (random_clip_index >= clips.Length)
		{
			random_clip_index = 0;
		}
		return clips[random_clip_index++];
	}

	public AudioClip GetClip(UISfx3D ui_sfx_3d)
	{
		return ui_sfx_3d switch
		{
			UISfx3D.AntSelect => antSelect, 
			UISfx3D.BuildingSelect => buildingSelect, 
			UISfx3D.BiomeObjectSelect => biomeObjectSelect, 
			UISfx3D.TrailActionPointAppear => trailActionPointAppear, 
			UISfx3D.TrailActionPointDisappear => trailActionPointDisappear, 
			UISfx3D.TrailPlace => trailPlace, 
			UISfx3D.TrailPlaceClosed => trailPlaceClosed, 
			UISfx3D.TrailDelete => trailDelete, 
			UISfx3D.TrailDrawing => trailDrawing, 
			UISfx3D.TrailObstructed => trailObstructed, 
			UISfx3D.BuildingRotate => buildingRotate, 
			UISfx3D.BuildingPlace => buildingPlace, 
			UISfx3D.BuildingDelete => buildingDelete, 
			UISfx3D.BuildingComplete => buildingComplete, 
			UISfx3D.CopySettings => copySettings, 
			UISfx3D.PasteSettings => pasteSettings, 
			_ => null, 
		};
	}

	public AudioClip GetClip(WorldSfx world_sfx)
	{
		return world_sfx switch
		{
			WorldSfx.AntPickup => antPickup, 
			WorldSfx.AntPickupEnergyPod => antPickupEnergyPod, 
			WorldSfx.AntPickupLarva => antPickupLarva, 
			WorldSfx.AntPickupIronRaw => antPickupIronRaw, 
			WorldSfx.AntPickupIronBar => antPickupIronBar, 
			WorldSfx.AntPickupCopperRaw => antPickupCopperRaw, 
			WorldSfx.AntPickupCopperBar => antPickupCopperBar, 
			WorldSfx.AntPickupFiber => antPickupFiber, 
			WorldSfx.AntForage => antForage, 
			WorldSfx.AntPlantCut => antPlantCut, 
			WorldSfx.AntMineRetrieve => antMineRetrieve, 
			WorldSfx.AntDrop => antDrop, 
			WorldSfx.AntDropEnergyPod => antDropEnergyPod, 
			WorldSfx.AntDropLarva => antDropLarva, 
			WorldSfx.AntDropIronRaw => antDropIronRaw, 
			WorldSfx.AntDropIronBar => antDropIronBar, 
			WorldSfx.AntDropCopperRaw => antDropCopperRaw, 
			WorldSfx.AntDropCopperBar => antDropCopperBar, 
			WorldSfx.AntDropFiber => antDropFiber, 
			WorldSfx.AntHop => antHop, 
			WorldSfx.AntDie => antDie, 
			WorldSfx.QueenEat => queenEat, 
			WorldSfx.QueenLarvaSpawn => queenLarvaSpawn, 
			WorldSfx.PickupWooshArrive => pickupWooshArrive, 
			_ => null, 
		};
	}

	private BiomeAudio GetBiomeAudio(BiomeType biome_type)
	{
		foreach (BiomeAudio item in biomesAudio)
		{
			if (item.biomeType == biome_type)
			{
				return item;
			}
		}
		Debug.LogWarning($"AudioLinks: No biome audio for {biome_type}");
		return biomesAudio[0];
	}

	public AudioClip GetClipAmbience(BiomeType biome_type)
	{
		return GetBiomeAudio(biome_type).audioAmbience;
	}

	public AudioClip GetClipBiomeMusicBase(BiomeType biome_type)
	{
		return GetBiomeAudio(biome_type).audioBase;
	}

	public AudioClip GetClipBiomeMusicBusy(BiomeType biome_type)
	{
		return GetBiomeAudio(biome_type).audioBusy;
	}

	public AudioClip GetClipBiomeMusicPolluted(BiomeType biome_type)
	{
		return GetBiomeAudio(biome_type).audioPolluted;
	}

	public AudioClip GetClipIslandAppears(BiomeType biome_type)
	{
		return GetBiomeAudio(biome_type).audioIslandAppears;
	}

	public AudioClip GetClipMapMusic(int intensity)
	{
		return mapMusic[Mathf.Clamp(intensity, 0, mapMusic.Count - 1)];
	}

	public AudioClip GetClipMusic(MusicType music_type)
	{
		return music_type switch
		{
			MusicType.Menu => menuMusic, 
			MusicType.TechTree => techtreeMusic, 
			MusicType.Credits => creditsMusic, 
			_ => null, 
		};
	}
}
