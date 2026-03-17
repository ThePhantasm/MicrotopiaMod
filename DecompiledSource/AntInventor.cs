using System.Collections.Generic;
using UnityEngine;

public class AntInventor : Ant
{
	[Header("Inventor")]
	public Transform insertPoint;

	public Transform currencyPoint;

	public SkinnedMeshRenderer reservoir;

	public Collider colReservoir;

	public AnimationCurve curve_Key1;

	public AnimationCurve curve_Key2;

	public ParticleSystem psExplosion;

	public Material matReservoir;

	public Material matReservoirCompleted;

	public float timeDeath;

	private float reservoirSize;

	private float targetReservoirSize;

	private float autoTargetReservoirSize;

	private float tDeath = float.MaxValue;

	private bool autoMode;

	private float autoTimer;

	private string currentRecipe = "";

	[SerializeField]
	private string debugRecipe = "";

	private Dictionary<PickupType, int> eatenPickups = new Dictionary<PickupType, int>();

	private bool debugFull;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write(currentRecipe);
		save.Write(eatenPickups.Count);
		if (eatenPickups.Count > 0)
		{
			foreach (KeyValuePair<PickupType, int> eatenPickup in eatenPickups)
			{
				save.Write((int)eatenPickup.Key);
				save.Write(eatenPickup.Value);
			}
		}
		save.Write(tDeath);
		save.Write(autoMode);
		save.Write(autoTimer);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		currentRecipe = save.ReadString();
		eatenPickups = new Dictionary<PickupType, int>();
		int num = save.ReadInt();
		for (int i = 0; i < num; i++)
		{
			eatenPickups.Add((PickupType)save.ReadInt(), save.ReadInt());
		}
		tDeath = save.ReadFloat();
		if (save.version >= 64)
		{
			autoMode = save.ReadBool();
			autoTimer = save.ReadFloat();
		}
		else
		{
			autoMode = false;
			autoTimer = 0f;
		}
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		reservoir.SetObActive(active: true);
		psExplosion.SetObActive(active: false);
		SetRecipe(currentRecipe, during_load);
		if (debugRecipe == "")
		{
			foreach (ResearchRecipeData researchRecipe in TechTree.researchRecipes)
			{
				if (researchRecipe.caste == data.caste && researchRecipe.energy > 0f)
				{
					debugRecipe = researchRecipe.code;
					break;
				}
			}
		}
		debugFull = !DebugSettings.standard.inventorsAlwaysFull;
	}

	public override void AntUpdate(float dt)
	{
		base.AntUpdate(dt);
		if (DebugSettings.standard.inventorsAlwaysFull && !debugFull)
		{
			debugFull = true;
			UpdateReservior();
		}
		if (!DebugSettings.standard.inventorsAlwaysFull && debugFull)
		{
			debugFull = false;
			UpdateReservior();
		}
		if (IsDead())
		{
			reservoir.SetObActive(active: false);
		}
		float num = (autoMode ? autoTargetReservoirSize : targetReservoirSize);
		if (reservoirSize != num)
		{
			reservoirSize = Mathf.Lerp(reservoirSize, num, dt);
			if (Mathf.Abs(reservoirSize - num) < 0.01f)
			{
				reservoirSize = num;
			}
			SetReservoirProgress(reservoirSize);
		}
		if (CanComplete() && reservoir.sharedMaterial != matReservoirCompleted)
		{
			reservoir.sharedMaterial = matReservoirCompleted;
		}
		else if (!CanComplete() && reservoir.sharedMaterial != matReservoir)
		{
			reservoir.sharedMaterial = matReservoir;
		}
		float num2 = 3f;
		float num3 = 60f / num2;
		if (!autoMode)
		{
			return;
		}
		if (TechReady())
		{
			autoTimer += dt;
			if (autoTimer > num3)
			{
				GiveInventorPoint(1);
				autoTimer = 0f;
			}
			autoTargetReservoirSize = 0.5f + 0.5f * (autoTimer / num3);
		}
		else
		{
			autoTargetReservoirSize = targetReservoirSize;
		}
	}

	public override float GetSpeed()
	{
		return base.GetSpeed() * 0.2f + base.GetSpeed() * 0.8f * (1f - reservoirSize);
	}

	public void SetAutoMode(bool target)
	{
		autoMode = target;
	}

	public override bool OpenUiOnClick()
	{
		if (!autoMode && TechReady())
		{
			return false;
		}
		return true;
	}

	public override void OnSelected(bool is_selected, bool was_selected)
	{
		base.OnSelected(is_selected, was_selected);
		if (!autoMode && is_selected && CanComplete())
		{
			Die(DeathCause.INVENTOR);
			Gameplay.instance.Select(null);
			AudioManager.PlayUI(base.transform.position, UISfx3D.AntSelect);
		}
	}

	public override bool ShouldPlayClickAudio()
	{
		return !CanComplete();
	}

	public override void OnPickupArrival(Pickup pickup)
	{
		Progress.AddSeenPickup(pickup.data.type);
		EatPickup(pickup);
	}

	public override bool CanEatPickup(PickupType _type)
	{
		if (currentRecipe == "")
		{
			foreach (ResearchRecipeData researchRecipe in TechTree.researchRecipes)
			{
				if (researchRecipe.caste != data.caste)
				{
					continue;
				}
				if (researchRecipe.energy > 0f && PickupData.Get(_type).energyAmount > 0f)
				{
					return true;
				}
				foreach (PickupCost cost in researchRecipe.costs)
				{
					if (cost.type == _type)
					{
						return true;
					}
				}
			}
		}
		else
		{
			ResearchRecipeData researchRecipeData = ResearchRecipeData.Get(currentRecipe);
			if (researchRecipeData.energy > 0f && PickupData.Get(_type).energyAmount > 0f && GetEatenEnergy() < researchRecipeData.energy)
			{
				return true;
			}
			foreach (PickupCost cost2 in researchRecipeData.costs)
			{
				if (cost2.type == _type)
				{
					if (!eatenPickups.ContainsKey(_type))
					{
						return true;
					}
					if (eatenPickups[_type] + 1 <= cost2.intValue)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public override void EatPickup(Pickup p)
	{
		if (currentRecipe == "")
		{
			foreach (ResearchRecipeData researchRecipe in TechTree.researchRecipes)
			{
				if (researchRecipe.caste != data.caste)
				{
					continue;
				}
				foreach (PickupCost cost in researchRecipe.costs)
				{
					if (cost.type == p.type)
					{
						SetRecipe(researchRecipe.code, during_load: false);
						break;
					}
				}
			}
			if (currentRecipe == "")
			{
				foreach (ResearchRecipeData researchRecipe2 in TechTree.researchRecipes)
				{
					if (researchRecipe2.caste == data.caste && researchRecipe2.energy > 0f && p.data.energyAmount > 0f)
					{
						SetRecipe(researchRecipe2.code, during_load: false);
						break;
					}
				}
			}
		}
		if (carryingPickups.Contains(p))
		{
			carryingPickups.Remove(p);
		}
		Progress.AddPickupFedToInventor(p.type);
		if (!eatenPickups.ContainsKey(p.type))
		{
			eatenPickups.Add(p.type, 0);
		}
		eatenPickups[p.type]++;
		p.Delete();
		UpdateReservior();
		if (anim != null)
		{
			anim.SetTrigger("Swallow");
		}
		PlayAudioShort(WorldSfx.QueenEat);
	}

	protected override float MinExchangeTime()
	{
		return 0.6f;
	}

	private float GetEatenEnergy()
	{
		float num = 0f;
		foreach (KeyValuePair<PickupType, int> eatenPickup in eatenPickups)
		{
			num += PickupData.Get(eatenPickup.Key).energyAmount * (float)eatenPickup.Value;
		}
		return num;
	}

	public void SetRecipe(string _tech, bool during_load)
	{
		currentRecipe = _tech;
		UpdateReservior(during_load);
	}

	private void UpdateReservior(bool instant = false)
	{
		if (DebugSettings.standard.inventorsAlwaysFull)
		{
			targetReservoirSize = 1f;
		}
		else
		{
			if (currentRecipe == "")
			{
				targetReservoirSize = 0f;
				if (instant)
				{
					reservoirSize = targetReservoirSize;
				}
				return;
			}
			ResearchRecipeData researchRecipeData = ResearchRecipeData.Get(currentRecipe);
			float num = 0f;
			float num2 = 0f;
			num2 += researchRecipeData.energy;
			foreach (PickupCost cost in researchRecipeData.costs)
			{
				num2 += (float)cost.intValue;
				if (eatenPickups.ContainsKey(cost.type))
				{
					num += (float)Mathf.Clamp(eatenPickups[cost.type], 0, cost.intValue);
				}
			}
			num += GetEatenEnergy();
			targetReservoirSize = Mathf.Clamp01(num / num2);
		}
		if (instant)
		{
			reservoirSize = targetReservoirSize;
			SetReservoirProgress(reservoirSize);
		}
	}

	private void SetReservoirProgress(float f)
	{
		reservoir.SetBlendShapeWeight(0, curve_Key2.Evaluate(f) * 100f);
		reservoir.SetBlendShapeWeight(1, curve_Key1.Evaluate(f) * 100f);
		colReservoir.enabled = f > 0.8f && !autoMode;
	}

	public Dictionary<PickupType, string> GetResearchProgress()
	{
		Dictionary<PickupType, string> dictionary = new Dictionary<PickupType, string>();
		if (currentRecipe != "")
		{
			foreach (PickupCost cost in ResearchRecipeData.Get(currentRecipe).costs)
			{
				int num = 0;
				if (eatenPickups.ContainsKey(cost.type))
				{
					num += eatenPickups[cost.type];
				}
				dictionary.Add(cost.type, $"{num} / {cost.intValue}");
			}
		}
		return dictionary;
	}

	public bool TechReady()
	{
		if (!IsDead())
		{
			return targetReservoirSize == 1f;
		}
		return false;
	}

	private bool CanComplete()
	{
		if (!autoMode && TechReady())
		{
			return Progress.HasUnlocked(GeneralUnlocks.COMPLETABLE_INVENTOR);
		}
		return false;
	}

	public void Complete()
	{
		string text = (DebugSettings.standard.inventorsAlwaysFull ? debugRecipe : currentRecipe);
		if (text != "" && targetReservoirSize > 0f && Mathf.CeilToInt((float)ResearchRecipeData.Get(text).productQuantity * targetReservoirSize) > 0)
		{
			GiveInventorPoint(GetPointsQuantity());
			tDeath = 0f;
			AudioLink antInventorExplode = AudioLinks.standard.antInventorExplode;
			if (antInventorExplode.IsSet())
			{
				AudioChannel looseChannel = AudioManager.GetLooseChannel();
				looseChannel.SetPos(base.transform.position);
				looseChannel.Play(antInventorExplode);
			}
			reservoir.SetObActive(active: false);
			psExplosion.SetObActive(active: true);
			psExplosion.transform.parent = null;
			if (GameManager.instance != null)
			{
				GameManager.instance.AddPausableParticles(psExplosion);
			}
			Progress.inventorsCompleted++;
		}
	}

	private int GetPointsQuantity()
	{
		return Mathf.CeilToInt((float)ResearchRecipeData.Get(DebugSettings.standard.inventorsAlwaysFull ? debugRecipe : currentRecipe).productQuantity * targetReservoirSize);
	}

	public void GiveInventorPoint(int amount)
	{
		ResearchRecipeData researchRecipeData = ResearchRecipeData.Get(DebugSettings.standard.inventorsAlwaysFull ? debugRecipe : currentRecipe);
		Progress.AddInventorPoints(researchRecipeData.productCurrency, amount, preview: false);
		UIGame.instance.StartCurrencyAnimation(researchRecipeData.productCurrency, amount, psExplosion.transform);
	}

	public override bool IsDead()
	{
		if (base.IsDead())
		{
			return true;
		}
		if (tDeath < timeDeath)
		{
			return true;
		}
		return false;
	}

	public float GetDeathProgress()
	{
		if (!IsDead())
		{
			return 0f;
		}
		return tDeath / timeDeath;
	}

	public override void Die(DeathCause _cause)
	{
		if (!autoMode)
		{
			Complete();
		}
		base.transform.parent = null;
		SetColliders(target: true);
		base.Die(_cause);
	}

	protected override void DoDelete()
	{
		if (GameManager.instance != null)
		{
			GameManager.instance.RemovePausableParticles(psExplosion);
		}
		psExplosion.transform.parent = base.transform;
		base.DoDelete();
	}

	protected override float GetCorpseRotTime()
	{
		return 60f;
	}

	public override void SetHoverUI(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI(ui_hover);
		if (!IsDead())
		{
			if (!(currentTrail != null) || !currentTrail.IsInBuilding(out var owner) || !(owner is InventorPad))
			{
				ui_hover.SetInfo();
			}
			else
			{
				SetHoverUI_Inventor(ui_hover, include_health: false);
			}
		}
	}

	public override void UpdateHoverUI(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI(ui_hover);
		if (IsDead())
		{
			return;
		}
		if (!(currentTrail != null) || !currentTrail.IsInBuilding(out var owner) || !(owner is InventorPad))
		{
			if (currentRecipe != "")
			{
				ui_hover.UpdateInfo(Loc.GetUI("ANT_INVENTOR_MOVETOPAD_CONTINUE"));
			}
			else
			{
				ui_hover.UpdateInfo(Loc.GetUI("ANT_INVENTOR_MOVETOPAD"));
			}
		}
		else
		{
			UpdateHoverUI_Inventor(ui_hover, include_health: false);
		}
	}

	public void SetHoverUI_Inventor(UIHoverClickOb ui_hover, bool include_health)
	{
		if (currentRecipe == "")
		{
			ui_hover.SetInfo();
		}
		else
		{
			ResearchRecipeData researchRecipeData = ResearchRecipeData.Get(currentRecipe);
			if (researchRecipeData.costs.Count > 0)
			{
				ui_hover.SetInventory();
			}
			if (researchRecipeData.energy > 0f)
			{
				ui_hover.SetEnergy(Loc.GetUI("ANT_INVENTOR_COLLECTEDENERGY"));
			}
		}
		if (include_health)
		{
			ui_hover.SetHealth(Loc.GetUI("ANT_LIFESPAN"));
		}
	}

	public void UpdateHoverUI_Inventor(UIHoverClickOb ui_hover, bool include_health)
	{
		if (currentRecipe == "")
		{
			ui_hover.UpdateInfo(Loc.GetUI("ANT_INVENTOR_WAITINGFORFOOD"));
		}
		else
		{
			ResearchRecipeData researchRecipeData = ResearchRecipeData.Get(currentRecipe);
			if (researchRecipeData.costs.Count > 0)
			{
				ui_hover.inventoryGrid.Update(Loc.GetUI("ANT_INVENTOR_PROGRESS"), GetResearchProgress(), Loc.GetUI("GENERIC_EMPTY"));
			}
			if (researchRecipeData.energy > 0f)
			{
				ui_hover.UpdateEnergy(Mathf.Clamp(GetEatenEnergy(), 0f, researchRecipeData.energy) + " / " + researchRecipeData.energy, Mathf.Clamp01(GetEatenEnergy() / researchRecipeData.energy));
			}
		}
		if (include_health)
		{
			if (IsImmortal())
			{
				ui_hover.UpdateHealth("?", 1f);
			}
			else
			{
				float num = energy;
				float num2 = energyTotal;
				ui_hover.UpdateHealth("", num / num2);
			}
			if (radiationDeathTimer > 0f)
			{
				ui_hover.UpdateRadDeath(Mathf.Clamp01(radiationDeathTimer / GlobalValues.standard.radDeathTime));
			}
		}
	}

	protected override bool GetAntInfo(out string s)
	{
		base.GetAntInfo(out s);
		if (!IsDead())
		{
			if (currentRecipe != "")
			{
				s = s + "\n\n" + Loc.GetUI("ANT_INVENTOR_MOVETOPAD_CONTINUE");
			}
			else
			{
				s = s + "\n\n" + Loc.GetUI("ANT_INVENTOR_MOVETOPAD");
			}
		}
		return s != "";
	}

	public void SetClickUI_Inventor(UIClickLayout_InventorPad ui_invent)
	{
		ui_invent.SetEnergy(Loc.GetUI("ANT_INVENTOR_COLLECTEDENERGY"));
		ui_invent.SetHealth(Loc.GetUI("ANT_LIFESPAN"));
	}

	public void UpdateClickUI_Inventor(UIClickLayout_InventorPad ui_invent)
	{
		if (currentRecipe == "")
		{
			ui_invent.SetInfo(Loc.GetUI("ANT_INVENTOR_WAITINGFORFOOD"));
			ui_invent.UpdateEnergy("", 0f);
		}
		else
		{
			ResearchRecipeData researchRecipeData = ResearchRecipeData.Get(currentRecipe);
			if (researchRecipeData.energy > 0f)
			{
				ui_invent.UpdateEnergy(Mathf.Clamp(GetEatenEnergy(), 0f, researchRecipeData.energy) + " / " + researchRecipeData.energy, Mathf.Clamp01(GetEatenEnergy() / researchRecipeData.energy));
			}
		}
		string amount = (IsImmortal() ? "?" : (energy / statusEffects.lifeDrainFactor).Unit(PhysUnit.TIME_MINUTES));
		ui_invent.UpdateHealth(amount, GetRemainingLife());
		if (radiationDeathTimer > 0f)
		{
			ui_invent.UpdateRadDeath(Mathf.Clamp01(radiationDeathTimer / GlobalValues.standard.radDeathTime));
		}
	}
}
