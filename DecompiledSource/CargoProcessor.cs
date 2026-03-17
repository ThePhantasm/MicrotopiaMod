public abstract class CargoProcessor : Building
{
	private Trail cargoTrail;

	private CargoAnt nextSegment;

	private CargoAnt handledSegment;

	private float maxWaitLoad = 5f;

	private float maxWaitUnload = 5f;

	private float waiting;

	private bool loadDone;

	private bool unloadDone;

	protected CargoAnt curSegment;

	public override void Write(Save save)
	{
		base.Write(save);
		save.Write((!(cargoTrail == null)) ? cargoTrail.linkId : 0);
		save.Write((!(nextSegment == null)) ? nextSegment.linkId : 0);
		save.Write((!(curSegment == null)) ? curSegment.linkId : 0);
		WriteConfig(save);
		save.Write(waiting);
		save.Write(loadDone);
		save.Write(unloadDone);
	}

	public override void Read(Save save)
	{
		base.Read(save);
		cargoTrail = GameManager.instance.FindLink<Trail>(save.ReadInt());
		nextSegment = GameManager.instance.FindLink<CargoAnt>(save.ReadInt());
		curSegment = GameManager.instance.FindLink<CargoAnt>(save.ReadInt());
		ReadConfig(save);
		waiting = save.ReadFloat();
		loadDone = save.ReadBool();
		unloadDone = save.ReadBool();
	}

	public override void WriteConfig(ISaveContainer save)
	{
		base.WriteConfig(save);
		save.Write(maxWaitLoad);
		save.Write(maxWaitUnload);
	}

	public override void ReadConfig(ISaveContainer save)
	{
		base.ReadConfig(save);
		maxWaitLoad = save.ReadFloat();
		maxWaitUnload = save.ReadFloat();
	}

	protected override void PlaceBuildingTrails()
	{
		base.PlaceBuildingTrails();
		cargoTrail = listSpawnedTrails[0][0];
	}

	public override void BuildingUpdate(float dt, bool runWorld)
	{
		base.BuildingUpdate(dt, runWorld);
		if (!runWorld || currentStatus != BuildingStatus.COMPLETED)
		{
			return;
		}
		if (curSegment != null)
		{
			waiting += dt;
			if (!loadDone && waiting > maxWaitLoad)
			{
				loadDone = true;
				CheckContinue();
			}
			if (!unloadDone && waiting > maxWaitUnload)
			{
				unloadDone = true;
				CheckContinue();
			}
			return;
		}
		if (nextSegment != null && nextSegment.trailProgress > 0.5f)
		{
			nextSegment.trailProgress = 0.5f;
			nextSegment.transform.position = cargoTrail.GetPos(0.5f);
			SetReady(nextSegment);
			nextSegment = null;
			return;
		}
		float num = float.MaxValue;
		nextSegment = null;
		foreach (Ant currentAnt in cargoTrail.currentAnts)
		{
			AntCaste caste = currentAnt.data.caste;
			if ((uint)(caste - 6) > 1u)
			{
				continue;
			}
			CargoAnt cargoAnt = (CargoAnt)currentAnt;
			float num2 = 0.5f - cargoAnt.trailProgress;
			if (num2 < 0f)
			{
				continue;
			}
			if (cargoAnt == handledSegment)
			{
				handledSegment = null;
				if (num2 < 0.01f)
				{
					continue;
				}
			}
			if (num2 < num)
			{
				num = num2;
				nextSegment = cargoAnt;
			}
		}
	}

	protected virtual void SetReady(CargoAnt segment)
	{
		curSegment = segment;
		extractablePickupsChanged = true;
		foreach (CargoAnt item in curSegment.EAllSubAnts())
		{
			item.SetMoveState(MoveState.WaitForCargo);
		}
		waiting = 0f;
		loadDone = (unloadDone = false);
	}

	protected abstract bool IsLoaded(CargoAnt segment);

	private void CheckContinue()
	{
		if (loadDone && !unloadDone && !IsLoaded(curSegment))
		{
			unloadDone = true;
		}
		if (unloadDone && !loadDone && IsLoaded(curSegment))
		{
			loadDone = true;
		}
		if (!loadDone || !unloadDone)
		{
			return;
		}
		foreach (CargoAnt item in curSegment.EAllSubAnts())
		{
			item.SetMoveState(MoveState.Normal);
		}
		handledSegment = curSegment;
		curSegment = null;
		extractablePickupsChanged = true;
	}

	protected override void SetHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.SetHoverUI_Intake(ui_hover);
		ui_hover.SetCargoButton(unload: false, delegate
		{
			maxWaitLoad = NextWait(maxWaitLoad);
			ui_hover.UpdateCargoButton(unload: false, GetWaitTxt(maxWaitLoad));
		});
		ui_hover.SetCargoButton(unload: true, delegate
		{
			maxWaitUnload = NextWait(maxWaitUnload);
			ui_hover.UpdateCargoButton(unload: true, GetWaitTxt(maxWaitUnload));
		});
		ui_hover.UpdateCargoButton(unload: false, GetWaitTxt(maxWaitLoad));
		ui_hover.UpdateCargoButton(unload: true, GetWaitTxt(maxWaitUnload));
	}

	protected void SetLoadDone()
	{
		loadDone = (unloadDone = true);
		extractablePickupsChanged = true;
		CheckContinue();
	}

	protected void SetUnloadDone()
	{
		unloadDone = true;
		extractablePickupsChanged = true;
		CheckContinue();
	}

	private float NextWait(float wait)
	{
		if (wait <= 2f)
		{
			if (wait == 0f)
			{
				return 1f;
			}
			if (wait == 1f)
			{
				return 2f;
			}
			if (wait == 2f)
			{
				return 5f;
			}
		}
		else
		{
			if (wait == 5f)
			{
				return 10f;
			}
			if (wait == 10f)
			{
				return float.MaxValue;
			}
			if (wait == float.MaxValue)
			{
				return 0f;
			}
		}
		return 5f;
	}

	private string GetWaitTxt(float wait)
	{
		if (wait != float.MaxValue)
		{
			return $"{wait:0} seconds";
		}
		return "unlimited";
	}

	protected override void UpdateHoverUI_Intake(UIHoverClickOb ui_hover)
	{
		base.UpdateHoverUI_Intake(ui_hover);
	}
}
