// Copyright (c) Meta Platforms, Inc. and affiliates.
// Built-in RP only (UnityCG.cginc). Grayscale + Sobel edge magnitude in one pass;
// the 3x3 neighborhood sampling itself acts as a light smoothing pass, standing in
// for a separate Gaussian blur step to keep this a single, cheap Blit.
Shader "MainApp/EdgeDetectionSobel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Luma(float2 uv, float2 offset)
            {
                float3 c = tex2D(_MainTex, uv + offset).rgb;
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;

                float tl = Luma(i.uv, texel * float2(-1, -1));
                float tc = Luma(i.uv, texel * float2(0, -1));
                float tr = Luma(i.uv, texel * float2(1, -1));
                float ml = Luma(i.uv, texel * float2(-1, 0));
                float mr = Luma(i.uv, texel * float2(1, 0));
                float bl = Luma(i.uv, texel * float2(-1, 1));
                float bc = Luma(i.uv, texel * float2(0, 1));
                float br = Luma(i.uv, texel * float2(1, 1));

                float gx = -tl - 2 * ml - bl + tr + 2 * mr + br;
                float gy = -tl - 2 * tc - tr + bl + 2 * bc + br;
                float edge = sqrt(gx * gx + gy * gy);

                return fixed4(edge, edge, edge, 1);
            }
            ENDCG
        }
    }
}
