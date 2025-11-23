Shader "XDay/Fog"
{
    Properties
    {
        _FogColorDark ("Fog Color Dark", Color) = (0, 0, 0, 0.5)
        _FogColorBright ("Fog Color Bright", Color) = (0, 0, 0, 0.5)
        _Mask ("Mask", 2D) = "white" {}
        _MaskScaleOffset ("Mask Scale Offet", Vector) = (1, 1, 1, 1)
        //_ShadowColor  ("ShadowColor", Color) = (0, 0, 0, 0.5)
        
        _FogNoise ("Fog Noise", 2D) = "white" {}
        _NoiseScaleOffset ("Noise Scale Offset", Vector) = (0, 0, 0, 0)
        //_EdgeNoiseScaleOffset (" Edge Noise Scale Offset", Vector) = (1, 1, 0, 0)
        _EdgeSoftness ("Edge Softness", Range(0, 3)) = 1
        _FogDensity ("FogDensity", Range(0, 1)) = 0.5
        _FadeDuration ("Duration", Range(0, 10)) = 1//start set
        _ElapsedTime ("Elapsed Time", Float) = 0.0 //update set
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
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ FADEFOGWAR

            #include "UnityCG.cginc"

            #define FOG_TIME _Time.x

           // #define FADEFOGWAR 1

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

            //fixed4 _ShadowColor;
            fixed4 _FogColorDark;
            fixed4 _FogColorBright;
            sampler2D _Mask;
            float4 _MaskScaleOffset;
            float4 _NoiseScaleOffset;

           // float4 _EdgeNoiseScaleOffset;

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
                // 计算世界坐标
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 maskUV = (i.uv * _MaskScaleOffset.xy) + _MaskScaleOffset.zw;
                float2 noiseUV = (i.uv* _NoiseScaleOffset.xy) + _NoiseScaleOffset.zw;

               // float2 edegeNoiseUV = (i.uv* _EdgeNoiseScaleOffset.xy) + _EdgeNoiseScaleOffset.zw;

                //float2 noiseUV1 = edegeNoiseUV + _Wind.xy * FOG_TIME;
               // half4 noise = tex2D(_FogNoise, noiseUV1);
                
                // 采样迷雾遮罩
                fixed4 maskAll = tex2D(_Mask, maskUV);
             
                float2 noiseUV2 = noiseUV + _Wind.zw  * FOG_TIME;
                float2 noise2 = tex2D(_FogNoise, noiseUV2).rg;
                
                // 计算迷雾强度
                float4 fogStrength = saturate(maskAll);
              
                // 边缘软化
                float4 trail = smoothstep( 0,0.5, max(0,fogStrength * (1 - fogStrength)));

                fogStrength = smoothstep(saturate(1-_EdgeSoftness * noise2.r), 1 , fogStrength );
               
               // fogStrength = lerp(fogStrength,noise.r,trail);

                //combine color
                fixed4 fogColorCombine = lerp(_FogColorBright,_FogColorDark,noise2.g);

                //shadowColor
               // fixed4 shadowColor = _ShadowColor * noise2 ;

               // fixed4 result = lerp(fogColorCombine ,shadowColor,saturate(fogStrength.g - fogStrength.r));

                fixed4 result = fogColorCombine;
                result.a *= fogStrength.r;//max(fogStrength.r,fogStrength.g) ;

               
                #if FADEFOGWAR
                    float progress = _ElapsedTime / _FadeDuration;
                    progress = saturate(progress);
                 //   fixed4 resultNew = lerp(fogColorCombine ,shadowColor,saturate(fogStrength.a - fogStrength.b));
                    fixed4 resultNew = fogColorCombine;
                    resultNew.a *= fogStrength.b;//max(fogStrength.b,fogStrength.a);
                    result = lerp(result,resultNew,progress);
                #endif
                
                result.a *= _FogDensity;
                return result;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}