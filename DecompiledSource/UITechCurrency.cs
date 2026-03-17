using UnityEngine;

public class UITechCurrency : UITextImageButton
{
	[SerializeField]
	private InventorPointsIcon[] icons;

	private InventorPoints pointsType;

	private AntInventor inventor;

	private Transform startPoint;

	private float counter;

	private float waitTime;

	private float explodeTime;

	private float moveTime;

	private Vector3 circle;

	private Vector3 lastStartPoint;

	public void Init(InventorPoints _type, string s = "")
	{
		InventorPointsIcon[] array = icons;
		foreach (InventorPointsIcon inventorPointsIcon in array)
		{
			inventorPointsIcon.icon.SetObActive(_type == inventorPointsIcon.points);
		}
		SetText(s);
	}

	public void StartAnimation(InventorPoints _type, Transform start_point, float wait_time, float explode_time, float move_time)
	{
		pointsType = _type;
		startPoint = start_point;
		counter = 0f;
		waitTime = wait_time;
		explodeTime = explode_time;
		moveTime = move_time;
		float maxInclusive = 200f;
		circle = Random.insideUnitCircle * Random.Range(0f, maxInclusive);
		SetPosition(GetStartPos());
		rtBase.localScale = Vector3.zero;
		SetText("");
		ResetOverlays();
		Show(target: true);
	}

	public void CurrencyUpdate()
	{
		counter += Time.deltaTime;
		if (counter < waitTime)
		{
			SetPosition(GetStartPos());
			rtBase.localScale = Vector3.zero;
		}
		else if (counter < waitTime + explodeTime)
		{
			float time = (counter - waitTime) / explodeTime;
			Vector3 position = Vector2.Lerp(GetStartPos(), GetStartPos() + circle, GlobalValues.standard.curveEaseIn.Evaluate(time));
			SetPosition(position);
			time = Mathf.Clamp01((counter - waitTime) / (explodeTime / 2f));
			rtBase.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time);
		}
		else if (counter < waitTime + explodeTime + moveTime)
		{
			Vector3 position2 = Vector2.Lerp(GetStartPos() + circle, UIGame.instance.GetTechTreeButtonPos(), GlobalValues.standard.curveEaseOutHeavy.Evaluate((counter - waitTime - explodeTime) / moveTime));
			SetPosition(position2);
		}
		else
		{
			UIGame.instance.EndCurrency(pointsType, this);
		}
	}

	private Vector3 GetStartPos()
	{
		if (startPoint != null)
		{
			lastStartPoint = GetPositionFromWorld(startPoint.position);
		}
		return lastStartPoint;
	}
}
