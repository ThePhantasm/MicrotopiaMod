using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TrailGate : ClickableObject
{
	public enum GateCopyMode
	{
		Default,
		Settings,
		Clipboard
	}

	private static TrailGate clipboard;

	public Animator anim;

	public Renderer[] rends;

	[SerializeField]
	private GameObject obArrow;

	[SerializeField]
	private GameObject obTrafficGreen;

	[SerializeField]
	private GameObject obTrafficRed;

	protected Trail ownerTrail;

	protected bool externalControl;

	private float delayClose;

	private float delayDisableAnim;

	private bool isOpen;

	private Coroutine cTraffic;

	public static void ClearClipboard()
	{
		clipboard = null;
	}

	public override void Init(bool during_load = false)
	{
		base.Init(during_load);
		SetOb(obArrow, target: false);
		SetOb(obTrafficGreen, target: false);
		SetOb(obTrafficRed, target: false);
		if (anim != null)
		{
			anim.enabled = false;
		}
	}

	public override void Write(Save save)
	{
		WriteConfig(save);
	}

	public override void Read(Save save)
	{
		ReadConfig(save);
	}

	public virtual void LoadLinks()
	{
	}

	public void SetOwnerTrail(Trail _trail)
	{
		ownerTrail = _trail;
	}

	public Trail GetOwnerTrail()
	{
		return ownerTrail;
	}

	public new void Delete()
	{
		Object.Destroy(base.gameObject);
	}

	private void Update()
	{
		GateUpdate();
	}

	protected virtual void GateUpdate()
	{
		UpdateVisual(Time.deltaTime);
	}

	public virtual void UpdateVisual(float dt)
	{
		if (externalControl)
		{
			bool flag = CheckIfSatisfied(null, final: false, chain_satisfied: false) && CheckIfChainSatisfied(null, final: false);
			if (flag != isOpen)
			{
				SetOb(obArrow, flag);
				if (flag)
				{
					SetAnimOpen(open: true);
					delayClose = 0f;
				}
				else
				{
					delayClose = 0.4f;
				}
				isOpen = flag;
			}
		}
		if (delayClose > 0f)
		{
			delayClose -= dt;
			if (delayClose <= 0f)
			{
				SetAnimOpen(open: false);
				if (!externalControl)
				{
					SetOb(obArrow, target: false);
				}
			}
		}
		if (delayDisableAnim > 0f)
		{
			delayDisableAnim -= dt;
			if (delayDisableAnim <= 0f)
			{
				anim.enabled = false;
			}
		}
	}

	public void ShowAllowAnt(bool satisfied, bool entering, bool chain_satisfied)
	{
		if (satisfied && entering && !externalControl)
		{
			SetAnimOpen(open: true);
			SetOb(obArrow, target: true);
			delayClose = 0.6f;
		}
		if (cTraffic != null)
		{
			StopCoroutine(cTraffic);
			SetOb(obTrafficGreen, target: false);
			SetOb(obTrafficRed, target: false);
		}
		if (satisfied)
		{
			cTraffic = StartCoroutine(CShowTrafficGreen(chain_satisfied ? 0.6f : 0.3f));
		}
		else
		{
			cTraffic = StartCoroutine(CShowTrafficRed());
		}
	}

	private void SetAnimOpen(bool open)
	{
		anim.enabled = true;
		anim.SetBool(ClickableObject.paramOpen, open);
		delayDisableAnim = 0.5f;
	}

	private IEnumerator CShowTrafficGreen(float duration)
	{
		if (!(obTrafficGreen == null))
		{
			SetOb(obTrafficGreen, target: true);
			yield return new WaitForSeconds(duration);
			SetOb(obTrafficGreen, target: false);
		}
	}

	private IEnumerator CShowTrafficRed()
	{
		if (!(obTrafficRed == null))
		{
			SetOb(obTrafficRed, target: true);
			yield return new WaitForSeconds(0.1f);
			SetOb(obTrafficRed, target: false);
			yield return new WaitForSeconds(0.1f);
			SetOb(obTrafficRed, target: true);
			yield return new WaitForSeconds(0.1f);
			SetOb(obTrafficRed, target: false);
		}
	}

	public virtual bool CheckIfSatisfied(Ant _ant, bool final, bool chain_satisfied)
	{
		return false;
	}

	public virtual void EnterGate(Ant _ant)
	{
	}

	public bool CheckIfChainSatisfied(Ant _ant, bool final)
	{
		List<Trail> gateChain = ownerTrail.GetGateChain();
		bool result = true;
		foreach (Trail item in gateChain)
		{
			if (!(item == ownerTrail))
			{
				bool flag = item.trailGate.CheckIfSatisfied(_ant, final: false, chain_satisfied: false);
				if (final)
				{
					item.trailGate.ShowAllowAnt(flag, entering: false, chain_satisfied: false);
				}
				if (!flag)
				{
					result = false;
				}
			}
		}
		return result;
	}

	public void SetMaterial(Material mat)
	{
		Renderer[] array = rends;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].sharedMaterial = mat;
		}
	}

	public virtual void CleanObjectLinks()
	{
	}

	public override void SetClickUi(UIClickLayout ui_click)
	{
		ButtonWithHotkey button = ui_click.GetButton(UIClickButtonType.CopySettings, show_button_error: false);
		if (button != null)
		{
			button.SetButton(delegate
			{
				Gameplay.CopySettings(this);
			}, InputAction.CopySettings);
			button.SetHover("CLICKBOTBUT_HOVER_COPY_GATE");
		}
		button = ui_click.GetButton(UIClickButtonType.PasteSettings, show_button_error: false);
		if (button != null)
		{
			button.SetButton(delegate
			{
				Gameplay.PasteSettings(this);
			}, InputAction.PasteSettings);
			button.SetInteractable(CanPasteClipboard(this));
			button.SetHover("CLICKBOTBUT_HOVER_PASTE_GATE");
		}
	}

	public static bool CanPasteClipboard(TrailGate gate)
	{
		if (clipboard != null)
		{
			return clipboard.GetTrailType() == gate.GetTrailType();
		}
		return false;
	}

	public static bool CopyToClipboard(TrailGate gate)
	{
		clipboard = gate;
		return true;
	}

	public static bool PasteClipboard(TrailGate gate)
	{
		if (!CanPasteClipboard(gate))
		{
			return false;
		}
		gate.CopyFrom(clipboard, GateCopyMode.Clipboard);
		return true;
	}

	public abstract TrailType GetTrailType();

	public abstract void CopyFrom(TrailGate other, GateCopyMode copy_mode = GateCopyMode.Default);

	public abstract void WriteConfig(ISaveContainer save);

	public abstract void ReadConfig(ISaveContainer save);

	public override float HologramSize()
	{
		return 0.75f;
	}

	private void SetOb(GameObject ob, bool target)
	{
		if (ob != null)
		{
			ob.SetObActive(target);
		}
	}
}
