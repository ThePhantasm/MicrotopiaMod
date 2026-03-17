using UnityEngine;
using UnityEngine.Rendering;

namespace HighlightPlus;

public delegate void FullScreenBlitMethod(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int passIndex);
