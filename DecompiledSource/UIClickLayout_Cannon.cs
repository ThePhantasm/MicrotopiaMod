using UnityEngine;
using UnityEngine.UI;

public class UIClickLayout_Cannon : UIClickLayout_Building
{
	[Header("Cannon")]
	[SerializeField]
	private Slider slCannonRot;

	[SerializeField]
	private Slider slCannonAngle;

	[SerializeField]
	private Slider slCannonPower;

	public void SetCannon(AntLauncher _cannon)
	{
		slCannonRot.onValueChanged.RemoveAllListeners();
		slCannonAngle.onValueChanged.RemoveAllListeners();
		slCannonPower.onValueChanged.RemoveAllListeners();
		slCannonRot.value = _cannon.rotation;
		slCannonAngle.value = _cannon.angle;
		slCannonPower.value = 1f - _cannon.power;
		slCannonRot.onValueChanged.AddListener(delegate(float v)
		{
			_cannon.rotation = v;
			_cannon.UpdateTrajectory();
		});
		slCannonAngle.onValueChanged.AddListener(delegate(float v)
		{
			_cannon.angle = v;
			_cannon.UpdateTrajectory();
		});
		slCannonPower.onValueChanged.AddListener(delegate(float v)
		{
			_cannon.power = 1f - v;
		});
	}
}
