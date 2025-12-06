Shader "XDay/FogOfWar"
{
    Properties
    {
        _FogColorDark ("Fog Color Dark", Color) = (0, 0, 0, 0.5)
        _FogColorBright ("Fog Color Bright", Color) = (0, 0, 0, 0.5)
        _Mask ("Mask", 2D) = "white" {}
        _MaskScaleOffset ("Mask Scale Offet", Vector) = (1, 1, 1, 1)
        _FogNoise ("Fog Noise", 2D) = "white" {}
        _NoiseScaleOffset ("Noise Scale Offset", Vector) = (0, 0, 0, 0)
        _EdgeSoftness ("Edge Softness", Range(0, 3)) = 1
        _FogDensity ("FogDensity", Range(0, 1)) = 0.5
        _FadeDuration ("Duration", Range(0, 10)) = 1
        _ElapsedTime ("Elapsed Time", float) = 0
        _Wind("WindDirectionSpeed", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True"
        }
        
        LOD 100
        
        Cull Off
        ZWrite Off
        ZTest Always
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _FogColorDark;
            fixed4 _FogColorBright;
            sampler2D _Mask;
            float4 _MaskScaleOffset;
            float4 _NoiseScaleOffset;

            sampler2D _FogNoise;
            float _EdgeSoftness;
            float _FogDensity;
           
            float _FadeDuration;
            float _ElapsedTime;
           
            float4 _Wind;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                return o;
            }

            fixed4 GetFogColor(fixed4 maskCur, float2 noise2, fixed4 fogColorCombine)
            {
                float4 fogStrength = saturate(maskCur);
              
                fogStrength = smoothstep(saturate(1-_EdgeSoftness * noise2.r), 1, fogStrength);
                fixed4 result = fogColorCombine;
                result.a *= fogStrength.r;
                return result;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 maskUV = (i.uv * _MaskScaleOffset.xy) + _MaskScaleOffset.zw;
                float2 noiseUV = (i.uv* _NoiseScaleOffset.xy) + _NoiseScaleOffset.zw;

                fixed4 maskCur = tex2D(_Mask, maskUV);
             
                float2 noiseUV2 = noiseUV + _Wind.zw  * _Time.x;
                float2 noise2 = tex2D(_FogNoise, noiseUV2).rg;
                fixed4 fogColorCombine = lerp(_FogColorBright, _FogColorDark, noise2.g);

                fixed4 fogCur = GetFogColor(maskCur, noise2, fogColorCombine);
                
                fogCur.a *= _FogDensity;
                return fogCur;
            }

            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}