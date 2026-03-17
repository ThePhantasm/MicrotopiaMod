using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace HighlightPlus;

public class HighlightPlusRenderPassFeature : ScriptableRendererFeature
{
	private class HighlightPass : ScriptableRenderPass
	{
		private class DistanceComparer : IComparer<HighlightEffect>
		{
			public Vector3 camPos;

			public int Compare(HighlightEffect e1, HighlightEffect e2)
			{
				Vector3 obj = ((e1 == null) ? Vector3.zero : e1.transform.position);
				float num = obj.x - camPos.x;
				float num2 = obj.y - camPos.y;
				float num3 = obj.z - camPos.z;
				float num4 = num * num + num2 * num2 + num3 * num3;
				Vector3 obj2 = ((e2 == null) ? Vector3.zero : e2.transform.position);
				float num5 = obj2.x - camPos.x;
				float num6 = obj2.y - camPos.y;
				float num7 = obj2.z - camPos.z;
				float num8 = num5 * num5 + num6 * num6 + num7 * num7;
				if (num4 > num8)
				{
					return -1;
				}
				if (num4 < num8)
				{
					return 1;
				}
				return 0;
			}
		}

		public bool usesCameraOverlay;

		private ScriptableRenderer renderer;

		private RenderTextureDescriptor cameraTextureDescriptor;

		private DistanceComparer effectDistanceComparer;

		private static bool isVREnabled;

		private bool clearStencil;

		private FullScreenBlitMethod fullScreenBlitMethod = FullScreenBlit;

		private static Matrix4x4 matrix4x4identity = Matrix4x4.identity;

		private static Mesh _fullScreenMesh;

		private static Mesh fullscreenMesh
		{
			get
			{
				if (_fullScreenMesh != null)
				{
					return _fullScreenMesh;
				}
				float y = 1f;
				float y2 = 0f;
				_fullScreenMesh = new Mesh();
				_fullScreenMesh.SetVertices(new List<Vector3>
				{
					new Vector3(-1f, -1f, 0f),
					new Vector3(-1f, 1f, 0f),
					new Vector3(1f, -1f, 0f),
					new Vector3(1f, 1f, 0f)
				});
				_fullScreenMesh.SetUVs(0, new List<Vector2>
				{
					new Vector2(0f, y2),
					new Vector2(0f, y),
					new Vector2(1f, y2),
					new Vector2(1f, y)
				});
				_fullScreenMesh.SetIndices(new int[6] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, calculateBounds: false);
				_fullScreenMesh.UploadMeshData(markNoLongerReadable: true);
				return _fullScreenMesh;
			}
		}

		public void Setup(HighlightPlusRenderPassFeature passFeature, ScriptableRenderer renderer)
		{
			base.renderPassEvent = passFeature.renderPassEvent;
			clearStencil = passFeature.clearStencil;
			this.renderer = renderer;
			if (effectDistanceComparer == null)
			{
				effectDistanceComparer = new DistanceComparer();
			}
			isVREnabled = XRSettings.enabled && Application.isPlaying;
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			this.cameraTextureDescriptor = cameraTextureDescriptor;
			ConfigureInput(ScriptableRenderPassInput.Depth);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			int count = HighlightEffect.effects.Count;
			if (count == 0)
			{
				return;
			}
			Camera camera = renderingData.cameraData.camera;
			int num = 1 << camera.gameObject.layer;
			RenderTargetIdentifier cameraColorTarget = renderer.cameraColorTarget;
			RenderTargetIdentifier cameraDepthTarget = renderer.cameraDepthTarget;
			if (!HighlightEffect.customSorting && (Time.frameCount % 10 == 0 || !Application.isPlaying))
			{
				effectDistanceComparer.camPos = camera.transform.position;
				HighlightEffect.effects.Sort(effectDistanceComparer);
			}
			bool flag = clearStencil;
			for (int i = 0; i < count; i++)
			{
				HighlightEffect highlightEffect = HighlightEffect.effects[i];
				if (highlightEffect != null && highlightEffect.isActiveAndEnabled && ((int)highlightEffect.camerasLayerMask & num) != 0)
				{
					CommandBuffer commandBuffer = highlightEffect.GetCommandBuffer(camera, cameraColorTarget, cameraDepthTarget, fullScreenBlitMethod, flag);
					if (commandBuffer != null)
					{
						context.ExecuteCommandBuffer(commandBuffer);
						flag = false;
					}
				}
			}
		}

		private static void FullScreenBlit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int passIndex)
		{
			destination = new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, -1);
			cmd.SetRenderTarget(destination);
			cmd.SetGlobalTexture(ShaderParams.MainTex, source);
			cmd.SetGlobalFloat(ShaderParams.AspectRatio, isVREnabled ? 0.5f : 1f);
			cmd.DrawMesh(fullscreenMesh, matrix4x4identity, material, 0, passIndex);
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
		}
	}

	private HighlightPass renderPass;

	public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

	[Tooltip("Clears stencil buffer before rendering highlight effects. This option can solve compatibility issues with shaders that also use stencil buffers.")]
	public bool clearStencil;

	public static bool installed;

	private void OnDisable()
	{
		installed = false;
	}

	public override void Create()
	{
		renderPass = new HighlightPass();
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (renderingData.cameraData.renderType == CameraRenderType.Base)
		{
			Camera camera = renderingData.cameraData.camera;
			renderPass.usesCameraOverlay = camera.GetUniversalAdditionalCameraData().cameraStack.Count > 0;
		}
		renderPass.Setup(this, renderer);
		renderer.EnqueuePass(renderPass);
		installed = true;
	}
}
