using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace HighlightPlus;

[ExecuteInEditMode]
[HelpURL("https://kronnect.com/guides/highlight-plus-introduction/")]
public class HighlightEffect : MonoBehaviour
{
	private struct ModelMaterials
	{
		public bool render;

		public Transform transform;

		public bool renderWasVisibleDuringSetup;

		public Mesh mesh;

		public Mesh originalMesh;

		public Renderer renderer;

		public bool isSkinnedMesh;

		public Material[] fxMatMask;

		public Material[] fxMatOutline;

		public Material[] fxMatGlow;

		public Material[] fxMatSolidColor;

		public Material[] fxMatSeeThroughInner;

		public Material[] fxMatSeeThroughBorder;

		public Material[] fxMatOverlay;

		public Material[] fxMatInnerGlow;

		public NormalsOption normalsOption;

		public Matrix4x4 renderingMatrix;

		public bool isCombined;

		public bool preserveOriginalMesh
		{
			get
			{
				if (!isCombined)
				{
					return normalsOption == NormalsOption.PreserveOriginal;
				}
				return false;
			}
		}

		public void Init()
		{
			render = false;
			transform = null;
			mesh = (originalMesh = null);
			renderer = null;
			isSkinnedMesh = false;
			normalsOption = NormalsOption.Smooth;
			isCombined = false;
		}
	}

	private enum FadingState
	{
		FadingOut = -1,
		NoFading,
		FadingIn
	}

	[Tooltip("The current profile (optional). A profile let you store Highlight Plus settings and apply those settings easily to many objects. You can also load a profile and apply its settings at runtime, using the ProfileLoad() method of the Highlight Effect component.")]
	public HighlightProfile profile;

	[Tooltip("If enabled, settings from the profile will be applied to this component automatically when game starts or when any profile setting is updated.")]
	public bool profileSync;

	[Tooltip("If enabled, effects will be visible also when not in Play mode.")]
	public bool previewInEditor = true;

	[Tooltip("Which cameras can render the effect.")]
	public LayerMask camerasLayerMask = -1;

	[Tooltip("Different options to specify which objects are affected by this Highlight Effect component.")]
	public TargetOptions effectGroup;

	[Tooltip("The layer that contains the affected objects by this effect when effectGroup is set to LayerMask.")]
	public LayerMask effectGroupLayer = -1;

	[Tooltip("Only include objects whose names contains this text.")]
	public string effectNameFilter;

	[Tooltip("Combine meshes of all objects in this group affected by Highlight Effect reducing draw calls.")]
	public bool combineMeshes;

	[Tooltip("The alpha threshold for transparent cutout objects. Pixels with alpha below this value will be discarded.")]
	[Range(0f, 1f)]
	public float alphaCutOff;

	[Tooltip("If back facing triangles are ignored.Backfaces triangles are not visible but you may set this property to false to force highlight effects to act on those triangles as well.")]
	public bool cullBackFaces = true;

	[Tooltip("Show highlight effects even if the object is not visible. If this object or its children use GPU Instancing tools, the MeshRenderer can be disabled although the object is visible. In this case, this option is useful to enable highlighting.")]
	public bool ignoreObjectVisibility;

	[Tooltip("Support reflection probes. Enable only if you want the effects to be visible in reflections.")]
	public bool reflectionProbes;

	[Tooltip("Enables GPU instancing. Reduces draw calls in outline and outer glow effects on platforms that support GPU instancing. Should be enabled by default.")]
	public bool GPUInstancing = true;

	[Tooltip("Enables depth buffer clipping. Only applies to outline or outer glow in High Quality mode.")]
	public bool depthClip;

	[Tooltip("Fades out effects based on distance to camera")]
	public bool cameraDistanceFade;

	[Tooltip("The closest distance particles can get to the camera before they fade from the camera’s view.")]
	public float cameraDistanceFadeNear;

	[Tooltip("The farthest distance particles can get away from the camera before they fade from the camera’s view.")]
	public float cameraDistanceFadeFar = 1000f;

	[Tooltip("Normals handling option:\nPreserve original: use original mesh normals.\nSmooth: average normals to produce a smoother outline/glow mesh based effect.\nReorient: recomputes normals based on vertex direction to centroid.")]
	public NormalsOption normalsOption;

	[Tooltip("Ignore highlighting on this object.")]
	public bool ignore;

	[SerializeField]
	private bool _highlighted;

	public float fadeInDuration;

	public float fadeOutDuration;

	public bool flipY;

	[Tooltip("Keeps the outline/glow size unaffected by object distance.")]
	public bool constantWidth = true;

	[Tooltip("Mask to include or exclude certain submeshes. By default, all submeshes are included.")]
	public int subMeshMask = -1;

	[Range(0f, 1f)]
	[Tooltip("Intensity of the overlay effect. A value of 0 disables the overlay completely.")]
	public float overlay;

	[ColorUsage(true, true)]
	public Color overlayColor = Color.yellow;

	public float overlayAnimationSpeed = 1f;

	[Range(0f, 1f)]
	public float overlayMinIntensity = 0.5f;

	[Range(0f, 1f)]
	[Tooltip("Controls the blending or mix of the overlay color with the natural colors of the object.")]
	public float overlayBlending = 1f;

	[Tooltip("Optional overlay texture.")]
	public Texture2D overlayTexture;

	public float overlayTextureScale = 1f;

	[Range(0f, 1f)]
	[Tooltip("Intensity of the outline. A value of 0 disables the outline completely.")]
	public float outline = 1f;

	[ColorUsage(true, true)]
	public Color outlineColor = Color.black;

	public float outlineWidth = 0.45f;

	public QualityLevel outlineQuality = QualityLevel.Medium;

	[Range(1f, 8f)]
	[Tooltip("Reduces the quality of the outline but improves performance a bit.")]
	public int outlineDownsampling = 2;

	public Visibility outlineVisibility;

	public GlowBlendMode glowBlendMode;

	public bool outlineBlitDebug;

	[Tooltip("If enabled, this object won't combine the outline with other objects.")]
	public bool outlineIndependent;

	[Range(0f, 5f)]
	[Tooltip("The intensity of the outer glow effect. A value of 0 disables the glow completely.")]
	public float glow;

	public float glowWidth = 0.4f;

	public QualityLevel glowQuality = QualityLevel.Medium;

	[Range(1f, 8f)]
	[Tooltip("Reduces the quality of the glow but improves performance a bit.")]
	public int glowDownsampling = 2;

	[ColorUsage(true, true)]
	public Color glowHQColor = new Color(0.64f, 1f, 0f, 1f);

	[Tooltip("When enabled, outer glow renders with dithering. When disabled, glow appears as a solid color.")]
	public bool glowDithering = true;

	[Tooltip("Seed for the dithering effect")]
	public float glowMagicNumber1 = 0.75f;

	[Tooltip("Another seed for the dithering effect that combines with first seed to create different patterns")]
	public float glowMagicNumber2 = 0.5f;

	public float glowAnimationSpeed = 1f;

	public Visibility glowVisibility;

	public bool glowBlitDebug;

	[Tooltip("Blends glow passes one after another. If this option is disabled, glow passes won't overlap (in this case, make sure the glow pass 1 has a smaller offset than pass 2, etc.)")]
	public bool glowBlendPasses = true;

	[NonReorderable]
	public GlowPassData[] glowPasses;

	[Tooltip("If enabled, glow effect will not use a stencil mask. This can be used to render the glow effect alone.")]
	public bool glowIgnoreMask;

	[Range(0f, 5f)]
	[Tooltip("The intensity of the inner glow effect. A value of 0 disables the glow completely.")]
	public float innerGlow;

	[Range(0f, 2f)]
	public float innerGlowWidth = 1f;

	[ColorUsage(true, true)]
	public Color innerGlowColor = Color.white;

	public Visibility innerGlowVisibility;

	[Tooltip("Enables the targetFX effect. This effect draws an animated sprite over the object.")]
	public bool targetFX;

	public Texture2D targetFXTexture;

	[ColorUsage(true, true)]
	public Color targetFXColor = Color.white;

	public Transform targetFXCenter;

	public float targetFXRotationSpeed = 50f;

	public float targetFXInitialScale = 4f;

	public float targetFXEndScale = 1.5f;

	[Tooltip("Makes target scale relative to object renderer bounds")]
	public bool targetFXScaleToRenderBounds = true;

	[Tooltip("Places target FX sprite at the bottom of the highlighted object.")]
	public bool targetFXAlignToGround;

	[Tooltip("Fade out effect with altitude")]
	public float targetFXFadePower = 32f;

	public float targetFXGroundMaxDistance = 10f;

	public LayerMask targetFXGroundLayerMask = -1;

	public float targetFXTransitionDuration = 0.5f;

	[Tooltip("The duration of the effect. A value of 0 will keep the target sprite on screen while object is highlighted.")]
	public float targetFXStayDuration = 1.5f;

	public Visibility targetFXVisibility = Visibility.AlwaysOnTop;

	[Tooltip("See-through mode for this Highlight Effect component.")]
	public SeeThroughMode seeThrough = SeeThroughMode.Never;

	[Tooltip("This mask setting let you specify which objects will be considered as occluders and cause the see-through effect for this Highlight Effect component. For example, you assign your walls to a different layer and specify that layer here, so only walls and not other objects, like ground or ceiling, will trigger the see-through effect.")]
	public LayerMask seeThroughOccluderMask = -1;

	[Tooltip("A multiplier for the occluder volume size which can be used to reduce the actual size of occluders when Highlight Effect checks if they're occluding this object.")]
	[Range(0.01f, 0.6f)]
	public float seeThroughOccluderThreshold = 0.3f;

	[Tooltip("Uses stencil buffers to ensure pixel-accurate occlusion test. If this option is disabled, only physics raycasting is used to test for occlusion.")]
	public bool seeThroughOccluderMaskAccurate;

	[Tooltip("The interval of time between occlusion tests.")]
	public float seeThroughOccluderCheckInterval = 1f;

	[Tooltip("If enabled, occlusion test is performed for each children element. If disabled, the bounds of all children is combined and a single occlusion test is performed for the combined bounds.")]
	public bool seeThroughOccluderCheckIndividualObjects;

	[Tooltip("Shows the see-through effect only if the occluder if at this 'offset' distance from the object.")]
	public float seeThroughDepthOffset;

	[Tooltip("Hides the see-through effect if the occluder is further than this distance from the object (0 = infinite)")]
	public float seeThroughMaxDepth;

	[Range(0f, 5f)]
	public float seeThroughIntensity = 0.8f;

	[Range(0f, 1f)]
	public float seeThroughTintAlpha = 0.5f;

	[ColorUsage(true, true)]
	public Color seeThroughTintColor = Color.red;

	[Range(0f, 1f)]
	public float seeThroughNoise = 1f;

	[Range(0f, 1f)]
	public float seeThroughBorder;

	public Color seeThroughBorderColor = Color.black;

	[Tooltip("Only display the border instead of the full see-through effect.")]
	public bool seeThroughBorderOnly;

	public float seeThroughBorderWidth = 0.45f;

	[Tooltip("This option clears the stencil buffer after rendering the see-through effect which results in correct rendering order and supports other stencil-based effects that render afterwards.")]
	public bool seeThroughOrdered;

	[SerializeField]
	[HideInInspector]
	private ModelMaterials[] rms;

	[SerializeField]
	[HideInInspector]
	private int rmsCount;

	[NonSerialized]
	public Transform target;

	[NonSerialized]
	public float highlightStartTime;

	[NonSerialized]
	public float targetFxStartTime;

	[NonSerialized]
	public bool isSelected;

	[NonSerialized]
	public HighlightProfile previousSettings;

	private const float TAU = 0.70711f;

	private static Material fxMatMask;

	private static Material fxMatSolidColor;

	private static Material fxMatSeeThrough;

	private static Material fxMatSeeThroughBorder;

	private static Material fxMatOverlay;

	private static Material fxMatClearStencil;

	private static Material fxMatSeeThroughMask;

	private Material fxMatGlowTemplate;

	private Material fxMatInnerGlow;

	private Material fxMatOutlineTemplate;

	private Material fxMatTarget;

	private Material fxMatComposeGlow;

	private Material fxMatComposeOutline;

	private Material fxMatBlurGlow;

	private Material fxMatBlurOutline;

	private static Vector4[] offsets;

	private float fadeStartTime;

	private FadingState fading;

	private CommandBuffer cbHighlight;

	private int[] mipGlowBuffers;

	private int[] mipOutlineBuffers;

	private int glowRT;

	private int outlineRT;

	private static Mesh quadMesh;

	private static Mesh cubeMesh;

	private int sourceRT;

	private Matrix4x4 quadGlowMatrix;

	private Matrix4x4 quadOutlineMatrix;

	private Vector3[] corners;

	private RenderTextureDescriptor sourceDesc;

	private Color debugColor;

	private Color blackColor;

	private Visibility lastOutlineVisibility;

	private bool requireUpdateMaterial;

	[NonSerialized]
	public static List<HighlightEffect> effects = new List<HighlightEffect>();

	public static bool customSorting;

