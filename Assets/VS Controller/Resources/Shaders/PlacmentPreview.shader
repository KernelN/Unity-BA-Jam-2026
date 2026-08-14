Shader "PlacmentPreview"
{
    Properties
    {
        _Color ("Color", Color) = (0,1,0,0.35)
        _RimPower ("Rim Power", Range(1,8)) = 3
        _RimBoost ("Rim Boost", Range(0,4)) = 1.5

        [HDR]_GlowColor ("Glow Color", Color) = (0,0,0,0)
        _GlowStrength ("Glow Strength", Range(0,10)) = 2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _RimPower;
            float _RimBoost;

            fixed4 _GlowColor;
            float _GlowStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 nrmWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float3 wsPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.nrmWS = UnityObjectToWorldNormal(v.normal);
                o.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - wsPos);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            float Dither8x8(float2 pix)
            {
                float2 p = fmod(pix, 8.0);
                float idx = p.x + p.y * 8.0;
                return frac(sin(idx * 12.9898) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.nrmWS);
                float3 v = normalize(i.viewDirWS);

                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * _RimBoost;

                fixed4 col = _Color;

                col.rgb = col.rgb + rim;

                float2 sp = (i.screenPos.xy / i.screenPos.w) * _ScreenParams.xy;
                float d = Dither8x8(sp);

                float a = lerp(col.a, 1.0, saturate(rim));
                clip(a - d);

                col.rgb += _GlowColor.rgb * _GlowStrength * saturate(rim);

                col.a = 1; 
                return col;
            }
            ENDCG
        }
    }
}