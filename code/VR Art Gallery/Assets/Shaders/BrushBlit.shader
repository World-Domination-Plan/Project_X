Shader "Hidden/BrushBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BrushTex ("Brush", 2D) = "white" {}
        _BrushColor ("Brush Color", Color) = (1,0,0,1)
        _BrushParams ("Brush Params", Vector) = (0.5, 0.5, 0.05, 0.8)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _BrushTex;
            float4 _BrushColor;
            float4 _BrushParams; // xy=center UV, z=radius, w=hardness

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 brushCenter = _BrushParams.xy;
                float brushRadius = _BrushParams.z;
                float hardness = _BrushParams.w;

                float2 diff = i.uv - brushCenter;
                float dist = length(diff);

                fixed4 canvas = tex2D(_MainTex, i.uv);

                if (dist < brushRadius)
                {
                    float2 brushUV = (diff / brushRadius) * 0.5 + 0.5;
                    float brushMask = tex2D(_BrushTex, brushUV).a;

                    float falloff = 1.0 - smoothstep(brushRadius * hardness, brushRadius, dist);
                    float influence = brushMask * falloff;
                    float opacity = influence * _BrushColor.a;

                    fixed4 result = canvas;
                    result.rgb = lerp(canvas.rgb, _BrushColor.rgb, opacity);
                    result.a = canvas.a;
                    return result;
                }

                return canvas;
            }
            ENDCG
        }
    }
}