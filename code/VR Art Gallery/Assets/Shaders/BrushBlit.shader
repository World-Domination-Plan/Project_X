Shader "Hidden/BrushBlit"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            Name "BrushBlit"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BrushTex);
            SAMPLER(sampler_BrushTex);

            float4 _BrushColor;
            float4 _BrushParams; // (u, v, radiusUV, hardness)

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                // For Graphics.Blit fullscreen quad: treat input XY as clip-space
                OUT.positionHCS = float4(IN.positionOS.xy, 0.0, 1.0);
                OUT.uv = IN.uv;
                return OUT;
            }

            float SoftCircle(float2 p, float2 c, float r, float h)
            {
                float d = distance(p, c);
                float inner = r * (1.0 - saturate(h));
                return 1.0 - smoothstep(inner, r, d);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float2 c = _BrushParams.xy;
                float  r = _BrushParams.z;
                float  h = _BrushParams.w;

                float a = SoftCircle(uv, c, r, h);

                float2 local = (uv - c) / max(r, 1e-5) * 0.5 + 0.5;
                half mask = SAMPLE_TEXTURE2D(_BrushTex, sampler_BrushTex, local).r;

                half strength = (half)a * mask;
                return lerp(baseCol, _BrushColor, strength);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
