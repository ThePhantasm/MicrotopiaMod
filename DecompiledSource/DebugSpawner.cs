using UnityEngine;

public class DebugSpawner : MonoBehaviour
{
	[Header("Pickups")]
	public PickupType pickupToSpawn;

	public bool spawnPickup;

	[Header("Ants")]
	public AntCaste antToSpawn;

	public bool spawnAnt;

	[Header("Status Effects")]
	public StatusEffect statusEffect;

	public bool giveToSelectedAnts;

	[Header("Other random debug")]
	public bool lodsEnabled;

	public int targetFrameRate;

	public bool setFrameRate;

	private bool _lodsEnabled;

	private float lodBias;

	private void Awake()
	{
		_lodsEnabled = (lodsEnabled = true);
		lodBias = QualitySettings.lodBias;
		targetFrameRate = -1;
	}
}
