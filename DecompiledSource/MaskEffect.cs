using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class MaskEffect : MonoBehaviour
{
	public Material mat;

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		Graphics.Blit(src, dest, mat);
	}
}
