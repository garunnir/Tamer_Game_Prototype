// WildTamer/MinimapFog
// URP Unlit UI shader for the minimap fog-of-war overlay.
//
// Rendering logic:
//   - _BackgroundTex : the world/terrain map image shown on the minimap.
//   - _FogMaskTex    : greyscale RenderTexture produced by FowBrushStamp.
//                      Alpha (or R channel) == 1 means HIDDEN, == 0 means REVEALED.
//   - smoothstep applied around the fog edge for a soft gradient.
//   - Final alpha written so the UI canvas can composite correctly.
//
// UI Canvas setup → see manual_setting.md (Step 5).

Shader "WildTamer/MinimapFog"
{
    Properties
    {
        [NoScaleOffset] _BackgroundTex ("Background Map",  2D)    = "white"  {}
        [NoScaleOffset] _FogMaskTex    ("Fog Mask RT",     2D)    = "black"  {}

        // Fog colour for hidden areas (default opaque black).
        _FogColor       ("Fog Color",           Color)            = (0, 0, 0, 1)

        // smoothstep band: fog transitions from fully revealed to fully hidden
        // between _EdgeLow and _EdgeHigh (in mask-alpha space [0, 1]).
        // Typical values: Low = 0.45, High = 0.55 for a tight but soft edge.
        _EdgeLow        ("Edge Smoothstep Low",  Range(0, 1))     = 0.45
        _EdgeHigh       ("Edge Smoothstep High", Range(0, 1))     = 0.55

        // ── Local View scrolling (set by MinimapScrollController) ────────────
        // _PlayerPos  : normalised player position in global-map UV space [0,1].
        //               Set by script as (playerX-worldMin.x)/worldSize.x, same
        //               formula as WorldToMinimapUV global path.
        // _ViewRadius : world-space radius of the local window (world units).
        // _WorldSize  : full world dimensions in world units (xy).
        // _IsLocalView: 1 = Local scroll active, 0 = Global (default).
        _PlayerPos      ("Player Pos (normalized XZ)", Vector)    = (0.5, 0.5, 0, 0)
        _ViewRadius     ("View Radius (world units)",  Float)     = 30
        _WorldSize      ("World Size  (world units)",  Vector)    = (100, 100, 0, 0)
        [Toggle] _IsLocalView ("Local View",           Float)     = 0

        // Unity UI stencil / masking support (required for Mask components).
        _StencilComp    ("Stencil Comparison",  Float)            = 8
        _Stencil        ("Stencil ID",          Float)            = 0
        _StencilOp      ("Stencil Operation",   Float)            = 0
        _StencilWriteMask ("Stencil Write Mask",Float)            = 255
        _StencilReadMask  ("Stencil Read Mask", Float)            = 255
        _ColorMask        ("Color Mask",        Float)            = 15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "IgnoreProjector"= "True"
            "PreviewType"    = "Plane"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull    Off
        ZWrite  Off
        ZTest   [unity_GUIZTestMode]
        Blend   SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "MinimapFog"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Textures ────────────────────────────────────────────────────────

            TEXTURE2D(_BackgroundTex);
            SAMPLER(sampler_BackgroundTex);

            TEXTURE2D(_FogMaskTex);
            SAMPLER(sampler_FogMaskTex);

            // ── Constant buffer ─────────────────────────────────────────────────

            CBUFFER_START(UnityPerMaterial)
                half4  _FogColor;
                float  _EdgeLow;
                float  _EdgeHigh;
                // Required by URP UI batching even if unused here.
                half4  _BackgroundTex_ST;
                half4  _FogMaskTex_ST;
                // Local View scrolling
                float4 _PlayerPos;      // xy = normalised global-map UV [0,1]
                float  _ViewRadius;     // world-space radius
                float4 _WorldSize;      // xy = world dimensions in world units
                float  _IsLocalView;    // 1 = local, 0 = global
            CBUFFER_END

            // ── Vertex input / output ───────────────────────────────────────────

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;       // vertex tint from CanvasRenderer
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  color       : COLOR;
            };

            // ── Vertex shader ───────────────────────────────────────────────────

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            // ── Fragment shader ─────────────────────────────────────────────────

            half4 frag(Varyings IN) : SV_Target
            {
                // ── Background UV (local scroll or global pass-through) ───────
                //
                // Global mode : bgUV == IN.uv  (identity, full-world sampling).
                // Local mode  : bgUV is remapped to the clamped View Window so
                //               the background image scrolls with the player.
                //
                // The clamped-window math mirrors MinimapController.SetViewCenter()
                // exactly, keeping shader and C# in sync without any extra uniforms.
                //
                // Clamp policy: saturate() acts as a hardware Clamp wrap mode,
                // preventing the sampler from tiling at map edges.

                float2 bgUV = IN.uv;

                if (_IsLocalView > 0.5)
                {
                    // Normalise radius to UV space (world units → [0,1]).
                    float2 rNorm = _ViewRadius / max(_WorldSize.xy, 0.001);
                    float2 lo    = rNorm;
                    float2 hi    = 1.0 - rNorm;

                    // Clamp window center to keep the window inside world bounds.
                    // When rNorm >= 0.5 the radius covers the entire axis → show the
                    // full world on that axis (center = 0.5, winSize = 1.0).
                    float2 center;
                    center.x = (lo.x < hi.x) ? clamp(_PlayerPos.x, lo.x, hi.x) : 0.5;
                    center.y = (lo.y < hi.y) ? clamp(_PlayerPos.y, lo.y, hi.y) : 0.5;

                    float2 winSize;
                    winSize.x = (lo.x < hi.x) ? (rNorm.x * 2.0) : 1.0;
                    winSize.y = (lo.y < hi.y) ? (rNorm.y * 2.0) : 1.0;

                    // Map screen UV [0,1] → background sub-region, then clamp.
                    bgUV = saturate(center - winSize * 0.5 + IN.uv * winSize);
                }

                // Sample background map at the (possibly remapped) UV.
                half4 background = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, bgUV);

                // ── Fog mask ─────────────────────────────────────────────────
                // Sampled at bgUV — the SAME coordinate used for the background.
                // FowController.StampAtCamera() always stamps in global UV
                // (WorldToGlobalUV), so the FoW RT is a persistent global-space map.
                // Using bgUV here means both textures are looked up at the same
                // global position, keeping fog perfectly aligned with the terrain
                // in both Global and Local view modes.
                //
                // R channel: 0 = fully fogged, 1 = fully revealed  (additive accum).
                half maskValue    = SAMPLE_TEXTURE2D(_FogMaskTex, sampler_FogMaskTex, bgUV).r;
                half revealedMask = saturate(maskValue);
                half fogAlpha     = 1.0h - revealedMask;

                // Soft edge via smoothstep over the fog boundary.
                half softFog = smoothstep(_EdgeLow, _EdgeHigh, fogAlpha);

                // Composite: lerp from background to fog colour by softFog.
                half4 result;
                result.rgb = lerp(background.rgb, _FogColor.rgb, softFog);
                result.a   = lerp(background.a,   _FogColor.a,   softFog);

                // Multiply by vertex colour (Canvas tint / alpha).
                result *= IN.color;

                return result;
            }
            ENDHLSL
        }
    }

    // No fallback — UI shaders should not silently fall back to legacy pipelines.
    Fallback Off
}
