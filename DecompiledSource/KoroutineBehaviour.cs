using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KoroutineBehaviour : MonoBehaviour
{
	public delegate void FinalizeAction();

	private struct KoroutineInfo
	{
		public Coroutine coroutine;

		public FinalizeAction finalizer;

		public List<KoroutineId> children;
	}

	public struct KoroutineId
	{
		private ulong id;

		private KoroutineBehaviour caller;

		public static KoroutineId empty = new KoroutineId(null, 0uL);

		public KoroutineId(KoroutineBehaviour _caller, ulong _id)
		{
			id = _id;
			caller = _caller;
		}

		public override string ToString()
		{
			return caller.GetHashCode() + "/" + id;
		}

		public bool IsEmpty()
		{
			return caller == null;
		}

		public void SetEmpty()
		{
			id = 0uL;
			caller = null;
		}

		public KoroutineBehaviour GetCaller()
		{
			return caller;
		}

		public override bool Equals(object obj)
		{
			return Equals((KoroutineId)obj);
		}

		public bool Equals(KoroutineId other)
		{
			if (caller == other.caller)
			{
				if (!(caller == null))
				{
					return id == other.id;
				}
				return true;
			}
			return false;
		}

		public static bool operator ==(KoroutineId a, KoroutineId b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(KoroutineId a, KoroutineId b)
		{
			return !a.Equals(b);
		}

		public override int GetHashCode()
		{
			return ((!(caller == null)) ? caller.GetHashCode() : 0) ^ (int)id;
		}

		public bool IsRunning()
		{
			if (IsEmpty())
			{
				return false;
			}
			return caller.IsKoroutineRunning(this);
		}
	}

	private static Dictionary<KoroutineId, KoroutineInfo> koroutines_ = new Dictionary<KoroutineId, KoroutineInfo>();

	private static ulong kId_ = 0uL;

	private static KoroutineId lastId_;

	private static bool SHOW_CALLS = false;

	protected virtual void OnDestroy()
	{
		KoroutineId[] array = new KoroutineId[koroutines_.Count];
		koroutines_.Keys.CopyTo(array, 0);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].GetCaller() == this)
			{
				StopKoroutine(array[i]);
			}
		}
	}

	public Coroutine StartKoroutine(IEnumerator e)
	{
		KoroutineId kid;
		return StartKoroutine(default(KoroutineId), e, out kid);
	}

	public Coroutine StartKoroutine(IEnumerator e, out KoroutineId kid)
	{
		return StartKoroutine(default(KoroutineId), e, out kid);
	}

	public Coroutine StartKoroutine(KoroutineId parentId, IEnumerator e)
	{
		KoroutineId kid;
		return StartKoroutine(parentId, e, out kid);
	}

	public Coroutine StartKoroutine(KoroutineId parent_id, IEnumerator e, out KoroutineId kid)
	{
		kid = new KoroutineId(this, ++kId_);
		lastId_ = kid;
		if (SHOW_CALLS)
		{
			KoroutineId koroutineId = kid;
			Debug.Log("StartKoroutine: " + koroutineId.ToString());
		}
		koroutines_.Add(kid, default(KoroutineInfo));
		Coroutine coroutine = StartCoroutine(e);
		if (!koroutines_.TryGetValue(kid, out var value))
		{
			if (SHOW_CALLS)
			{
				KoroutineId koroutineId = kid;
				Debug.Log("StartKoroutine " + koroutineId.ToString() + ": already stopped in first frame");
			}
			return coroutine;
		}
		value.coroutine = coroutine;
		koroutines_[kid] = value;
		if (!parent_id.IsEmpty())
		{
			if (!koroutines_.TryGetValue(parent_id, out var value2))
			{
				KoroutineId koroutineId = parent_id;
				Debug.LogWarning("StartKoroutine: parent id " + koroutineId.ToString() + " not found");
			}
			else
			{
				if (value2.children == null)
				{
					value2.children = new List<KoroutineId>(2);
				}
				value2.children.Add(kid);
				koroutines_[parent_id] = value2;
			}
		}
		return coroutine;
	}

	public bool AreKoroutineChildrenRunning(KoroutineId parent_id)
	{
		if (parent_id.IsEmpty())
		{
			return false;
		}
		if (!koroutines_.TryGetValue(parent_id, out var value))
		{
			KoroutineId koroutineId = parent_id;
			Debug.LogWarning("AreKoroutineChildrenRunning: parent id " + koroutineId.ToString() + " not found");
			return false;
		}
		if (value.children == null)
		{
			return false;
		}
		foreach (KoroutineId child in value.children)
		{
			if (IsKoroutineRunning(child))
			{
				return true;
			}
		}
		return false;
	}

	public KoroutineId SetFinalizer(FinalizeAction finalizer = null)
	{
		if (!SetFinalizer(lastId_, finalizer))
		{
			KoroutineId koroutineId = lastId_;
			Debug.LogWarning("SetFinalizer: id " + koroutineId.ToString() + " not found");
		}
		return lastId_;
	}

	private bool SetFinalizer(KoroutineId kid, FinalizeAction finalizer)
	{
		if (kid.IsEmpty())
		{
			return false;
		}
		if (kid.GetCaller() != this)
		{
			return kid.GetCaller().SetFinalizer(kid, finalizer);
		}
		if (SHOW_CALLS)
		{
			KoroutineId koroutineId = kid;
			Debug.Log("SetFinalizer " + koroutineId.ToString() + ((finalizer == null) ? " (clear)" : ""));
		}
		if (!koroutines_.TryGetValue(kid, out var value))
		{
			return false;
		}
		value = koroutines_[kid];
		value.finalizer = finalizer;
		koroutines_[kid] = value;
		return true;
	}

	public bool StopKoroutine(KoroutineId kid, bool do_finalizer = true)
	{
		if (kid.IsEmpty())
		{
			return false;
		}
		if (kid.GetCaller() != this)
		{
			return kid.GetCaller().StopKoroutine(kid, do_finalizer);
		}
		if (!koroutines_.TryGetValue(kid, out var value))
		{
			return false;
		}
		if (SHOW_CALLS)
		{
			KoroutineId koroutineId = kid;
			Debug.Log("StopKoroutine " + koroutineId.ToString());
		}
		if (value.coroutine != null)
		{
			StopCoroutine(value.coroutine);
		}
		if (value.children != null)
		{
			for (int i = 0; i < value.children.Count; i++)
			{
				StopKoroutine(value.children[i], do_finalizer);
			}
		}
		if (do_finalizer && value.finalizer != null && !GameManager.isQuitting)
		{
			value.finalizer();
		}
		koroutines_.Remove(kid);
		return true;
	}

	public bool ClearFinalizer(KoroutineId kid)
	{
		if (!SetFinalizer(kid, null))
		{
			KoroutineId koroutineId = kid;
			Debug.LogWarning("ClearFinalizer: id " + koroutineId.ToString() + " not found");
			return false;
		}
		return true;
	}

	private bool IsKoroutineRunning(KoroutineId kid)
	{
		if (kid.GetCaller() != this)
		{
			return kid.GetCaller().IsKoroutineRunning(kid);
		}
		return koroutines_.ContainsKey(kid);
	}
}
