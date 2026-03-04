Shader "WildTamer/FowBrushStamp"
{
    Properties
    {
        _BrushTex    ("Brush Texture", 2D)     = "white" {}
        _StampUV     ("Stamp UV",      Vector)  = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius",  Float)   = 0.15
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest  Always
        Cull   Off
        Blend  One One      // additive: accumulate revealed areas

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BrushTex);
            SAMPLER(sampler_BrushTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BrushTex_ST;
                float4 _StampUV;        // xy = stamp center in RT UV space
                float  _BrushRadius;    // brush half-size as a fraction of the RT
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // CommandBuffer.Blit sets MVP to identity; TransformObjectToHClip works correctly.
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 delta   = IN.uv - _StampUV.xy;
                float2 localUV = delta / max(_BrushRadius, 0.001) + 0.5;

                // Discard pixels outside the brush rect (clip discards when value < 0).
                clip(localUV.x);
                clip(localUV.y);
                clip(1.0 - localUV.x);
                clip(1.0 - localUV.y);

                half brush = SAMPLE_TEXTURE2D(_BrushTex, sampler_BrushTex, localUV).r;
                return half4(brush, brush, brush, brush);
            }
            ENDHLSL
        }
    }
}
