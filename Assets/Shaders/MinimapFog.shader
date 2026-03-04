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
                // Sample background map.
                half4 background = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, IN.uv);

                // Sample fog mask.
                // R channel: 1 = hidden, 0 = fully revealed
                // (FowBrushStamp writes revealed values additively, clamped to [0,1]
                //  so the RenderTexture starts black = fully hidden).
                half maskValue = SAMPLE_TEXTURE2D(_FogMaskTex, sampler_FogMaskTex, IN.uv).r;

                // fogAlpha: 1 = fully hidden (show fog), 0 = fully revealed (show map).
                // Invert the revealed mask so 0 = revealed, 1 = hidden.
                half revealedMask = saturate(maskValue);
                half fogAlpha = 1.0h - revealedMask;

                // Soft edge via smoothstep over the fog boundary.
                // smoothstep returns 0 when fogAlpha <= _EdgeLow,
                //                    1 when fogAlpha >= _EdgeHigh.
                half softFog = smoothstep(_EdgeLow, _EdgeHigh, fogAlpha);

                // Composite: lerp from background to fog colour by softFog.
                half4 result;
                result.rgb = lerp(background.rgb, _FogColor.rgb, softFog);

                // Preserve background alpha in revealed areas;
                // opaque fog colour in hidden areas.
                result.a = lerp(background.a, _FogColor.a, softFog);

                // Multiply by vertex colour (Canvas tint / alpha).
                result *= IN.color;

                return result;
            }
            ENDHLSL
        }
    }

    // No fallback — UI shaders should not silently fall back to legacy pipelines.
    FallbackError
}
