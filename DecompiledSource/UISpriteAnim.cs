using UnityEngine;
using UnityEngine.UI;

public class UISpriteAnim : MonoBehaviour
{
	[SerializeField]
	private Sprite[] sprites;

	[SerializeField]
	private float spriteFrameDur = 0.1f;

	[SerializeField]
	private Image image;

	private float curTime;

	private int frame;

	private void Awake()
	{
		image.sprite = sprites[0];
	}

	private void Update()
	{
		curTime += Time.deltaTime;
		if (curTime > spriteFrameDur)
		{
			curTime -= spriteFrameDur;
			frame++;
			if (frame >= sprites.Length)
			{
				frame = 0;
			}
			image.sprite = sprites[frame];
		}
	}
}
