using System;
using UnityEngine;

public class Plant : BiomeObject
{
	[NonSerialized]
	[Header("Plant")]
	public PlantState state;

	[NonSerialized]
	public PlantType type;

	[NonSerialized]
	public Ecology ecology;

	protected PlantData plantData;

	protected float remainingTime;

	[SerializeField]
	private Transform saplingVersion;

	[SerializeField]
	private Transform wiltedVersion;

	[Tooltip("Only set for large plants")]
	public float extraEdgeCheckRadius;

	public void Fill(PlantType _type)
	{
		type = _type;
		plantData = PlantData.Get(type);
		code = type.ToString();
	}

	public override void Write(Save save)
	{
		save.Write(base.transform.position);
		save.WriteYRot(base.transform.rotation);
		save.Write(spawnSize);
		base.Write(save);
		save.Write((byte)state);
		if (state != PlantState.Grown)
		{
			save.Write(remainingTime);
		}
	}

	public override void Read(Save save)
	{
		base.Read(save);
		PlantState plantState = (PlantState)save.ReadByte();
		state = plantState;
		remainingTime = ((state == PlantState.Grown) ? 0f : save.ReadFloat());
	}

	public virtual void LoadLinkPickups()
	{
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		SetState(state);
	}

	public virtual void SetState(PlantState new_state)
	{
		PlantState plantState = state;
		state = new_state;
		if (effectArea != null)
		{
			effectArea.SetActive(state == PlantState.Grown);
		}
		if (saplingVersion != null)
		{
			saplingVersion.SetObActive(state == PlantState.Growing);
		}
		currentMesh.SetObActive(state == PlantState.Grown);
		if (wiltedVersion != null)
		{
			wiltedVersion.SetObActive(state == PlantState.Dead);
		}
		switch (state)
		{
		case PlantState.Growing:
			remainingTime = plantData.growTime;
			break;
		case PlantState.Grown:
			if (plantState == PlantState.Growing)
			{
				ConnectToTrails();
			}
			break;
		case PlantState.Dead:
			if (plantState == PlantState.Grown)
			{
				DisconnectFromTrails();
			}
			remainingTime = plantData.wiltTime;
			break;
		}
	}

	public virtual void UpdateGrowWilt(float dt)
	{
		switch (state)
		{
		case PlantState.Growing:
			remainingTime -= dt;
			if (remainingTime < 0f)
			{
				SetState(PlantState.Grown);
			}
			break;
		case PlantState.Dead:
			remainingTime -= dt;
			if (remainingTime < 0f)
			{
				SetState(PlantState.Remove);
			}
			break;
		}
	}

	protected override void DoDelete()
	{
		if (ecology != null)
		{
			ecology.GotRemoved(this);
		}
		base.DoDelete();
	}
}