	private float occlusionCheckLastTime;

	private int occlusionRenderFrame;

	private bool useSmoothGlow;

	private bool useSmoothOutline;

	private bool useSmoothBlend;

	private bool lastOcclusionTestResult;

	private bool useGPUInstancing;

	private MaterialPropertyBlock glowPropertyBlock;

	private MaterialPropertyBlock outlinePropertyBlock;

	private static readonly List<Vector4> matDataDirection = new List<Vector4>();

	private static readonly List<Vector4> matDataGlow = new List<Vector4>();

	private static readonly List<Vector4> matDataColor = new List<Vector4>();

	private static Matrix4x4[] matrices;

	private int outlineOffsetsMin;

	private int outlineOffsetsMax;

	private int glowOffsetsMin;

	private int glowOffsetsMax;

	private static CombineInstance[] combineInstances;

	private Matrix4x4 matrix4x4Identity = Matrix4x4.identity;

	private bool maskRequired;

	private FullScreenBlitMethod FullScreenBlit;

	private Transform overrideRenderers;

	private RenderTargetIdentifier colorAttachmentBuffer;

	private RenderTargetIdentifier depthAttachmentBuffer;

	private readonly List<Renderer> tempRR = new List<Renderer>();

	private static List<Vector3> vertices;

	private static List<Vector3> normals;

	private static Vector3[] newNormals;

	private static int[] matches;

	private static readonly Dictionary<Vector3, int> vv = new Dictionary<Vector3, int>();

	private static readonly Dictionary<int, Mesh> smoothMeshes = new Dictionary<int, Mesh>();

	private static readonly Dictionary<int, Mesh> reorientedMeshes = new Dictionary<int, Mesh>();

	private static readonly Dictionary<int, Mesh> combinedMeshes = new Dictionary<int, Mesh>();

	private static readonly List<Material> rendererSharedMaterials = new List<Material>();

	private int combinedMeshesHashId;

	private const int MAX_VERTEX_COUNT = 65535;

	[Range(0f, 1f)]
	public float hitFxInitialIntensity;

	public HitFxMode hitFxMode;

	public float hitFxFadeOutDuration = 0.25f;

	[ColorUsage(true, true)]
	public Color hitFxColor = Color.white;

	public float hitFxRadius = 0.5f;

	private float hitInitialIntensity;

	private float hitStartTime;

	private float hitFadeOutDuration;

	private Color hitColor;

	private bool hitActive;

	private Vector3 hitPosition;

	private float hitRadius;

	private static readonly List<HighlightSeeThroughOccluder> occluders = new List<HighlightSeeThroughOccluder>();

	private static readonly Dictionary<Camera, int> occludersFrameCount = new Dictionary<Camera, int>();

	private static Material fxMatOccluder;

	private static RaycastHit[] hits;

	private static Collider[] colliders;

	private const int MAX_OCCLUDER_HITS = 50;

	private static RaycastHit[] occluderHits;

	private readonly Dictionary<Camera, List<Renderer>> cachedOccludersPerCamera = new Dictionary<Camera, List<Renderer>>();

	public bool highlighted
	{
		get
		{
			return _highlighted;
		}
		set
		{
			SetHighlighted(value);
		}
	}

	public int includedObjectsCount => rmsCount;

	public event OnObjectHighlightEvent OnObjectHighlightStart;

	public event OnObjectHighlightEvent OnObjectHighlightEnd;

	public event OnRendererHighlightEvent OnRendererHighlightStart;

	public event OnTargetAnimatesEvent OnTargetAnimates;

	public void RestorePreviousHighlightEffectSettings()
	{
		previousSettings.Load(this);
	}

	private void OnEnable()
	{
		effects.Add(this);
	}

	public void Init(Transform override_renderers)
	{
		lastOutlineVisibility = outlineVisibility;
		debugColor = new Color(1f, 0f, 0f, 0.5f);
		blackColor = new Color(0f, 0f, 0f, 0f);
		if (offsets == null || offsets.Length != 8)
		{
			offsets = new Vector4[8]
			{
				new Vector4(0f, 1f),
				new Vector4(1f, 0f),
				new Vector4(0f, -1f),
				new Vector4(-1f, 0f),
				new Vector4(-0.70711f, 0.70711f),
				new Vector4(0.70711f, 0.70711f),
				new Vector4(0.70711f, -0.70711f),
				new Vector4(-0.70711f, -0.70711f)
			};
		}
		if (corners == null || corners.Length != 8)
		{
			corners = new Vector3[8];
		}
		if (quadMesh == null)
		{
			BuildQuad();
		}
		if (cubeMesh == null)
		{
			BuildCube();
		}
		if (target == null)
		{
			target = base.transform;
		}
		if (profileSync && profile != null)
		{
			profile.Load(this);
		}
		if (glowPasses == null || glowPasses.Length == 0)
		{
			glowPasses = new GlowPassData[4];
			glowPasses[0] = new GlowPassData
			{
				offset = 4f,
				alpha = 0.1f,
				color = new Color(0.64f, 1f, 0f, 1f)
			};
			glowPasses[1] = new GlowPassData
			{
				offset = 3f,
				alpha = 0.2f,
				color = new Color(0.64f, 1f, 0f, 1f)
			};
			glowPasses[2] = new GlowPassData
			{
				offset = 2f,
				alpha = 0.3f,
				color = new Color(0.64f, 1f, 0f, 1f)
			};
			glowPasses[3] = new GlowPassData
			{
				offset = 1f,
				alpha = 0.4f,
				color = new Color(0.64f, 1f, 0f, 1f)
			};
		}
		sourceRT = Shader.PropertyToID("_HPSourceRT");
		useGPUInstancing = GPUInstancing && SystemInfo.supportsInstancing;
		if (useGPUInstancing)
		{
			if (glowPropertyBlock == null)
			{
				glowPropertyBlock = new MaterialPropertyBlock();
			}
			if (outlinePropertyBlock == null)
			{
				outlinePropertyBlock = new MaterialPropertyBlock();
			}
		}
		overrideRenderers = override_renderers;
		CheckGeometrySupportDependencies();
		SetupMaterial();
	}

	private void OnDisable()
	{
		UpdateMaterialProperties();
		if (effects != null)
		{
			int num = effects.IndexOf(this);
			if (num >= 0)
			{
				effects.RemoveAt(num);
			}
		}
	}

	private void DestroyMaterial(Material mat)
	{
		if (mat != null)
		{
			UnityEngine.Object.DestroyImmediate(mat);
		}
	}

	private void DestroyMaterialArray(Material[] mm)
	{
		if (mm != null)
		{
			for (int i = 0; i < mm.Length; i++)
			{
				DestroyMaterial(mm[i]);
			}
		}
	}

	private void OnDestroy()
	{
		if (rms != null)
		{
			for (int i = 0; i < rms.Length; i++)
			{
				DestroyMaterialArray(rms[i].fxMatMask);
				DestroyMaterialArray(rms[i].fxMatOutline);
				DestroyMaterialArray(rms[i].fxMatGlow);
				DestroyMaterialArray(rms[i].fxMatSolidColor);
				DestroyMaterialArray(rms[i].fxMatSeeThroughInner);
				DestroyMaterialArray(rms[i].fxMatSeeThroughBorder);
				DestroyMaterialArray(rms[i].fxMatOverlay);
				DestroyMaterialArray(rms[i].fxMatInnerGlow);
			}
		}
		DestroyMaterial(fxMatGlowTemplate);
		DestroyMaterial(fxMatInnerGlow);
		DestroyMaterial(fxMatOutlineTemplate);
		DestroyMaterial(fxMatTarget);
		DestroyMaterial(fxMatComposeGlow);
		DestroyMaterial(fxMatComposeOutline);
		DestroyMaterial(fxMatBlurGlow);
		DestroyMaterial(fxMatBlurOutline);
		if (combinedMeshes.ContainsKey(combinedMeshesHashId))
		{
			combinedMeshes.Remove(combinedMeshesHashId);
		}
	}

	private void Reset()
	{
		SetupMaterial();
	}

	public void ProfileLoad(HighlightProfile profile)
	{
		if (profile != null)
		{
			this.profile = profile;
			profile.Load(this);
		}
	}

	public void ProfileReload()
	{
		if (profile != null)
		{
			profile.Load(this);
		}
	}

	public void ProfileSaveChanges(HighlightProfile profile)
	{
		if (profile != null)
		{
			profile.Save(this);
		}
	}

	public void ProfileSaveChanges()
	{
		if (profile != null)
		{
			profile.Save(this);
		}
	}

	public void Refresh()
	{
		if (base.enabled)
		{
			SetupMaterial();
		}
	}

	public CommandBuffer GetCommandBuffer(Camera cam, RenderTargetIdentifier colorAttachmentBuffer, RenderTargetIdentifier depthAttachmentBuffer, FullScreenBlitMethod fullScreenBlit, bool clearStencil)
	{
		this.colorAttachmentBuffer = colorAttachmentBuffer;
		this.depthAttachmentBuffer = depthAttachmentBuffer;
		FullScreenBlit = fullScreenBlit;
		BuildCommandBuffer(cam, clearStencil);
		return cbHighlight;
	}

