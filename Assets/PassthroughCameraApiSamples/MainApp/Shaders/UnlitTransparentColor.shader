// Copyright (c) Meta Platforms, Inc. and affiliates.
// Plain Built-in Render Pipeline shader (UnityCG.cginc only) - works regardless of
// whether URP is installed, unlike shaders that include Packages/com.unity.render-pipelines.universal.
Shader "MainApp/UnlitTransparentColor"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 0.6)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
