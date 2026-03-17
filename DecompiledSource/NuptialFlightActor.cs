using System.Collections.Generic;
using UnityEngine;

public class NuptialFlightActor : MonoBehaviour
{
	[SerializeField]
	private Renderer rend;

	public Animator anim;

	[SerializeField]
	private Vector2 speedRange = new Vector2(15f, 30f);

	[SerializeField]
	private float timeTakeOff;

	public AntCaste caste;

	private NuptialFlightMode mode;

	private float currentSpeed;

	private float targetSpeed;

	private Vector3 startPos;

	private Vector3 endPos;

	private float progress;

	[SerializeField]
	private TriggerArea gyneTrigger;

	private float tStart;

	private float tTurn;

	private Vector3 targetPos;

	private Vector2 tTurnRange_freeform = new Vector2(5f, 30f);

	private List<NuptialFlightActor> followers = new List<NuptialFlightActor>();

	private List<GameObject> foundObs = new List<GameObject>();

	private NuptialFlightActor followTarget;

	private Vector3 followOffset;

	private float turnSpeed;

	private Vector2 tTurnRange_following = new Vector2(0f, 5f);

	private void Awake()
	{
		if (anim != null)
		{
			anim.cullingMode = AnimatorCullingMode.CullCompletely;
		}
	}

	public void Write(Save save)
	{
		save.Write(base.transform.position);
		save.Write(base.transform.rotation.eulerAngles);
		save.Write((int)mode);
		switch (mode)
		{
		case NuptialFlightMode.STRAIGHT_LINE:
			save.Write(startPos);
			save.Write(endPos);
			save.Write(progress);
			break;
		case NuptialFlightMode.FREEFORM:
			save.Write(tStart);
			save.Write(targetSpeed);
			break;
		case NuptialFlightMode.FOLLOWING:
			break;
		}
	}

	public void Read(Save save)
	{
		if (save.version < 69)
		{
			startPos = save.ReadVector3();
			endPos = save.ReadVector3();
			progress = save.ReadFloat();
			if (save.version < 45)
			{
				save.ReadFloat();
			}
			return;
		}
		base.transform.position = save.ReadVector3();
		base.transform.rotation = Quaternion.Euler(save.ReadVector3());
		mode = (NuptialFlightMode)save.ReadInt();
		switch (mode)
		{
		case NuptialFlightMode.STRAIGHT_LINE:
			startPos = save.ReadVector3();
			endPos = save.ReadVector3();
			progress = save.ReadFloat();
			break;
		case NuptialFlightMode.FREEFORM:
			tStart = save.ReadFloat();
			targetSpeed = save.ReadFloat();
			break;
		case NuptialFlightMode.FOLLOWING:
			break;
		}
	}

	public void InitFreeForm()
	{
		mode = NuptialFlightMode.FREEFORM;
		tStart = timeTakeOff;
		targetSpeed = Random.Range(speedRange.x, speedRange.y);
		SetTargetPos();
		Init();
	}

	public void InitStraightLine(Vector3 start, Vector3 end)
	{
		mode = NuptialFlightMode.STRAIGHT_LINE;
		startPos = start;
		endPos = end;
		progress = 0f;
		Init();
	}

	public void InitFollowing(NuptialFlightActor target)
	{
		mode = NuptialFlightMode.FOLLOWING;
		followTarget = target;
		turnSpeed = Random.Range(100f, 250f);
		SetTargetPos();
	}

	public void Init()
	{
		anim.SetBool("Fly", value: true);
		if (mode == NuptialFlightMode.STRAIGHT_LINE)
		{
			base.transform.rotation = Quaternion.LookRotation(Toolkit.LookVector(startPos, endPos), Vector3.up);
			currentSpeed = Random.Range(speedRange.x, speedRange.y);
		}
		if (mode == NuptialFlightMode.FOLLOWING)
		{
			currentSpeed = Random.Range(speedRange.x, speedRange.y);
		}
		DoUpdate(0f);
	}

	public void DoUpdate(float dt)
	{
		switch (mode)
		{
		case NuptialFlightMode.STRAIGHT_LINE:
			progress += dt * currentSpeed / Vector3.Distance(startPos, endPos);
			base.transform.position = startPos + (endPos - startPos) * progress;
			break;
		case NuptialFlightMode.FREEFORM:
			if (tStart > 0f)
			{
				tStart -= dt;
				break;
			}
			if (currentSpeed != targetSpeed)
			{
				currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, dt);
			}
			if (NuptialFlight.IsNuptialFlightActive())
			{
				tTurn -= dt;
				if (tTurn < 0f)
				{
					SetTargetPos();
				}
				Quaternion to = Quaternion.LookRotation(Toolkit.LookVector(base.transform.position, targetPos), Vector3.up);
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, dt * 30f);
			}
			base.transform.position += base.transform.forward * currentSpeed * dt;
			break;
		case NuptialFlightMode.FOLLOWING:
			if (!(followTarget == null))
			{
				tTurn -= dt;
				if (tTurn < 0f)
				{
					SetTargetPos();
				}
				Quaternion to = Quaternion.LookRotation(Toolkit.LookVector(base.transform.position, followTarget.transform.position + followOffset), Vector3.up);
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, dt * turnSpeed);
				base.transform.position += base.transform.forward * currentSpeed * dt;
			}
			break;
		}
	}

	public void DoFixedUpdate(float xdt)
	{
		if (mode != NuptialFlightMode.FREEFORM)
		{
			return;
		}
		if (gyneTrigger == null)
		{
			Debug.LogError(base.name + " missing gyne trigger!");
			return;
		}
		foreach (Collider item in gyneTrigger.EOverlapping())
		{
			if (!foundObs.Contains(item.gameObject))
			{
				foundObs.Add(item.gameObject);
				NuptialFlightActor componentInParent = item.GetComponentInParent<NuptialFlightActor>();
				if (!(componentInParent == null) && !(componentInParent == this) && !followers.Contains(componentInParent) && componentInParent.GetMode() == NuptialFlightMode.STRAIGHT_LINE)
				{
					followers.Add(componentInParent);
					componentInParent.InitFollowing(this);
					NuptialFlight.AddDroneAttracted();
				}
			}
		}
		gyneTrigger.ResetOverlap();
	}

	public bool FlyingDone()
	{
		return progress >= 1f;
	}

	public bool IsVisible()
	{
		return rend.isVisible;
	}

	private void SetTargetPos()
	{
		switch (mode)
		{
		case NuptialFlightMode.FREEFORM:
		{
			tTurn = Random.Range(tTurnRange_freeform.x, tTurnRange_freeform.y);
			Vector3 position = base.transform.position;
			Vector2 vector = Random.insideUnitCircle * Random.Range(50f, 200f);
			position.x += vector.x;
			position.y = Random.Range(70f, 120f);
			position.z += vector.y;
			targetPos = position;
			break;
		}
		case NuptialFlightMode.FOLLOWING:
			tTurn = Random.Range(tTurnRange_following.x, tTurnRange_following.y);
			followOffset = Random.insideUnitSphere * 30f;
			break;
		}
	}

	public NuptialFlightMode GetMode()
	{
		return mode;
	}

	public void Delete()
	{
		GameManager.instance.DeleteNuptialFlightActor(this);
	}
}
