Shader "MainApp/PhotoPerspective"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Zoom ("Zoom", Float) = 1
        _Pitch ("Pitch", Range(-0.2, 0.2)) = 0
        _Yaw ("Yaw", Range(-0.2, 0.2)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "CanUseSpriteAtlas" = "True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Zoom;
            float _Pitch;
            float _Yaw;

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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 p = uv * 2.0 - 1.0;

                // Stick up = look down: magnify the far (top) uniformly, no trapezoid squash.
                float pitch = clamp(_Pitch, -0.2, 0.2);
                float yaw = clamp(_Yaw, -0.2, 0.2);
                float persp = max(0.72, 1.0 - p.y * pitch * 0.55);
                p /= persp;
                p.x -= p.y * yaw;

                float zoom = max(_Zoom, 1.0);
                float size = 1.0 / zoom;
                p *= size;
                uv = p * 0.5 + 0.5;
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return fixed4(0, 0, 0, 1);
                }

                return tex2D(_MainTex, uv) * _Color;
            }
            ENDCG
        }
    }
}
