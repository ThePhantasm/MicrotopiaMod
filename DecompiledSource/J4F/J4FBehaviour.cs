using UnityEngine;

namespace J4F;

public class J4FBehaviour : MonoBehaviour
{
	private void Awake()
	{
		OnAwake();
	}

	protected virtual void OnAwake()
	{
	}

	private void Start()
	{
		OnStart();
	}

	protected virtual void OnStart()
	{
	}

	private void Update()
	{
		OnUpdate();
	}

	protected virtual void OnUpdate()
	{
	}
}
