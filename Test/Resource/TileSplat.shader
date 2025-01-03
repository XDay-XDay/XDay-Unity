Shader "XDay/TileSplat"
{
    Properties
    {
        _NormalMap ("Normal Map", 2D) = "white" {}
        _Layer0 ("Layer 0", 2D) = "white" {}
        _Layer1 ("Layer 1", 2D) = "white" {}
        _Layer2 ("Layer 2", 2D) = "white" {}
        _Layer3 ("Layer 3", 2D) = "white" {}
        _SplatMask ("Mask Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
            };

            struct v2f
            {
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _NormalMap;
            sampler2D _Layer0;
            sampler2D _Layer1;
            sampler2D _Layer2;
            sampler2D _Layer3;
            sampler2D _SplatMask;

            float4 _NormalMap_ST;
            float4 _Layer0_ST;
            float4 _Layer1_ST;
            float4 _Layer2_ST;
            float4 _Layer3_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0.xy = v.uv0;
                o.uv0.zw = TRANSFORM_TEX(v.uv0, _NormalMap);
                o.uv1.xy = TRANSFORM_TEX(v.uv0, _Layer0);
                o.uv1.zw = TRANSFORM_TEX(v.uv0, _Layer1);
                o.uv2.xy = TRANSFORM_TEX(v.uv0, _Layer2);
                o.uv2.zw = TRANSFORM_TEX(v.uv0, _Layer3);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = UnpackNormal(tex2D(_NormalMap, i.uv0.zw));
                float3 rotatedNormal = float3(normal.x, normal.z, -normal.y);
                fixed4 layer0 = tex2D(_Layer0, i.uv1.xy);
                fixed4 layer1 = tex2D(_Layer1, i.uv1.zw);
                fixed4 layer2 = tex2D(_Layer2, i.uv2.xy);
                fixed4 layer3 = tex2D(_Layer3, i.uv2.zw);

                fixed4 mask = tex2D(_SplatMask, i.uv0);
                float diffuse = max(dot(rotatedNormal, _WorldSpaceLightPos0.xyz), 0.0);
                fixed4 albedo = mask.r * layer0 + mask.g * layer1 + mask.b * layer2 + mask.a * layer3;
                albedo.rgb = diffuse * albedo.rgb * _LightColor0.rgb;
                return albedo;
            }

            ENDCG
        }
    }
}