	private void BuildCommandBuffer(Camera cam, bool clearStencil)
	{
		if (colorAttachmentBuffer == 0)
		{
			colorAttachmentBuffer = BuiltinRenderTextureType.CameraTarget;
		}
		if (depthAttachmentBuffer == 0)
		{
			depthAttachmentBuffer = BuiltinRenderTextureType.CameraTarget;
		}
		if (cam == null || cbHighlight == null)
		{
			return;
		}
		cbHighlight.Clear();
		if (!reflectionProbes && cam.cameraType == CameraType.Reflection)
		{
			return;
		}
		cbHighlight.SetRenderTarget(colorAttachmentBuffer, depthAttachmentBuffer);
		if (requireUpdateMaterial)
		{
			requireUpdateMaterial = false;
			UpdateMaterialProperties();
		}
		bool flag = true;
		if (clearStencil)
		{
			cbHighlight.DrawMesh(quadMesh, matrix4x4Identity, fxMatClearStencil, 0, 0);
			flag = false;
		}
		bool flag2 = seeThroughIntensity > 0f && (seeThrough == SeeThroughMode.AlwaysWhenOccluded || (seeThrough == SeeThroughMode.WhenHighlighted && _highlighted));
		if (flag2)
		{
			flag2 = RenderSeeThroughOccluders(cbHighlight, cam);
			if (flag2 && (int)seeThroughOccluderMask != -1)
			{
				if (seeThroughOccluderMaskAccurate)
				{
					CheckOcclusionAccurate(cbHighlight, cam);
				}
				else
				{
					flag2 = CheckOcclusion(cam);
				}
			}
		}
		if (!_highlighted && !flag2 && !hitActive)
		{
			return;
		}
		int cullingMask = cam.cullingMask;
		if (!ignoreObjectVisibility)
		{
			for (int i = 0; i < rmsCount; i++)
			{
				if (rms[i].renderer != null && rms[i].renderer.isVisible != rms[i].renderWasVisibleDuringSetup)
				{
					SetupMaterial();
					break;
				}
			}
		}
		float num = (_highlighted ? glow : 0f);
		if (fxMatMask == null)
		{
			return;
		}
		Visibility visibility = glowVisibility;
		Visibility visibility2 = outlineVisibility;
		float aspect = cam.aspect;
		bool flag3 = false;
		for (int j = 0; j < rmsCount; j++)
		{
			rms[j].render = false;
			Transform transform = rms[j].transform;
			if (transform == null)
			{
				continue;
			}
			Mesh mesh = rms[j].mesh;
			if (mesh == null)
			{
				continue;
			}
			if (!ignoreObjectVisibility)
			{
				int layer = transform.gameObject.layer;
				if (((1 << layer) & cullingMask) == 0 || !rms[j].renderer.isVisible)
				{
					continue;
				}
			}
			rms[j].render = true;
			flag3 = true;
			if (rms[j].isCombined)
			{
				rms[j].renderingMatrix = transform.localToWorldMatrix;
			}
			if (!outlineIndependent)
			{
				continue;
			}
			if (useSmoothBlend)
			{
				if (flag)
				{
					flag = false;
					cbHighlight.DrawMesh(quadMesh, matrix4x4Identity, fxMatClearStencil, 0, 0);
				}
			}
			else
			{
				if (!(outline > 0f) && !(glow > 0f))
				{
					continue;
				}
				float num2 = outlineWidth;
				if (glow > 0f)
				{
					num2 = Mathf.Max(num2, glowWidth);
				}
				for (int k = 0; k < mesh.subMeshCount; k++)
				{
					if (((1 << k) & subMeshMask) == 0)
					{
						continue;
					}
					if (outlineQuality.UsesMultipleOffsets())
					{
						for (int l = outlineOffsetsMin; l <= outlineOffsetsMax; l++)
						{
							Vector4 value = offsets[l] * (num2 / 100f);
							value.y *= aspect;
							cbHighlight.SetGlobalVector(ShaderParams.OutlineDirection, value);
							if (rms[j].isCombined)
							{
								cbHighlight.DrawMesh(rms[j].mesh, rms[j].renderingMatrix, rms[j].fxMatOutline[k], k, 1);
							}
							else
							{
								cbHighlight.DrawRenderer(rms[j].renderer, rms[j].fxMatOutline[k], k, 1);
							}
						}
					}
					else
					{
						cbHighlight.SetGlobalVector(ShaderParams.OutlineDirection, Vector4.zero);
						if (rms[j].isCombined)
						{
							cbHighlight.DrawMesh(rms[j].mesh, rms[j].renderingMatrix, rms[j].fxMatOutline[k], k, 1);
						}
						else
						{
							cbHighlight.DrawRenderer(rms[j].renderer, rms[j].fxMatOutline[k], k, 1);
						}
					}
				}
			}
		}
		bool flag4 = _highlighted && ((outline > 0f && visibility2 != Visibility.Normal) || (glow > 0f && visibility != Visibility.Normal) || (innerGlow > 0f && innerGlowVisibility != Visibility.Normal));
		if (maskRequired)
		{
			for (int m = 0; m < rmsCount; m++)
			{
				if (rms[m].render)
				{
					RenderMask(m, rms[m].mesh, flag4);
				}
			}
		}
		float num3 = 1f;
		float num4 = 1f;
		if (fading != FadingState.NoFading)
		{
			if (fading == FadingState.FadingIn)
			{
				if (fadeInDuration > 0f)
				{
					num3 = (Time.time - fadeStartTime) / fadeInDuration;
					if (num3 > 1f)
					{
						num3 = 1f;
						fading = FadingState.NoFading;
					}
				}
			}
			else if (fadeOutDuration > 0f)
			{
				num3 = 1f - (Time.time - fadeStartTime) / fadeOutDuration;
				if (num3 < 0f)
				{
					num3 = 0f;
					fading = FadingState.NoFading;
					_highlighted = false;
					if (this.OnObjectHighlightEnd != null)
					{
						this.OnObjectHighlightEnd(base.gameObject);
					}
					SendMessage("HighlightEnd", null, SendMessageOptions.DontRequireReceiver);
				}
			}
		}
		if (glowQuality == QualityLevel.High)
		{
			num *= 0.25f;
		}
		else if (glowQuality == QualityLevel.Medium)
		{
			num *= 0.5f;
		}
		bool flag5 = false;
		bool flag6 = seeThroughBorder * seeThroughBorderWidth > 0f;
		for (int n = 0; n < rmsCount; n++)
		{
			if (!rms[n].render)
			{
				continue;
			}
			Mesh mesh2 = rms[n].mesh;
			num4 = num3;
			if (cameraDistanceFade)
			{
				num4 *= ComputeCameraDistanceFade(rms[n].transform.position, cam.transform);
			}
			if (_highlighted || hitActive)
			{
				Color color = overlayColor;
				float y = overlayMinIntensity;
				float z = overlayBlending;
				Color value2 = innerGlowColor;
				float num5 = innerGlow;
				if (hitActive)
				{
					color.a = (_highlighted ? overlay : 0f);
					value2.a = (_highlighted ? num5 : 0f);
					float num6 = ((hitFadeOutDuration > 0f) ? ((Time.time - hitStartTime) / hitFadeOutDuration) : 1f);
					if (num6 >= 1f)
					{
						hitActive = false;
					}
					else if (hitFxMode == HitFxMode.InnerGlow)
					{
						bool flag7 = _highlighted && num5 > 0f;
						value2 = (flag7 ? Color.Lerp(hitColor, innerGlowColor, num6) : hitColor);
						value2.a = (flag7 ? Mathf.Lerp(1f - num6, num5, num6) : (1f - num6));
						value2.a *= hitInitialIntensity;
					}
					else
					{
						bool flag8 = _highlighted && overlay > 0f;
						color = (flag8 ? Color.Lerp(hitColor, color, num6) : hitColor);
						color.a = (flag8 ? Mathf.Lerp(1f - num6, overlay, num6) : (1f - num6));
						color.a *= hitInitialIntensity;
						y = 1f;
						z = 0f;
					}
				}
				else
				{
					color.a = overlay * num4;
					value2.a = num5 * num4;
				}
				for (int num7 = 0; num7 < mesh2.subMeshCount; num7++)
				{
					if (((1 << num7) & subMeshMask) == 0)
					{
						continue;
					}
					if (color.a > 0f)
					{
						Material material = rms[n].fxMatOverlay[num7];
						material.SetColor(ShaderParams.OverlayColor, color);
						material.SetVector(ShaderParams.OverlayData, new Vector4(overlayAnimationSpeed, y, z, overlayTextureScale));
						if (hitActive && hitFxMode == HitFxMode.LocalHit)
						{
							material.SetVector(ShaderParams.OverlayHitPosData, new Vector4(hitPosition.x, hitPosition.y, hitPosition.z, hitRadius));
							material.SetFloat(ShaderParams.OverlayHitStartTime, hitStartTime);
						}
						else
						{
							material.SetVector(ShaderParams.OverlayHitPosData, Vector4.zero);
						}
						if (rms[n].isCombined)
						{
							cbHighlight.DrawMesh(rms[n].mesh, rms[n].renderingMatrix, rms[n].fxMatOverlay[num7], num7);
						}
						else
						{
							cbHighlight.DrawRenderer(rms[n].renderer, rms[n].fxMatOverlay[num7], num7);
						}
					}
					if (value2.a > 0f)
					{
						rms[n].fxMatInnerGlow[num7].SetColor(ShaderParams.InnerGlowColor, value2);
						if (rms[n].isCombined)
						{
							cbHighlight.DrawMesh(rms[n].mesh, rms[n].renderingMatrix, rms[n].fxMatInnerGlow[num7], num7);
						}
						else
						{
							cbHighlight.DrawRenderer(rms[n].renderer, rms[n].fxMatInnerGlow[num7], num7);
						}
					}
				}
			}
			if (!_highlighted)
			{
				continue;
			}
			bool flag9 = useGPUInstancing && !rms[n].isSkinnedMesh;
			for (int num8 = 0; num8 < mesh2.subMeshCount; num8++)
			{
				if (((1 << num8) & subMeshMask) == 0)
				{
					continue;
				}
				if (glow > 0f && glowQuality != QualityLevel.Highest)
				{
					matDataGlow.Clear();
					matDataColor.Clear();
					matDataDirection.Clear();
					for (int num9 = 0; num9 < glowPasses.Length; num9++)
					{
						if (glowQuality.UsesMultipleOffsets())
						{
							for (int num10 = glowOffsetsMin; num10 <= glowOffsetsMax; num10++)
							{
								Vector4 vector = offsets[num10];
								vector.y *= aspect;
								Color color2 = glowPasses[num9].color;
								Vector4 vector2 = new Vector4(num4 * num * glowPasses[num9].alpha, glowPasses[num9].offset * glowWidth / 100f, glowMagicNumber1, glowMagicNumber2);
								if (flag9)
								{
									matDataDirection.Add(vector);
									matDataGlow.Add(vector2);
									matDataColor.Add(new Vector4(color2.r, color2.g, color2.b, color2.a));
									continue;
								}
								cbHighlight.SetGlobalVector(ShaderParams.GlowDirection, vector);
								cbHighlight.SetGlobalColor(ShaderParams.GlowColor, color2);
								cbHighlight.SetGlobalVector(ShaderParams.Glow, vector2);
								if (rms[n].isCombined)
								{
									cbHighlight.DrawMesh(rms[n].mesh, rms[n].renderingMatrix, rms[n].fxMatGlow[num8], num8);
								}
								else
								{
									cbHighlight.DrawRenderer(rms[n].renderer, rms[n].fxMatGlow[num8], num8);
								}
							}
							continue;
						}
						Vector4 vector3 = new Vector4(num4 * num * glowPasses[num9].alpha, glowPasses[num9].offset * glowWidth / 100f, glowMagicNumber1, glowMagicNumber2);
						Color color3 = glowPasses[num9].color;
						if (flag9)
						{
							matDataDirection.Add(Vector4.zero);
							matDataGlow.Add(vector3);
							matDataColor.Add(new Vector4(color3.r, color3.g, color3.b, color3.a));
							continue;
						}
						cbHighlight.SetGlobalColor(ShaderParams.GlowColor, color3);
						cbHighlight.SetGlobalVector(ShaderParams.Glow, vector3);
						cbHighlight.SetGlobalVector(ShaderParams.GlowDirection, Vector4.zero);
						if (rms[n].isCombined)
						{
							cbHighlight.DrawMesh(rms[n].mesh, rms[n].renderingMatrix, rms[n].fxMatGlow[num8], num8);
						}
						else
						{
							cbHighlight.DrawRenderer(rms[n].renderer, rms[n].fxMatGlow[num8], num8);
						}
					}
					if (flag9)
					{
						int count = matDataDirection.Count;
						if (count > 0)
						{
							glowPropertyBlock.Clear();
							glowPropertyBlock.SetVectorArray(ShaderParams.GlowDirection, matDataDirection);
							glowPropertyBlock.SetVectorArray(ShaderParams.GlowColor, matDataColor);
							glowPropertyBlock.SetVectorArray(ShaderParams.Glow, matDataGlow);
							if (matrices == null || matrices.Length < count)
							{
								matrices = new Matrix4x4[count];
							}
							if (rms[n].isCombined)
							{
								for (int num11 = 0; num11 < count; num11++)
								{
									matrices[num11] = rms[n].renderingMatrix;
								}
							}
							else
							{
								Matrix4x4 localToWorldMatrix = rms[n].transform.localToWorldMatrix;
								for (int num12 = 0; num12 < count; num12++)
								{
									matrices[num12] = localToWorldMatrix;
								}
							}
							cbHighlight.DrawMeshInstanced(mesh2, num8, rms[n].fxMatGlow[num8], 0, matrices, count, glowPropertyBlock);
						}
					}
				}
				if (!(outline > 0f) || outlineQuality == QualityLevel.Highest)
				{
					continue;
				}
				Color value3 = outlineColor;
				value3.a = outline * num4;
				if (outlineQuality.UsesMultipleOffsets())
				{
					matDataDirection.Clear();
					for (int num13 = outlineOffsetsMin; num13 <= outlineOffsetsMax; num13++)
					{
						cbHighlight.SetGlobalColor(ShaderParams.OutlineColor, value3);
						Vector4 vector4 = offsets[num13] * (outlineWidth / 100f);
						vector4.y *= aspect;
						if (flag9)
						{
							matDataDirection.Add(vector4);
							continue;
						}
						cbHighlight.SetGlobalVector(ShaderParams.OutlineDirection, vector4);
						if (rms[n].isCombined)
						{
							cbHighlight.DrawMesh(rms[n].mesh, rms[n].renderingMatrix, rms[n].fxMatOutline[num8], num8, 0);
						}
						else
						{
							cbHighlight.DrawRenderer(rms[n].renderer, rms[n].fxMatOutline[num8], num8, 0);
						}
					}
					if (!flag9)
					{
						continue;
					}
					int count2 = matDataDirection.Count;
					if (count2 <= 0)
					{
						continue;
					}
					outlinePropertyBlock.Clear();
					outlinePropertyBlock.SetVectorArray(ShaderParams.OutlineDirection, matDataDirection);
					if (matrices == null || matrices.Length < count2)
					{
						matrices = new Matrix4x4[count2];
					}
					if (rms[n].isCombined)
					{
						for (int num14 = 0; num14 < count2; num14++)
						{
							matrices[num14] = rms[n].renderingMatrix;
						}
					}
					else
					{
						Matrix4x4 localToWorldMatrix2 = rms[n].transform.localToWorldMatrix;
						for (int num15 = 0; num15 < count2; num15++)
						{
							matrices[num15] = localToWorldMatrix2;
						}
					}
					cbHighlight.DrawMeshInstanced(mesh2, num8, rms[n].fxMatOutline[num8], 0, matrices, count2, outlinePropertyBlock);
				}
				else
				{
					cbHighlight.SetGlobalColor(ShaderParams.OutlineColor, value3);
					cbHighlight.SetGlobalVector(ShaderParams.OutlineDirection, Vector4.zero);
					if (rms[n].isSkinnedMesh)
					{
						cbHighlight.DrawRenderer(rms[n].renderer, rms[n].fxMatOutline[num8], num8, 0);
					}
					else
					{
						cbHighlight.DrawMesh(mesh2, rms[n].transform.localToWorldMatrix, rms[n].fxMatOutline[num8], num8, 0);
					}
				}
			}
			if (!targetFX)
			{
				continue;
			}
			float num16 = 1f;
			if (targetFXStayDuration > 0f && Application.isPlaying)
			{
				num16 = Time.time - targetFxStartTime;
				if (num16 >= targetFXStayDuration)
				{
					num16 -= targetFXStayDuration;
					num16 = 1f - num16;
				}
				if (num16 > 1f)
				{
					num16 = 1f;
				}
			}
			bool flag10 = targetFXCenter != null;
			if (!(num16 > 0f) || (flag5 && flag10))
			{
				continue;
			}
			flag5 = true;
			float t = 1f;
			float num17 = 0f;
			float num18;
			if (Application.isPlaying)
			{
				num17 = (Time.time - targetFxStartTime) / targetFXTransitionDuration;
				if (num17 > 1f)
				{
					num17 = 1f;
				}
				t = Mathf.Sin(num17 * MathF.PI * 0.5f);
				num18 = Time.time;
			}
			else
			{
				num18 = (float)DateTime.Now.Subtract(DateTime.Today).TotalSeconds;
			}
			Bounds bounds = rms[n].renderer.bounds;
			if (!targetFXScaleToRenderBounds)
			{
				bounds.size = Vector3.one;
			}
			Vector3 size = bounds.size;
			float num19 = size.x;
			if (size.y < num19)
			{
				num19 = size.y;
			}
			if (size.z < num19)
			{
				num19 = size.z;
			}
			size.x = (size.y = (size.z = num19));
			size = Vector3.Lerp(size * targetFXInitialScale, size * targetFXEndScale, t);
			Vector3 center = (flag10 ? targetFXCenter.transform.position : bounds.center);
			if (targetFXAlignToGround)
			{
				Quaternion quaternion = Quaternion.Euler(90f, 0f, 0f);
				center.y += 0.5f;
				if (Physics.Raycast(center, Vector3.down, out var hitInfo, targetFXGroundMaxDistance, targetFXGroundLayerMask))
				{
					center = hitInfo.point;
					center.y += 0.01f;
					Vector4 value4 = hitInfo.normal;
					value4.w = targetFXFadePower;
					fxMatTarget.SetVector(ShaderParams.TargetFXRenderData, value4);
					quaternion = Quaternion.Euler(0f, num18 * targetFXRotationSpeed, 0f);
					if (this.OnTargetAnimates != null)
					{
						this.OnTargetAnimates(ref center, ref quaternion, ref size, num17);
					}
					Matrix4x4 matrix = Matrix4x4.TRS(center, quaternion, size);
					Color color4 = targetFXColor;
					color4.a *= num4 * num16;
					fxMatTarget.color = color4;
					cbHighlight.DrawMesh(cubeMesh, matrix, fxMatTarget, 0, 0);
				}
			}
			else
			{
				Quaternion quaternion = Quaternion.LookRotation(cam.transform.position - rms[n].transform.position);
				Quaternion quaternion2 = Quaternion.Euler(0f, 0f, num18 * targetFXRotationSpeed);
				quaternion *= quaternion2;
				if (this.OnTargetAnimates != null)
				{
					this.OnTargetAnimates(ref center, ref quaternion, ref size, num17);
				}
				Matrix4x4 matrix2 = Matrix4x4.TRS(center, quaternion, size);
				Color color5 = targetFXColor;
				color5.a *= num4 * num16;
				fxMatTarget.color = color5;
				cbHighlight.DrawMesh(quadMesh, matrix2, fxMatTarget, 0, 1);
			}
		}
		if (useSmoothBlend && _highlighted && flag3)
		{
			int num20 = 0;
			int num21 = 0;
			Bounds bounds2 = default(Bounds);
			num20 = cam.pixelWidth;
			num21 = cam.pixelHeight;
			if (num21 <= 0)
			{
				num21 = 1;
			}
			if (XRSettings.enabled && Application.isPlaying)
			{
				sourceDesc = XRSettings.eyeTextureDesc;
			}
			else
			{
				sourceDesc = new RenderTextureDescriptor(num20, num21, Application.isMobilePlatform ? RenderTextureFormat.Default : RenderTextureFormat.DefaultHDR);
				sourceDesc.volumeDepth = 1;
			}
			sourceDesc.msaaSamples = 1;
			sourceDesc.useMipMap = false;
			sourceDesc.depthBufferBits = 0;
			cbHighlight.GetTemporaryRT(sourceRT, sourceDesc, FilterMode.Bilinear);
			RenderTargetIdentifier renderTarget = new RenderTargetIdentifier(sourceRT, 0, CubemapFace.Unknown, -1);
			cbHighlight.SetRenderTarget(renderTarget);
			cbHighlight.ClearRenderTarget(clearDepth: false, clearColor: true, new Color(0f, 0f, 0f, 0f));
			for (int num22 = 0; num22 < rmsCount; num22++)
			{
				if (!rms[num22].render)
				{
					continue;
				}
				if (num22 == 0)
				{
					bounds2 = rms[num22].renderer.bounds;
				}
				else
				{
					bounds2.Encapsulate(rms[num22].renderer.bounds);
				}
				Mesh mesh3 = rms[num22].mesh;
				for (int num23 = 0; num23 < mesh3.subMeshCount; num23++)
				{
					if (((1 << num23) & subMeshMask) != 0 && num23 < rms[num22].fxMatSolidColor.Length)
					{
						if (rms[num22].isCombined)
						{
							cbHighlight.DrawMesh(rms[num22].mesh, rms[num22].renderingMatrix, rms[num22].fxMatSolidColor[num23], num23);
						}
						else
						{
							cbHighlight.DrawRenderer(rms[num22].renderer, rms[num22].fxMatSolidColor[num23], num23);
						}
					}
				}
			}
			if (ComputeSmoothQuadMatrix(cam, bounds2))
			{
				if (useSmoothGlow)
				{
					float num24 = glow * num4;
					fxMatComposeGlow.color = new Color(glowHQColor.r * num24, glowHQColor.g * num24, glowHQColor.b * num24, glowHQColor.a * num24);
					SmoothGlow(num20 / glowDownsampling, num21 / glowDownsampling);
				}
				if (useSmoothOutline)
				{
					float num25 = outline * num4;
					fxMatComposeOutline.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a * num25 * 10f);
					SmoothOutline(num20 / outlineDownsampling, num21 / outlineDownsampling);
				}
				ComposeSmoothBlend(visibility, visibility2);
			}
		}
		if (!flag2)
		{
			return;
		}
		if (flag4)
		{
			for (int num26 = 0; num26 < rmsCount; num26++)
			{
				if (rms[num26].render)
				{
					Mesh mesh4 = rms[num26].mesh;
					RenderSeeThroughClearStencil(num26, mesh4);
				}
			}
			for (int num27 = 0; num27 < rmsCount; num27++)
			{
				if (rms[num27].render)
				{
					Mesh mesh5 = rms[num27].mesh;
					RenderSeeThroughMask(num27, mesh5);
				}
			}
		}
		for (int num28 = 0; num28 < rmsCount; num28++)
		{
			if (!rms[num28].render)
			{
				continue;
			}
			Mesh mesh6 = rms[num28].mesh;
			for (int num29 = 0; num29 < mesh6.subMeshCount; num29++)
			{
				if (((1 << num29) & subMeshMask) == 0)
				{
					continue;
				}
				if (num29 < rms[num28].fxMatSeeThroughInner.Length && rms[num28].fxMatSeeThroughInner[num29] != null)
				{
					if (rms[num28].isCombined)
					{
						cbHighlight.DrawMesh(mesh6, rms[num28].renderingMatrix, rms[num28].fxMatSeeThroughInner[num29], num29);
					}
					else
					{
						cbHighlight.DrawRenderer(rms[num28].renderer, rms[num28].fxMatSeeThroughInner[num29], num29);
					}
				}
				if (flag6)
				{
					if (rms[num28].isCombined)
					{
						cbHighlight.DrawMesh(mesh6, rms[num28].renderingMatrix, rms[num28].fxMatSeeThroughBorder[num29], num29);
					}
					else
					{
						cbHighlight.DrawRenderer(rms[num28].renderer, rms[num28].fxMatSeeThroughBorder[num29], num29);
					}
				}
			}
		}
		if (!seeThroughOrdered)
		{
			return;
		}
		for (int num30 = 0; num30 < rmsCount; num30++)
		{
			if (!rms[num30].render)
			{
				continue;
			}
			Mesh mesh7 = rms[num30].mesh;
			for (int num31 = 0; num31 < mesh7.subMeshCount; num31++)
			{
				if (((1 << num31) & subMeshMask) != 0)
				{
					if (rms[num30].isCombined)
					{
						cbHighlight.DrawMesh(mesh7, rms[num30].renderingMatrix, fxMatClearStencil, num31, 1);
					}
					else
					{
						cbHighlight.DrawRenderer(rms[num30].renderer, fxMatClearStencil, num31, 1);
					}
				}
			}
		}
	}

	private void RenderMask(int k, Mesh mesh, bool renderMaskOnTop)
	{
		for (int i = 0; i < mesh.subMeshCount; i++)
		{
			if (((1 << i) & subMeshMask) != 0)
			{
				if (renderMaskOnTop)
				{
					rms[k].fxMatMask[i].SetInt(ShaderParams.ZTest, 8);
				}
				else
				{
					rms[k].fxMatMask[i].SetInt(ShaderParams.ZTest, 4);
				}
				if (rms[k].isCombined)
				{
					cbHighlight.DrawMesh(rms[k].mesh, rms[k].renderingMatrix, rms[k].fxMatMask[i], i, 0);
				}
				else
				{
					cbHighlight.DrawRenderer(rms[k].renderer, rms[k].fxMatMask[i], i, 0);
				}
			}
		}
	}

	private void RenderSeeThroughClearStencil(int k, Mesh mesh)
	{
		if (rms[k].isCombined)
		{
			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				if (((1 << i) & subMeshMask) != 0)
				{
					cbHighlight.DrawMesh(mesh, rms[k].renderingMatrix, fxMatClearStencil, i, 1);
				}
			}
			return;
		}
		for (int j = 0; j < mesh.subMeshCount; j++)
		{
			if (((1 << j) & subMeshMask) != 0)
			{
				cbHighlight.DrawRenderer(rms[k].renderer, fxMatClearStencil, j, 1);
			}
		}
	}

	private void RenderSeeThroughMask(int k, Mesh mesh)
	{
		if (rms[k].isCombined)
		{
			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				if (((1 << i) & subMeshMask) != 0)
				{
					cbHighlight.DrawMesh(mesh, rms[k].renderingMatrix, rms[k].fxMatMask[i], i, 1);
				}
			}
			return;
		}
		for (int j = 0; j < mesh.subMeshCount; j++)
		{
			if (((1 << j) & subMeshMask) != 0)
			{
				cbHighlight.DrawRenderer(rms[k].renderer, rms[k].fxMatMask[j], j, 1);
			}
		}
	}

	private bool ComputeSmoothQuadMatrix(Camera cam, Bounds bounds)
	{
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		corners[0] = new Vector3(min.x, min.y, min.z);
		corners[1] = new Vector3(min.x, min.y, max.z);
		corners[2] = new Vector3(max.x, min.y, min.z);
		corners[3] = new Vector3(max.x, min.y, max.z);
		corners[4] = new Vector3(min.x, max.y, min.z);
		corners[5] = new Vector3(min.x, max.y, max.z);
		corners[6] = new Vector3(max.x, max.y, min.z);
		corners[7] = new Vector3(max.x, max.y, max.z);
		Vector3 scrMin = new Vector3(float.MaxValue, float.MaxValue, 0f);
		Vector3 scrMax = new Vector3(float.MinValue, float.MinValue, 0f);
		float num = float.MaxValue;
		for (int i = 0; i < corners.Length; i++)
		{
			corners[i] = cam.WorldToScreenPoint(corners[i]);
			if (corners[i].x < scrMin.x)
			{
				scrMin.x = corners[i].x;
			}
			if (corners[i].y < scrMin.y)
			{
				scrMin.y = corners[i].y;
			}
			if (corners[i].x > scrMax.x)
			{
				scrMax.x = corners[i].x;
			}
			if (corners[i].y > scrMax.y)
			{
				scrMax.y = corners[i].y;
			}
			if (corners[i].z < num)
			{
				num = corners[i].z;
				if (num < cam.nearClipPlane)
				{
					scrMin.x = (scrMin.y = 0f);
					scrMax.x = cam.pixelWidth;
					scrMax.y = cam.pixelHeight;
					break;
				}
			}
		}
		if (scrMax.y == scrMin.y)
		{
			return false;
		}
		if (num < cam.nearClipPlane)
		{
			num = cam.nearClipPlane + 0.01f;
		}
		scrMin.z = (scrMax.z = num);
		if (outline > 0f)
		{
			BuildMatrix(cam, scrMin, scrMax, (int)(10f + 20f * outlineWidth + (float)(5 * outlineDownsampling)), ref quadOutlineMatrix);
		}
		if (glow > 0f)
		{
			BuildMatrix(cam, scrMin, scrMax, (int)(20f + 30f * glowWidth + (float)(10 * glowDownsampling)), ref quadGlowMatrix);
		}
		return true;
	}

	private void BuildMatrix(Camera cam, Vector3 scrMin, Vector3 scrMax, int border, ref Matrix4x4 quadMatrix)
	{
		scrMin.x -= border;
		scrMin.y -= border;
		scrMax.x += border;
		scrMax.y += border;
		Vector3 position = new Vector3(scrMax.x, scrMin.y, scrMin.z);
		scrMin = cam.ScreenToWorldPoint(scrMin);
		scrMax = cam.ScreenToWorldPoint(scrMax);
		position = cam.ScreenToWorldPoint(position);
		float x = Vector3.Distance(scrMin, position);
		float y = Vector3.Distance(scrMax, position);
		quadMatrix = Matrix4x4.TRS((scrMin + scrMax) * 0.5f, cam.transform.rotation, new Vector3(x, y, 1f));
	}

	private void SmoothGlow(int rtWidth, int rtHeight)
	{
		int num = 8;
		if (mipGlowBuffers == null || mipGlowBuffers.Length != num)
		{
			mipGlowBuffers = new int[num];
			for (int i = 0; i < num; i++)
			{
				mipGlowBuffers[i] = Shader.PropertyToID("_HPSmoothGlowTemp" + i);
			}
			glowRT = Shader.PropertyToID("_HPComposeGlowFinal");
			mipGlowBuffers[num - 2] = glowRT;
		}
		RenderTextureDescriptor desc = sourceDesc;
		desc.depthBufferBits = 0;
		for (int j = 0; j < num; j++)
		{
			float num2 = j / 2 + 2;
			int num3 = (int)((float)rtWidth / num2);
			int num4 = (int)((float)rtHeight / num2);
			if (num3 <= 0)
			{
				num3 = 1;
			}
			if (num4 <= 0)
			{
				num4 = 1;
			}
			desc.width = num3;
			desc.height = num4;
			cbHighlight.GetTemporaryRT(mipGlowBuffers[j], desc, FilterMode.Bilinear);
		}
		for (int k = 0; k < num - 1; k += 2)
		{
			if (k == 0)
			{
				FullScreenBlit(cbHighlight, sourceRT, mipGlowBuffers[k + 1], fxMatBlurGlow, 0);
			}
			else
			{
				FullScreenBlit(cbHighlight, mipGlowBuffers[k], mipGlowBuffers[k + 1], fxMatBlurGlow, 0);
			}
			FullScreenBlit(cbHighlight, mipGlowBuffers[k + 1], mipGlowBuffers[k], fxMatBlurGlow, 1);
			if (k < num - 2)
			{
				FullScreenBlit(cbHighlight, mipGlowBuffers[k], mipGlowBuffers[k + 2], fxMatBlurGlow, 2);
			}
		}
	}

	private void SmoothOutline(int rtWidth, int rtHeight)
	{
		int num = 4;
		if (mipOutlineBuffers == null || mipOutlineBuffers.Length != num)
		{
			mipOutlineBuffers = new int[num];
			for (int i = 0; i < num; i++)
			{
				mipOutlineBuffers[i] = Shader.PropertyToID("_HPSmoothOutlineTemp" + i);
			}
			outlineRT = Shader.PropertyToID("_HPComposeOutlineFinal");
			mipOutlineBuffers[num - 2] = outlineRT;
		}
		RenderTextureDescriptor desc = sourceDesc;
		desc.depthBufferBits = 0;
		for (int j = 0; j < num; j++)
		{
			float num2 = j / 2 + 2;
			int num3 = (int)((float)rtWidth / num2);
			int num4 = (int)((float)rtHeight / num2);
			if (num3 <= 0)
			{
				num3 = 1;
			}
			if (num4 <= 0)
			{
				num4 = 1;
			}
			desc.width = num3;
			desc.height = num4;
			cbHighlight.GetTemporaryRT(mipOutlineBuffers[j], desc, FilterMode.Bilinear);
		}
		for (int k = 0; k < num - 1; k += 2)
		{
			if (k == 0)
			{
				FullScreenBlit(cbHighlight, sourceRT, mipOutlineBuffers[k + 1], fxMatBlurOutline, 0);
			}
			else
			{
				FullScreenBlit(cbHighlight, mipOutlineBuffers[k], mipOutlineBuffers[k + 1], fxMatBlurOutline, 0);
			}
			FullScreenBlit(cbHighlight, mipOutlineBuffers[k + 1], mipOutlineBuffers[k], fxMatBlurOutline, 1);
			if (k < num - 2)
			{
				FullScreenBlit(cbHighlight, mipOutlineBuffers[k], mipOutlineBuffers[k + 2], fxMatBlurOutline, 2);
			}
		}
	}

	private void ComposeSmoothBlend(Visibility smoothGlowVisibility, Visibility smoothOutlineVisibility)
	{
		cbHighlight.SetRenderTarget(colorAttachmentBuffer, depthAttachmentBuffer);
		int num;
		if (glow > 0f)
		{
			num = ((glowQuality == QualityLevel.Highest) ? 1 : 0);
			if (num != 0)
			{
				fxMatComposeGlow.SetVector(ShaderParams.Flip, (XRSettings.enabled && flipY) ? new Vector4(1f, -1f) : new Vector4(0f, 1f));
				fxMatComposeGlow.SetInt(ShaderParams.ZTest, GetZTestValue(smoothGlowVisibility));
				fxMatComposeGlow.SetColor(ShaderParams.Debug, glowBlitDebug ? debugColor : blackColor);
				fxMatComposeGlow.SetInt(ShaderParams.GlowStencilComp, glowIgnoreMask ? 8 : 6);
				cbHighlight.DrawMesh(quadMesh, quadGlowMatrix, fxMatComposeGlow, 0, 0);
			}
		}
		else
		{
			num = 0;
		}
		bool flag = outline > 0f && outlineQuality == QualityLevel.Highest;
		if (flag)
		{
			fxMatComposeOutline.SetVector(ShaderParams.Flip, (XRSettings.enabled && flipY) ? new Vector4(1f, -1f) : new Vector4(0f, 1f));
			fxMatComposeOutline.SetInt(ShaderParams.ZTest, GetZTestValue(smoothOutlineVisibility));
			fxMatComposeOutline.SetColor(ShaderParams.Debug, outlineBlitDebug ? debugColor : blackColor);
			cbHighlight.DrawMesh(quadMesh, quadOutlineMatrix, fxMatComposeOutline, 0, 0);
		}
		if (num != 0)
		{
			for (int i = 0; i < mipGlowBuffers.Length; i++)
			{
				cbHighlight.ReleaseTemporaryRT(mipGlowBuffers[i]);
			}
		}
		if (flag)
		{
			for (int j = 0; j < mipOutlineBuffers.Length; j++)
			{
				cbHighlight.ReleaseTemporaryRT(mipOutlineBuffers[j]);
			}
		}
		cbHighlight.ReleaseTemporaryRT(sourceRT);
	}

	private void InitMaterial(ref Material material, string shaderName)
	{
		if (!(material != null))
		{
			Shader shader = Shader.Find(shaderName);
			if (shader == null)
			{
				Debug.LogError("Shader " + shaderName + " not found.");
				base.enabled = false;
			}
			else
			{
				material = new Material(shader);
			}
		}
	}

	private void Fork(Material mat, ref Material[] mats, Mesh mesh)
	{
		if (!(mesh == null))
		{
			int subMeshCount = mesh.subMeshCount;
			Fork(mat, ref mats, subMeshCount);
		}
	}

	private void Fork(Material material, ref Material[] array, int count)
	{
		if (array == null || array.Length < count)
		{
			DestroyMaterialArray(array);
			array = new Material[count];
		}
		for (int i = 0; i < count; i++)
		{
			if (array[i] == null)
			{
				array[i] = UnityEngine.Object.Instantiate(material);
			}
		}
	}

	public void SetTarget(Transform transform, bool only_set = false)
	{
		if (!(transform == target) && !(transform == null))
		{
			if (!only_set && _highlighted)
			{
				ImmediateFadeOut();
			}
			target = transform;
			if (!only_set)
			{
				SetupMaterial();
			}
		}
	}

	public void SetTargets(Transform transform, Renderer[] renderers)
	{
		if (!(transform == null))
		{
			if (_highlighted)
			{
				ImmediateFadeOut();
			}
			effectGroup = TargetOptions.Scripting;
			target = transform;
			SetupMaterial(renderers);
		}
	}

	public void SetHighlighted(bool state)
	{
		if (!Application.isPlaying)
		{
			_highlighted = state;
			return;
		}
		float time = Time.time;
		if (fading == FadingState.NoFading)
		{
			fadeStartTime = time;
		}
		if (state && !ignore)
		{
			if ((_highlighted && fading == FadingState.NoFading) || (this.OnObjectHighlightStart != null && !this.OnObjectHighlightStart(base.gameObject)))
			{
				return;
			}
			SendMessage("HighlightStart", null, SendMessageOptions.DontRequireReceiver);
			highlightStartTime = (targetFxStartTime = time);
			if (fadeInDuration > 0f)
			{
				if (fading == FadingState.FadingOut)
				{
					float num = fadeOutDuration - (time - fadeStartTime);
					fadeStartTime = time - num;
					fadeStartTime = Mathf.Min(fadeStartTime, time);
				}
				fading = FadingState.FadingIn;
			}
			else
			{
				fading = FadingState.NoFading;
			}
			_highlighted = true;
			requireUpdateMaterial = true;
		}
		else
		{
			if (!_highlighted)
			{
				return;
			}
			if (fadeOutDuration > 0f)
			{
				if (fading == FadingState.FadingIn)
				{
					float num2 = time - fadeStartTime;
					fadeStartTime = time + num2 - fadeInDuration;
					fadeStartTime = Mathf.Min(fadeStartTime, time);
				}
				fading = FadingState.FadingOut;
			}
			else
			{
				fading = FadingState.NoFading;
				ImmediateFadeOut();
				requireUpdateMaterial = true;
			}
		}
	}

	private void ImmediateFadeOut()
	{
		fading = FadingState.NoFading;
		_highlighted = false;
		if (this.OnObjectHighlightEnd != null)
		{
			this.OnObjectHighlightEnd(base.gameObject);
		}
		SendMessage("HighlightEnd", null, SendMessageOptions.DontRequireReceiver);
	}

	private void SetupMaterial()
	{
		if (target == null || fxMatMask == null)
		{
			return;
		}
		Renderer[] rr = null;
		if (overrideRenderers != null)
		{
			rr = overrideRenderers.GetComponentsInChildren<Renderer>();
		}
		else
		{
			switch (effectGroup)
			{
			case TargetOptions.OnlyThisObject:
			{
				Renderer component2 = target.GetComponent<Renderer>();
				if (component2 != null)
				{
					rr = new Renderer[1] { component2 };
				}
				break;
			}
			case TargetOptions.RootToChildren:
			{
				Transform parent = target;
				while (parent.parent != null)
				{
					parent = parent.parent;
				}
				rr = parent.GetComponentsInChildren<Renderer>();
				break;
			}
			case TargetOptions.LayerInScene:
			{
				HighlightEffect highlightEffect2 = this;
				if (target != base.transform)
				{
					HighlightEffect component3 = target.GetComponent<HighlightEffect>();
					if (component3 != null)
					{
						highlightEffect2 = component3;
					}
				}
				rr = FindRenderersWithLayerInScene(highlightEffect2.effectGroupLayer);
				break;
			}
			case TargetOptions.LayerInChildren:
			{
				HighlightEffect highlightEffect = this;
				if (target != base.transform)
				{
					HighlightEffect component = target.GetComponent<HighlightEffect>();
					if (component != null)
					{
						highlightEffect = component;
					}
				}
				rr = FindRenderersWithLayerInChildren(highlightEffect.effectGroupLayer);
				break;
			}
			case TargetOptions.Children:
				rr = target.GetComponentsInChildren<Renderer>();
				break;
			case TargetOptions.Scripting:
				_ = rmsCount;
				_ = 0;
				return;
			}
		}
		SetupMaterial(rr);
	}

	private void SetupMaterial(Renderer[] rr)
	{
		if (rr == null)
		{
			rr = new Renderer[0];
		}
		if (rms == null || rms.Length < rr.Length)
		{
			rms = new ModelMaterials[rr.Length];
		}
		rmsCount = 0;
		for (int i = 0; i < rr.Length; i++)
		{
			rms[rmsCount].Init();
			Renderer renderer = rr[i];
			if (effectGroup != TargetOptions.OnlyThisObject && !string.IsNullOrEmpty(effectNameFilter) && !renderer.name.Contains(effectNameFilter))
			{
				continue;
			}
			rms[rmsCount].renderer = renderer;
			rms[rmsCount].renderWasVisibleDuringSetup = renderer.isVisible;
			if (renderer.transform != target)
			{
				HighlightEffect component = renderer.GetComponent<HighlightEffect>();
				if (component != null && component.enabled)
				{
					continue;
				}
			}
			if (this.OnRendererHighlightStart != null && !this.OnRendererHighlightStart(renderer))
			{
				rmsCount++;
				continue;
			}
			rms[rmsCount].isCombined = false;
			bool flag = renderer is SkinnedMeshRenderer;
			rms[rmsCount].isSkinnedMesh = flag;
			rms[rmsCount].normalsOption = (flag ? NormalsOption.PreserveOriginal : normalsOption);
			CheckCommandBuffers();
			if (flag)
			{
				rms[rmsCount].isSkinnedMesh = true;
				rms[rmsCount].mesh = ((SkinnedMeshRenderer)renderer).sharedMesh;
			}
			else if (Application.isPlaying && renderer.isPartOfStaticBatch)
			{
				MeshCollider component2 = renderer.GetComponent<MeshCollider>();
				if (component2 != null)
				{
					rms[rmsCount].mesh = component2.sharedMesh;
				}
			}
			else
			{
				MeshFilter component3 = renderer.GetComponent<MeshFilter>();
				if (component3 != null)
				{
					rms[rmsCount].mesh = component3.sharedMesh;
				}
			}
			if (rms[rmsCount].mesh == null)
			{
				continue;
			}
			rms[rmsCount].transform = renderer.transform;
			Fork(fxMatMask, ref rms[rmsCount].fxMatMask, rms[rmsCount].mesh);
			Fork(fxMatOutlineTemplate, ref rms[rmsCount].fxMatOutline, rms[rmsCount].mesh);
			Fork(fxMatGlowTemplate, ref rms[rmsCount].fxMatGlow, rms[rmsCount].mesh);
			Fork(fxMatSeeThrough, ref rms[rmsCount].fxMatSeeThroughInner, rms[rmsCount].mesh);
			Fork(fxMatSeeThroughBorder, ref rms[rmsCount].fxMatSeeThroughBorder, rms[rmsCount].mesh);
			Fork(fxMatOverlay, ref rms[rmsCount].fxMatOverlay, rms[rmsCount].mesh);
			Fork(fxMatInnerGlow, ref rms[rmsCount].fxMatInnerGlow, rms[rmsCount].mesh);
			Fork(fxMatSolidColor, ref rms[rmsCount].fxMatSolidColor, rms[rmsCount].mesh);
			rms[rmsCount].originalMesh = rms[rmsCount].mesh;
			if (!rms[rmsCount].preserveOriginalMesh && (innerGlow > 0f || (glow > 0f && glowQuality != QualityLevel.Highest) || (outline > 0f && outlineQuality != QualityLevel.Highest)))
			{
				if (normalsOption == NormalsOption.Reorient)
				{
					ReorientNormals(rmsCount);
				}
				else
				{
					AverageNormals(rmsCount);
				}
			}
			rmsCount++;
		}
		if (combineMeshes)
		{
			CombineMeshes();
		}
		UpdateMaterialProperties();
	}

	private Renderer[] FindRenderersWithLayerInScene(LayerMask layer)
	{
		Renderer[] array = UnityEngine.Object.FindObjectsOfType<Renderer>();
		tempRR.Clear();
		foreach (Renderer renderer in array)
		{
			if (((1 << renderer.gameObject.layer) & (int)layer) != 0)
			{
				tempRR.Add(renderer);
			}
		}
		return tempRR.ToArray();
	}

	private Renderer[] FindRenderersWithLayerInChildren(LayerMask layer)
	{
		Renderer[] componentsInChildren = target.GetComponentsInChildren<Renderer>();
		tempRR.Clear();
		foreach (Renderer renderer in componentsInChildren)
		{
			if (((1 << renderer.gameObject.layer) & (int)layer) != 0)
			{
				tempRR.Add(renderer);
			}
		}
		return tempRR.ToArray();
	}

	private void CheckGeometrySupportDependencies()
	{
		InitMaterial(ref fxMatMask, "HighlightPlus/Geometry/Mask");
		InitMaterial(ref fxMatGlowTemplate, "HighlightPlus/Geometry/Glow");
		if (fxMatGlowTemplate != null && useGPUInstancing)
		{
			fxMatGlowTemplate.enableInstancing = true;
		}
		InitMaterial(ref fxMatInnerGlow, "HighlightPlus/Geometry/InnerGlow");
		InitMaterial(ref fxMatOutlineTemplate, "HighlightPlus/Geometry/Outline");
		if (fxMatOutlineTemplate != null && useGPUInstancing)
		{
			fxMatOutlineTemplate.enableInstancing = true;
		}
		InitMaterial(ref fxMatOverlay, "HighlightPlus/Geometry/Overlay");
		InitMaterial(ref fxMatSeeThrough, "HighlightPlus/Geometry/SeeThrough");
		InitMaterial(ref fxMatSeeThroughBorder, "HighlightPlus/Geometry/SeeThroughBorder");
		InitMaterial(ref fxMatSeeThroughMask, "HighlightPlus/Geometry/SeeThroughMask");
		InitMaterial(ref fxMatTarget, "HighlightPlus/Geometry/Target");
		InitMaterial(ref fxMatComposeGlow, "HighlightPlus/Geometry/ComposeGlow");
		InitMaterial(ref fxMatComposeOutline, "HighlightPlus/Geometry/ComposeOutline");
		InitMaterial(ref fxMatSolidColor, "HighlightPlus/Geometry/SolidColor");
		InitMaterial(ref fxMatBlurGlow, "HighlightPlus/Geometry/BlurGlow");
		InitMaterial(ref fxMatBlurOutline, "HighlightPlus/Geometry/BlurOutline");
		InitMaterial(ref fxMatClearStencil, "HighlightPlus/ClearStencil");
	}

	private void CheckCommandBuffers()
	{
		if (cbHighlight == null)
		{
			cbHighlight = new CommandBuffer();
			cbHighlight.name = "Highlight Plus for " + base.name;
		}
	}

	public void UpdateMaterialProperties()
	{
		if (rms == null)
		{
			return;
		}
		if (ignore)
		{
			_highlighted = false;
		}
		maskRequired = (_highlighted && (outline > 0f || (glow > 0f && !glowIgnoreMask))) || seeThrough != SeeThroughMode.Never || (targetFX && targetFXAlignToGround);
		Color value = seeThroughTintColor;
		value.a = seeThroughTintAlpha;
		if (lastOutlineVisibility != outlineVisibility)
		{
			if (glowQuality == QualityLevel.Highest && outlineQuality == QualityLevel.Highest)
			{
				glowVisibility = outlineVisibility;
			}
			lastOutlineVisibility = outlineVisibility;
		}
		if (outlineWidth < 0f)
		{
			outlineWidth = 0f;
		}
		if (outlineQuality == QualityLevel.Medium)
		{
			outlineOffsetsMin = 4;
			outlineOffsetsMax = 7;
		}
		else if (outlineQuality == QualityLevel.High)
		{
			outlineOffsetsMin = 0;
			outlineOffsetsMax = 7;
		}
		else
		{
			outlineOffsetsMin = (outlineOffsetsMax = 0);
		}
		if (glowWidth < 0f)
		{
			glowWidth = 0f;
		}
		if (glowQuality == QualityLevel.Medium)
		{
			glowOffsetsMin = 4;
			glowOffsetsMax = 7;
		}
		else if (glowQuality == QualityLevel.High)
		{
			glowOffsetsMin = 0;
			glowOffsetsMax = 7;
		}
		else
		{
			glowOffsetsMin = (glowOffsetsMax = 0);
		}
		if (targetFXTransitionDuration <= 0f)
		{
			targetFXTransitionDuration = 0.0001f;
		}
		if (targetFXStayDuration <= 0f)
		{
			targetFXStayDuration = 0f;
		}
		if (targetFXFadePower <= 0f)
		{
			targetFXFadePower = 0f;
		}
		if (seeThroughDepthOffset < 0f)
		{
			seeThroughDepthOffset = 0f;
		}
		if (seeThroughMaxDepth < 0f)
		{
			seeThroughMaxDepth = 0f;
		}
		if (seeThroughBorderWidth < 0f)
		{
			seeThroughBorderWidth = 0f;
		}
		useSmoothGlow = glow > 0f && glowQuality == QualityLevel.Highest;
		useSmoothOutline = outline > 0f && outlineQuality == QualityLevel.Highest;
		useSmoothBlend = useSmoothGlow || useSmoothOutline;
		if (useSmoothBlend && useSmoothGlow && useSmoothOutline)
		{
			outlineVisibility = glowVisibility;
		}
		if (useSmoothGlow)
		{
			fxMatComposeGlow.SetInt(ShaderParams.Cull, cullBackFaces ? 2 : 0);
			if (glowBlendMode == GlowBlendMode.Additive)
			{
				fxMatComposeGlow.SetInt(ShaderParams.BlendSrc, 1);
				fxMatComposeGlow.SetInt(ShaderParams.BlendDst, 1);
			}
			else
			{
				fxMatComposeGlow.SetInt(ShaderParams.BlendSrc, 5);
				fxMatComposeGlow.SetInt(ShaderParams.BlendDst, 10);
			}
			fxMatBlurGlow.SetFloat(ShaderParams.BlurScale, glowWidth / (float)glowDownsampling);
			fxMatBlurGlow.SetFloat(ShaderParams.Speed, glowAnimationSpeed);
		}
		if (useSmoothOutline)
		{
			fxMatComposeOutline.SetInt(ShaderParams.Cull, cullBackFaces ? 2 : 0);
			fxMatBlurOutline.SetFloat(ShaderParams.BlurScale, outlineWidth / (float)outlineDownsampling);
		}
		if (targetFX)
		{
			if (targetFXTexture == null)
			{
				targetFXTexture = Resources.Load<Texture2D>("HighlightPlus/target");
			}
			fxMatTarget.mainTexture = targetFXTexture;
			fxMatTarget.SetInt(ShaderParams.ZTest, GetZTestValue(targetFXVisibility));
		}
		float value2 = (outlineQuality.UsesMultipleOffsets() ? 0f : (outlineWidth / 100f));
		for (int i = 0; i < rmsCount; i++)
		{
			if (!(rms[i].mesh != null))
			{
				continue;
			}
			Renderer renderer = rms[i].renderer;
			if (renderer == null)
			{
				continue;
			}
			renderer.GetSharedMaterials(rendererSharedMaterials);
			for (int j = 0; j < rms[i].mesh.subMeshCount; j++)
			{
				if (((1 << j) & subMeshMask) == 0)
				{
					continue;
				}
				Material material = null;
				if (j < rendererSharedMaterials.Count)
				{
					material = rendererSharedMaterials[j];
				}
				if (material == null)
				{
					continue;
				}
				bool flag = false;
				Texture mainTexture = null;
				Vector2 mainTextureOffset = Vector2.zero;
				Vector2 mainTextureScale = Vector2.one;
				if (material.HasProperty(ShaderParams.MainTex))
				{
					mainTexture = material.GetTexture(ShaderParams.MainTex);
					mainTextureOffset = material.mainTextureOffset;
					mainTextureScale = material.mainTextureScale;
					flag = true;
				}
				else if (material.HasProperty(ShaderParams.BaseMap))
				{
					mainTexture = material.GetTexture(ShaderParams.BaseMap);
					flag = true;
					if (material.HasProperty(ShaderParams.BaseMapST))
					{
						Vector4 vector = material.GetVector(ShaderParams.BaseMapST);
						mainTextureScale.x = vector.x;
						mainTextureScale.y = vector.y;
						mainTextureOffset.x = vector.z;
						mainTextureOffset.y = vector.w;
					}
				}
				bool flag2 = alphaCutOff > 0f && flag;
				if (rms[i].fxMatMask != null && rms[i].fxMatMask.Length > j)
				{
					Material material2 = rms[i].fxMatMask[j];
					if (material2 != null)
					{
						material2.mainTexture = mainTexture;
						material2.mainTextureOffset = mainTextureOffset;
						material2.mainTextureScale = mainTextureScale;
						if (flag2)
						{
							material2.SetFloat(ShaderParams.CutOff, alphaCutOff);
							material2.EnableKeyword("HP_ALPHACLIP");
						}
						else
						{
							material2.DisableKeyword("HP_ALPHACLIP");
						}
						material2.SetInt(ShaderParams.Cull, cullBackFaces ? 2 : 0);
					}
				}
				if (rms[i].fxMatOutline != null && rms[i].fxMatOutline.Length > j)
				{
					Material material3 = rms[i].fxMatOutline[j];
					material3.SetFloat(ShaderParams.OutlineWidth, value2);
					material3.SetInt(ShaderParams.OutlineZTest, GetZTestValue(outlineVisibility));
					material3.SetInt(ShaderParams.Cull, cullBackFaces ? 2 : 0);
					material3.SetFloat(ShaderParams.ConstantWidth, constantWidth ? 1f : 0f);
					if (flag2)
					{
						material3.mainTexture = mainTexture;
						material3.mainTextureOffset = mainTextureOffset;
						material3.mainTextureScale = mainTextureScale;
						material3.SetFloat(ShaderParams.CutOff, alphaCutOff);
						material3.EnableKeyword("HP_ALPHACLIP");
					}
					else
					{
						material3.DisableKeyword("HP_ALPHACLIP");
					}
				}
				if (rms[i].fxMatGlow != null && rms[i].fxMatGlow.Length > j)
				{
					Material material4 = rms[i].fxMatGlow[j];
					material4.SetVector(ShaderParams.Glow2, new Vector4((outline > 0f) ? (outlineWidth / 100f) : 0f, glowAnimationSpeed, (!glowDithering) ? 1 : 0));
					material4.SetInt(ShaderParams.GlowZTest, GetZTestValue(glowVisibility));
					material4.SetInt(ShaderParams.Cull, cullBackFaces ? 2 : 0);
					material4.SetFloat(ShaderParams.ConstantWidth, constantWidth ? 1f : 0f);
					material4.SetInt(ShaderParams.GlowStencilOp, (!glowBlendPasses) ? 2 : 0);
					material4.SetInt(ShaderParams.GlowStencilComp, glowIgnoreMask ? 8 : 6);
					if (flag2)
					{
						material4.mainTexture = mainTexture;
						material4.mainTextureOffset = mainTextureOffset;
						material4.mainTextureScale = mainTextureScale;
						material4.SetFloat(ShaderParams.CutOff, alphaCutOff);
						material4.EnableKeyword("HP_ALPHACLIP");
					}
					else
					{
						material4.DisableKeyword("HP_ALPHACLIP");
					}
				}
				bool num = rms[i].fxMatSeeThroughBorder != null && rms[i].fxMatSeeThroughBorder.Length > j && seeThroughBorder * seeThroughBorderWidth > 0f;
				if (rms[i].fxMatSeeThroughInner != null && rms[i].fxMatSeeThroughInner.Length > j)
				{
					Material material5 = rms[i].fxMatSeeThroughInner[j];
					if (material5 != null)
					{
						material5.SetFloat(ShaderParams.SeeThrough, seeThroughIntensity);
						material5.SetFloat(ShaderParams.SeeThroughNoise, seeThroughNoise);
						material5.SetColor(ShaderParams.SeeThroughTintColor, value);
						if (seeThroughOccluderMaskAccurate && (int)seeThroughOccluderMask != -1)
						{
							material5.SetInt(ShaderParams.SeeThroughStencilRef, 1);
							material5.SetInt(ShaderParams.SeeThroughStencilComp, 3);
							material5.SetInt(ShaderParams.SeeThroughStencilPassOp, 1);
						}
						else
						{
							material5.SetInt(ShaderParams.SeeThroughStencilRef, 2);
							material5.SetInt(ShaderParams.SeeThroughStencilComp, 5);
							material5.SetInt(ShaderParams.SeeThroughStencilPassOp, 2);
						}
						material5.mainTexture = mainTexture;
						material5.mainTextureOffset = mainTextureOffset;
						material5.mainTextureScale = mainTextureScale;
						if (flag2)
						{
							material5.SetFloat(ShaderParams.CutOff, alphaCutOff);
							material5.EnableKeyword("HP_ALPHACLIP");
						}
						else
						{
							material5.DisableKeyword("HP_ALPHACLIP");
						}
						if (seeThroughDepthOffset > 0f || seeThroughMaxDepth > 0f)
						{
							material5.SetFloat(ShaderParams.SeeThroughDepthOffset, (seeThroughDepthOffset > 0f) ? seeThroughDepthOffset : (-1f));
							material5.SetFloat(ShaderParams.SeeThroughMaxDepth, (seeThroughMaxDepth > 0f) ? seeThroughMaxDepth : 999999f);
							material5.EnableKeyword("HP_DEPTH_OFFSET");
						}
						else
						{
							material5.DisableKeyword("HP_DEPTH_OFFSET");
						}
						if (seeThroughBorderOnly)
						{
							material5.EnableKeyword("HP_SEETHROUGH_ONLY_BORDER");
						}
						else
						{
							material5.DisableKeyword("HP_SEETHROUGH_ONLY_BORDER");
						}
					}
				}
				if (num)
				{
					Material material6 = rms[i].fxMatSeeThroughBorder[j];
					if (material6 != null)
					{
						material6.SetColor(ShaderParams.SeeThroughBorderColor, new Color(seeThroughBorderColor.r, seeThroughBorderColor.g, seeThroughBorderColor.b, seeThroughBorder));
						material6.SetFloat(ShaderParams.SeeThroughBorderWidth, (seeThroughBorder * seeThroughBorderWidth > 0f) ? (seeThroughBorderWidth / 100f) : 0f);
						material6.SetFloat(ShaderParams.SeeThroughBorderConstantWidth, constantWidth ? 1f : 0f);
						if (seeThroughOccluderMaskAccurate && (int)seeThroughOccluderMask != -1)
						{
							material6.SetInt(ShaderParams.SeeThroughStencilRef, 1);
							material6.SetInt(ShaderParams.SeeThroughStencilComp, 3);
							material6.SetInt(ShaderParams.SeeThroughStencilPassOp, 1);
						}
						else
						{
							material6.SetInt(ShaderParams.SeeThroughStencilRef, 2);
							material6.SetInt(ShaderParams.SeeThroughStencilComp, 5);
							material6.SetInt(ShaderParams.SeeThroughStencilPassOp, 0);
						}
						material6.mainTexture = mainTexture;
						material6.mainTextureOffset = mainTextureOffset;
						material6.mainTextureScale = mainTextureScale;
						if (flag2)
						{
							material6.SetFloat(ShaderParams.CutOff, alphaCutOff);
							material6.EnableKeyword("HP_ALPHACLIP");
						}
						else
						{
							material6.DisableKeyword("HP_ALPHACLIP");
						}
						if (seeThroughDepthOffset > 0f || seeThroughMaxDepth > 0f)
						{
							material6.SetFloat(ShaderParams.SeeThroughDepthOffset, (seeThroughDepthOffset > 0f) ? seeThroughDepthOffset : (-1f));
							material6.SetFloat(ShaderParams.SeeThroughMaxDepth, (seeThroughMaxDepth > 0f) ? seeThroughMaxDepth : 999999f);
							material6.EnableKeyword("HP_DEPTH_OFFSET");
						}
						else
						{
							material6.DisableKeyword("HP_DEPTH_OFFSET");
						}
					}
				}
				if (rms[i].fxMatOverlay != null && rms[i].fxMatOverlay.Length > j)
				{
					Material material7 = rms[i].fxMatOverlay[j];
					if (material7 != null)
					{
						material7.mainTexture = mainTexture;
						material7.mainTextureOffset = mainTextureOffset;
						material7.mainTextureScale = mainTextureScale;
						if (material.HasProperty(ShaderParams.Color))
						{
							material7.SetColor(ShaderParams.OverlayBackColor, material.GetColor(ShaderParams.Color));
						}
						material7.SetInt(ShaderParams.Cull, cullBackFaces ? 2 : 0);
						if (overlayTexture != null)
						{
							material7.SetTexture(ShaderParams.OverlayTexture, overlayTexture);
							material7.EnableKeyword("HP_USES_OVERLAY_TEXTURE");
						}
						else
						{
							material7.DisableKeyword("HP_USES_OVERLAY_TEXTURE");
						}
						if (flag2)
						{
							material7.SetFloat(ShaderParams.CutOff, alphaCutOff);
							material7.EnableKeyword("HP_ALPHACLIP");
						}
						else
						{
							material7.DisableKeyword("HP_ALPHACLIP");
						}
					}
				}
				if (rms[i].fxMatInnerGlow != null && rms[i].fxMatInnerGlow.Length > j)
				{
					Material material8 = rms[i].fxMatInnerGlow[j];
					if (material8 != null)
					{
						material8.mainTexture = mainTexture;
						material8.mainTextureOffset = mainTextureOffset;
						material8.mainTextureScale = mainTextureScale;
						material8.SetFloat(ShaderParams.InnerGlowWidth, innerGlowWidth);
						material8.SetInt(ShaderParams.InnerGlowZTest, GetZTestValue(innerGlowVisibility));
						material8.SetInt(ShaderParams.Cull, cullBackFaces ? 2 : 0);
						if (flag2)
						{
							material8.SetFloat(ShaderParams.CutOff, alphaCutOff);
							material8.EnableKeyword("HP_ALPHACLIP");
						}
						else
						{
							material8.DisableKeyword("HP_ALPHACLIP");
						}
					}
				}
				if (rms[i].fxMatSolidColor == null || rms[i].fxMatSolidColor.Length <= j)
				{
					continue;
				}
				Material material9 = rms[i].fxMatSolidColor[j];
				if (material9 != null)
				{
					material9.color = glowHQColor;
					material9.SetInt(ShaderParams.Cull, cullBackFaces ? 2 : 0);
					material9.mainTexture = mainTexture;
					material9.mainTextureOffset = mainTextureOffset;
					material9.mainTextureScale = mainTextureScale;
					if ((glow > 0f && glowQuality == QualityLevel.Highest && glowVisibility == Visibility.Normal) || (outline > 0f && outlineQuality == QualityLevel.Highest && outlineVisibility == Visibility.Normal))
					{
						material9.EnableKeyword("HP_DEPTHCLIP");
					}
					else
					{
						material9.DisableKeyword("HP_DEPTHCLIP");
					}
					if (flag2)
					{
						material9.SetFloat(ShaderParams.CutOff, alphaCutOff);
						material9.EnableKeyword("HP_ALPHACLIP");
					}
					else
					{
						material9.DisableKeyword("HP_ALPHACLIP");
					}
				}
			}
		}
	}

	private float ComputeCameraDistanceFade(Vector3 position, Transform cameraTransform)
	{
		float num = Vector3.Dot(position - cameraTransform.position, cameraTransform.forward);
		if (num < cameraDistanceFadeNear)
		{
			return 1f - Mathf.Min(1f, cameraDistanceFadeNear - num);
		}
		if (num > cameraDistanceFadeFar)
		{
			return 1f - Mathf.Min(1f, num - cameraDistanceFadeFar);
		}
		return 1f;
	}

	private int GetZTestValue(Visibility param)
	{
		return param switch
		{
			Visibility.AlwaysOnTop => 8, 
			Visibility.OnlyWhenOccluded => 5, 
			_ => 4, 
		};
	}

	private void BuildQuad()
	{
		quadMesh = new Mesh();
		Vector3[] array = new Vector3[4];
		float num = 0.5f;
		float num2 = 0.5f;
		array[0] = new Vector3(0f - num2, 0f - num, 0f);
		array[1] = new Vector3(0f - num2, num, 0f);
		array[2] = new Vector3(num2, 0f - num, 0f);
		array[3] = new Vector3(num2, num, 0f);
		Vector2[] array2 = new Vector2[array.Length];
		array2[0] = new Vector2(0f, 0f);
		array2[1] = new Vector2(0f, 1f);
		array2[2] = new Vector2(1f, 0f);
		array2[3] = new Vector2(1f, 1f);
		int[] triangles = new int[6] { 0, 1, 2, 3, 2, 1 };
		Vector3[] array3 = new Vector3[array.Length];
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i] = Vector3.forward;
		}
		quadMesh.vertices = array;
		quadMesh.uv = array2;
		quadMesh.triangles = triangles;
		quadMesh.normals = array3;
		quadMesh.RecalculateBounds();
	}

	private void BuildCube()
	{
		cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
	}

	public bool Includes(Transform transform)
	{
		for (int i = 0; i < rmsCount; i++)
		{
			if (rms[i].transform == transform)
			{
				return true;
			}
		}
		return false;
	}

	public void SetGlowColor(Color color)
	{
		if (glowPasses != null)
		{
			for (int i = 0; i < glowPasses.Length; i++)
			{
				glowPasses[i].color = color;
			}
		}
		glowHQColor = color;
		UpdateMaterialProperties();
	}

	private void AverageNormals(int objIndex)
	{
		if (rms == null || objIndex >= rms.Length)
		{
			return;
		}
		Mesh mesh = rms[objIndex].mesh;
		int hashCode = mesh.GetHashCode();
		if (!smoothMeshes.TryGetValue(hashCode, out var value) || value == null)
		{
			if (!mesh.isReadable)
			{
				return;
			}
			if (normals == null)
			{
				normals = new List<Vector3>();
			}
			else
			{
				normals.Clear();
			}
			mesh.GetNormals(normals);
			int count = normals.Count;
			if (count == 0)
			{
				return;
			}
			if (vertices == null)
			{
				vertices = new List<Vector3>();
			}
			else
			{
				vertices.Clear();
			}
			mesh.GetVertices(vertices);
			int num = vertices.Count;
			if (count < num)
			{
				num = count;
			}
			if (newNormals == null || newNormals.Length < num)
			{
				newNormals = new Vector3[num];
			}
			else
			{
				Vector3 zero = Vector3.zero;
				for (int i = 0; i < num; i++)
				{
					newNormals[i] = zero;
				}
			}
			if (matches == null || matches.Length < num)
			{
				matches = new int[num];
			}
			vv.Clear();
			for (int j = 0; j < num; j++)
			{
				Vector3 key = vertices[j];
				if (!vv.TryGetValue(key, out var value2))
				{
					value2 = (vv[key] = j);
				}
				matches[j] = value2;
			}
			for (int k = 0; k < num; k++)
			{
				int num3 = matches[k];
				newNormals[num3] += normals[k];
			}
			for (int l = 0; l < num; l++)
			{
				int num4 = matches[l];
				normals[l] = newNormals[num4].normalized;
			}
			value = UnityEngine.Object.Instantiate(mesh);
			value.SetNormals(normals);
			smoothMeshes[hashCode] = value;
		}
		rms[objIndex].mesh = value;
	}

	private void ReorientNormals(int objIndex)
	{
		if (rms == null || objIndex >= rms.Length)
		{
			return;
		}
		Mesh mesh = rms[objIndex].mesh;
		int hashCode = mesh.GetHashCode();
		if (!reorientedMeshes.TryGetValue(hashCode, out var value) || value == null)
		{
			if (!mesh.isReadable)
			{
				return;
			}
			if (normals == null)
			{
				normals = new List<Vector3>();
			}
			else
			{
				normals.Clear();
			}
			if (vertices == null)
			{
				vertices = new List<Vector3>();
			}
			else
			{
				vertices.Clear();
			}
			mesh.GetVertices(vertices);
			int count = vertices.Count;
			if (count == 0)
			{
				return;
			}
			Vector3 zero = Vector3.zero;
			for (int i = 0; i < count; i++)
			{
				zero += vertices[i];
			}
			zero /= (float)count;
			for (int j = 0; j < count; j++)
			{
				normals.Add((vertices[j] - zero).normalized);
			}
			value = UnityEngine.Object.Instantiate(mesh);
			value.SetNormals(normals);
			reorientedMeshes[hashCode] = value;
		}
		rms[objIndex].mesh = value;
	}

	private void CombineMeshes()
	{
		if (rmsCount <= 1)
		{
			return;
		}
		if (combineInstances == null || combineInstances.Length != rmsCount)
		{
			combineInstances = new CombineInstance[rmsCount];
		}
		int num = -1;
		int num2 = 0;
		combinedMeshesHashId = 0;
		int num3 = 0;
		Matrix4x4 worldToLocalMatrix = matrix4x4Identity;
		for (int i = 0; i < rmsCount; i++)
		{
			combineInstances[i].mesh = null;
			if (rms[i].isSkinnedMesh)
			{
				continue;
			}
			Mesh mesh = rms[i].mesh;
			if (mesh != null && mesh.isReadable && num3 + mesh.vertexCount <= 65535)
			{
				num3 += mesh.vertexCount;
				combineInstances[i].mesh = mesh;
				int instanceID = rms[i].renderer.gameObject.GetInstanceID();
				if (num < 0)
				{
					num = i;
					combinedMeshesHashId = instanceID;
					worldToLocalMatrix = rms[i].transform.worldToLocalMatrix;
				}
				else
				{
					combinedMeshesHashId ^= instanceID;
					rms[i].mesh = null;
				}
				combineInstances[i].transform = worldToLocalMatrix * rms[i].transform.localToWorldMatrix;
				num2++;
			}
		}
		if (num2 >= 2)
		{
			if (!combinedMeshes.TryGetValue(combinedMeshesHashId, out var value) || value == null)
			{
				value = new Mesh();
				value.CombineMeshes(combineInstances, mergeSubMeshes: true, useMatrices: true);
				combinedMeshes[combinedMeshesHashId] = value;
			}
			rms[num].mesh = value;
			rms[num].isCombined = true;
		}
	}

	public void HitFX()
	{
		HitFX(hitFxColor, hitFxFadeOutDuration, hitFxInitialIntensity);
	}

	public void HitFX(Vector3 position)
	{
		HitFX(hitFxColor, hitFxFadeOutDuration, hitFxInitialIntensity, position, hitFxRadius);
	}

	public void HitFX(Color color, float fadeOutDuration, float initialIntensity = 1f)
	{
		hitInitialIntensity = initialIntensity;
		hitFadeOutDuration = fadeOutDuration;
		hitColor = color;
		hitStartTime = Time.time;
		hitActive = true;
		if (overlay == 0f)
		{
			UpdateMaterialProperties();
		}
	}

	public void HitFX(Color color, float fadeOutDuration, float initialIntensity, Vector3 position, float radius)
	{
		hitInitialIntensity = initialIntensity;
		hitFadeOutDuration = fadeOutDuration;
		hitColor = color;
		hitStartTime = Time.time;
		hitActive = true;
		hitPosition = position;
		hitRadius = radius;
		if (overlay == 0f)
		{
			UpdateMaterialProperties();
		}
	}

	public void TargetFX()
	{
		targetFxStartTime = Time.time;
		if (!targetFX)
		{
			targetFX = true;
			UpdateMaterialProperties();
		}
	}

	public bool IsSeeThroughOccluded(Camera cam)
	{
		Bounds bounds = default(Bounds);
		for (int i = 0; i < rms.Length; i++)
		{
			if (rms[i].renderer != null)
			{
				if (bounds.size.x == 0f)
				{
					bounds = rms[i].renderer.bounds;
				}
				else
				{
					bounds.Encapsulate(rms[i].renderer.bounds);
				}
			}
		}
		Vector3 center = bounds.center;
		Vector3 position = cam.transform.position;
		Vector3 vector = center - position;
		float maxDistance = Vector3.Distance(center, position);
		if (hits == null || hits.Length == 0)
		{
			hits = new RaycastHit[64];
		}
		int count = occluders.Count;
		int num = Physics.BoxCastNonAlloc(center - vector, bounds.extents * 0.9f, vector.normalized, hits, Quaternion.identity, maxDistance);
		for (int j = 0; j < num; j++)
		{
			for (int k = 0; k < count; k++)
			{
				if (hits[j].collider.transform == occluders[k].transform)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void RegisterOccluder(HighlightSeeThroughOccluder occluder)
	{
		if (!occluders.Contains(occluder))
		{
			occluders.Add(occluder);
		}
	}

	public static void UnregisterOccluder(HighlightSeeThroughOccluder occluder)
	{
		if (occluders.Contains(occluder))
		{
			occluders.Remove(occluder);
		}
	}

	public bool RenderSeeThroughOccluders(CommandBuffer cb, Camera cam)
	{
		int count = occluders.Count;
		if (count == 0 || rmsCount == 0)
		{
			return true;
		}
		bool flag = false;
		for (int i = 0; i < count; i++)
		{
			HighlightSeeThroughOccluder highlightSeeThroughOccluder = occluders[i];
			if (!(highlightSeeThroughOccluder == null) && highlightSeeThroughOccluder.isActiveAndEnabled && highlightSeeThroughOccluder.detectionMethod == DetectionMethod.RayCast)
			{
				flag = true;
				break;
			}
		}
		if (flag && IsSeeThroughOccluded(cam))
		{
			return false;
		}
		occludersFrameCount.TryGetValue(cam, out var value);
		int frameCount = Time.frameCount;
		if (frameCount == value)
		{
			return true;
		}
		occludersFrameCount[cam] = frameCount;
		if (fxMatOccluder == null)
		{
			InitMaterial(ref fxMatOccluder, "HighlightPlus/Geometry/SeeThroughOccluder");
			if (fxMatOccluder == null)
			{
				return true;
			}
		}
		for (int j = 0; j < count; j++)
		{
			HighlightSeeThroughOccluder highlightSeeThroughOccluder2 = occluders[j];
			if (highlightSeeThroughOccluder2 == null || !highlightSeeThroughOccluder2.isActiveAndEnabled || highlightSeeThroughOccluder2.detectionMethod != DetectionMethod.Stencil || highlightSeeThroughOccluder2.meshData == null || highlightSeeThroughOccluder2.meshData.Length == 0)
			{
				continue;
			}
			for (int k = 0; k < highlightSeeThroughOccluder2.meshData.Length; k++)
			{
				Renderer renderer = highlightSeeThroughOccluder2.meshData[k].renderer;
				if (renderer.isVisible)
				{
					for (int l = 0; l < highlightSeeThroughOccluder2.meshData[k].subMeshCount; l++)
					{
						cb.DrawRenderer(renderer, fxMatOccluder, l);
					}
				}
			}
		}
		return true;
	}

	private bool CheckOcclusion(Camera cam)
	{
		float time = Time.time;
		int frameCount = Time.frameCount;
		if (Time.time - occlusionCheckLastTime < seeThroughOccluderCheckInterval && Application.isPlaying && occlusionRenderFrame != frameCount)
		{
			return lastOcclusionTestResult;
		}
		occlusionCheckLastTime = time;
		occlusionRenderFrame = frameCount;
		if (rms.Length == 0 || rms[0].renderer == null)
		{
			return false;
		}
		Vector3 position = cam.transform.position;
		Quaternion identity = Quaternion.identity;
		if (colliders == null || colliders.Length == 0)
		{
			colliders = new Collider[1];
		}
		if (seeThroughOccluderCheckIndividualObjects)
		{
			for (int i = 0; i < rms.Length; i++)
			{
				if (rms[i].renderer != null)
				{
					Bounds bounds = rms[i].renderer.bounds;
					Vector3 center = bounds.center;
					float maxDistance = Vector3.Distance(center, position);
					Vector3 halfExtents = bounds.extents * seeThroughOccluderThreshold;
					if (Physics.OverlapBoxNonAlloc(center, halfExtents, colliders, identity, seeThroughOccluderMask) > 0)
					{
						lastOcclusionTestResult = true;
						return true;
					}
					if (Physics.BoxCast(center, halfExtents, (position - center).normalized, identity, maxDistance, seeThroughOccluderMask))
					{
						lastOcclusionTestResult = true;
						return true;
					}
				}
			}
			lastOcclusionTestResult = false;
			return false;
		}
		Bounds bounds2 = rms[0].renderer.bounds;
		for (int j = 1; j < rms.Length; j++)
		{
			if (rms[j].renderer != null)
			{
				bounds2.Encapsulate(rms[j].renderer.bounds);
			}
		}
		Vector3 center2 = bounds2.center;
		Vector3 halfExtents2 = bounds2.extents * seeThroughOccluderThreshold;
		if (Physics.OverlapBoxNonAlloc(center2, halfExtents2, colliders, identity, seeThroughOccluderMask) > 0)
		{
			lastOcclusionTestResult = true;
			return true;
		}
		float maxDistance2 = Vector3.Distance(center2, position);
		lastOcclusionTestResult = Physics.BoxCast(center2, halfExtents2, (position - center2).normalized, identity, maxDistance2, seeThroughOccluderMask);
		return lastOcclusionTestResult;
	}

	private void AddWithoutRepetition<T>(List<T> target, List<T> source)
	{
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			T val = source[i];
			if (val != null && !target.Contains(val))
			{
				target.Add(val);
			}
		}
	}

	private void CheckOcclusionAccurate(CommandBuffer cbuf, Camera cam)
	{
		if (!cachedOccludersPerCamera.TryGetValue(cam, out var value))
		{
			value = new List<Renderer>();
			cachedOccludersPerCamera[cam] = value;
		}
		float time = Time.time;
		int frameCount = Time.frameCount;
		if (!(Time.time - occlusionCheckLastTime < seeThroughOccluderCheckInterval) || !Application.isPlaying || occlusionRenderFrame == frameCount)
		{
			if (rms.Length == 0 || rms[0].renderer == null)
			{
				return;
			}
			occlusionCheckLastTime = time;
			occlusionRenderFrame = frameCount;
			Quaternion identity = Quaternion.identity;
			Vector3 position = cam.transform.position;
			value.Clear();
			if (occluderHits == null || occluderHits.Length < 50)
			{
				occluderHits = new RaycastHit[50];
			}
			if (seeThroughOccluderCheckIndividualObjects)
			{
				for (int i = 0; i < rms.Length; i++)
				{
					if (rms[i].renderer != null)
					{
						Bounds bounds = rms[i].renderer.bounds;
						Vector3 center = bounds.center;
						float maxDistance = Vector3.Distance(center, position);
						int num = Physics.BoxCastNonAlloc(center, bounds.extents * seeThroughOccluderThreshold, (position - center).normalized, occluderHits, identity, maxDistance, seeThroughOccluderMask);
						for (int j = 0; j < num; j++)
						{
							occluderHits[j].collider.transform.root.GetComponentsInChildren(tempRR);
							AddWithoutRepetition(value, tempRR);
						}
					}
				}
			}
			else
			{
				Bounds bounds2 = rms[0].renderer.bounds;
				for (int k = 1; k < rms.Length; k++)
				{
					if (rms[k].renderer != null)
					{
						bounds2.Encapsulate(rms[k].renderer.bounds);
					}
				}
				Vector3 center2 = bounds2.center;
				float maxDistance2 = Vector3.Distance(center2, position);
				int num2 = Physics.BoxCastNonAlloc(center2, bounds2.extents * seeThroughOccluderThreshold, (position - center2).normalized, occluderHits, identity, maxDistance2, seeThroughOccluderMask);
				for (int l = 0; l < num2; l++)
				{
					occluderHits[l].collider.transform.root.GetComponentsInChildren(tempRR);
					AddWithoutRepetition(value, tempRR);
				}
			}
		}
		int count = value.Count;
		if (count > 0)
		{
			for (int m = 0; m < count; m++)
			{
				Renderer renderer = value[m];
				cbuf.DrawRenderer(renderer, fxMatSeeThroughMask);
			}
		}
	}

	public List<Renderer> GetOccluders(Camera camera)
	{
		List<Renderer> value = null;
		if (cachedOccludersPerCamera != null)
		{
			cachedOccludersPerCamera.TryGetValue(camera, out value);
		}
		return value;
	}
}
