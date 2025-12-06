Shader "XDay/TileSplat_Lit"
{
    Properties
    {
        [Header(Layer Model)]
        [KeywordEnum(LAYER2, LAYER3,LAYER4)]_LayerMode("层数(2，3，4)", float) = 0
        [KeywordEnum(AFTERLAYER2, AFTERLAYER3,AFTERLAYER4)]_AfterLayerMode("层数(2，3，4)", float) = 0
        [Header(Textures)]
        _Layer0 ("Layer 0 roughness(A)", 2D) = "white" {}
       
        _Layer1 ("Layer 1 roughness(A)", 2D) = "white" {}
       
        _Layer2 ("Layer 2 roughness(A)", 2D) = "white" {}
        
         _Layer3 ("Layer 3 roughness(A)", 2D) = "white" {}
       
        _SplatMask ("Mask Texture", 2D) = "white" {}
        
        _LayerAfter_0 ("Layer After 0 roughness(A)", 2D) = "white" {}
       
        _LayerAfter_1 ("Layer After 1 roughness(A)", 2D) = "white" {}
       
        _LayerAfter_2 ("Layer After 2 roughness(A)", 2D) = "white" {}
        
        _LayerAfter_3 ("Layer After 3 roughness(A)", 2D) = "white" {}
        
        _SplatMask_After ("Mask Texture After", 2D) = "white" {}
        
        _EdgeNoiseTex ("EdgeNoiseTex", 2D) = "black" {}
        _EdgeSoftness("EdgeSoftness", Range(0.1, 3)) = 0.1
        
        [Header(Lighting)]
        _CustomLightColor ("Light Color", Color) = (1, 1, 1, 1)
        _LightDirection ("Light Direction", Vector) = (0, 1, 0, 0)
        [Space]
        _CustomAmbientColor ("Ambient Color", Color) = (0.2, 0.2, 0.2, 1)
        _AmbientIntensity ("Ambient Intensity", Range(0, 1)) = 0.2
        _Color0 ("Color0", Color) = (1, 1, 1, 1)
        _Color1 ("Color1", Color) = (1, 1, 1, 1)
        _Color2 ("Color2", Color) = (1, 1, 1, 1)
        _Color3 ("Color3", Color) = (1, 1, 1, 1)
        
        _Color0_After ("Color0After", Vector) = (1, 1, 1, 1)
        _Color1_After ("Color1After", Vector) = (1, 1, 1, 1)
        _Color2_After ("Color2After", Vector) = (1, 1, 1, 1)
        _Color3_After ("Color3After", Vector) = (1, 1, 1, 1)
        
        _Color0_After ("Color0After", Vector) = (1, 1, 1, 1)
        _Color1_After ("Color1After", Vector) = (1, 1, 1, 1)
        _Color2_After ("Color2After", Vector) = (1, 1, 1, 1)
        _Color3_After ("Color3After", Vector) = (1, 1, 1, 1)
        
        [Header(Specular)]
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Roughness ("Roughness Intensity", Range(0.001, 1)) = 0.1
        
        
        [Header(Light Direction Control)]
        _LightDirectionTransform ("Light Direction Transform", Vector) = (0, 0, 0, 0)
        
        
        [Header(BlendTwoTerrain)]
        _BlendMap ("BlendMap", 2D) = "black" {}
        _WorldMaskScale("WorldMaskScaleOffset",Vector) = (1,1,0,0)
        
        [Header(Dissolve)]
        _NoiseMap("Dissolve Shape", 2D) = "white" {}
        _DisEdgeWidth(" Dissolve Edge Width", Range(0, 0.3)) = 0.01
        _DissolveEdgeColor1("Dissolve Edge Color1", Color) = (0.5443275,1,0.3443396,1)
        _DissolveEdgeColor2("Dissolve Edge Color2", Color) = (1,0.5443275,0.3443396,1)
        
        _DissolveEdgeColor("Dissolve Edge Color", Color) = (0.5443275,1,0.3443396,1)

        _EdgeIntensity("Edge Intensity", Range(1, 10)) = 6.36
        _EdgeContrast("Edge Threshold", Range(0, 2)) = 1
        _EdgeGradientThreshold("Threshold", Range(0, 1)) = 0.174
        _EdgeGradientContrast("Edge Gradient Contrast", Range(0, 2)) = 1.87
        _CenterRadius("DissolveCenter",Vector) = (1,1,0,0)
        _FadeDuration("FadeDuration", Float) = 4
        _ElapsedTime("ElapsedTime", Float) = 4
        
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature  _LAYERMODE_LAYER2 _LAYERMODE_LAYER3 _LAYERMODE_LAYER4
            #pragma shader_feature  _AFTERLAYERMODE_AFTERLAYER2 _AFTERLAYERMODE_AFTERLAYER3 _AFTERLAYERMODE_AFTERLAYER4

            #pragma  multi_compile _ _DissolveTerrain

           // #define _DissolveTerrain
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 uv0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;

                #if _LAYERMODE_LAYER3 || _LAYERMODE_LAYER4
                float4 uv2 : TEXCOORD2;
                #endif
                float3 worldNormal : TEXCOORD3;
                float3 worldPos : TEXCOORD4;
                float4 uv3 : TEXCOORD5;
                #if _AFTERLAYERMODE_AFTERLAYER3 || _AFTERLAYERMODE_AFTERLAYER4
                float4 uv4 : TEXCOORD6;
                #endif
                float4 uv5 : TEXCOORD7;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_Layer0); SAMPLER(sampler_Layer0);
            TEXTURE2D(_Layer1); SAMPLER(sampler_Layer1);
            #if _LAYERMODE_LAYER3 || _LAYERMODE_LAYER4
            TEXTURE2D(_Layer2); SAMPLER(sampler_Layer2);
            #endif
            #if _LAYERMODE_LAYER4 
            TEXTURE2D(_Layer3); SAMPLER(sampler_Layer3);
            #endif
            
            TEXTURE2D(_SplatMask); SAMPLER(sampler_SplatMask);


            TEXTURE2D(_LayerAfter_0); SAMPLER(sampler_LayerAfter_0);
            TEXTURE2D(_LayerAfter_1); SAMPLER(sampler_LayerAfter_1);
            #if _AFTERLAYERMODE_AFTERLAYER3 || _AFTERLAYERMODE_AFTERLAYER4
            TEXTURE2D(_LayerAfter_2); SAMPLER(sampler_LayerAfter_2);
            #endif
            #if _AFTERLAYERMODE_AFTERLAYER4
            TEXTURE2D(_LayerAfter_3); SAMPLER(sampler_LayerAfter_3);
            #endif
            TEXTURE2D(_SplatMask_After); SAMPLER(sampler_SplatMask_After);

            TEXTURE2D(_BlendMap); SAMPLER(sampler_BlendMap);

            TEXTURE2D(_EdgeNoiseTex); SAMPLER(sampler_EdgeNoiseTex);

            SAMPLER(sampler_NoiseMap);TEXTURE2D(_NoiseMap); 

            CBUFFER_START(UnityPerMaterial)
            float4 _Layer0_ST;
            float4 _Layer1_ST;
            #if _LAYERMODE_LAYER3 || _LAYERMODE_LAYER4
            float4 _Layer2_ST;
            #endif

            #if _LAYERMODE_LAYER4 
            float4 _Layer3_ST;
            #endif

            float4 _LayerAfter_0_ST;
            float4 _LayerAfter_1_ST;
            #if _AFTERLAYERMODE_AFTERLAYER3 || _AFTERLAYERMODE_AFTERLAYER4
            float4 _LayerAfter_2_ST;
            #endif

            #if _AFTERLAYERMODE_AFTERLAYER4
            float4 _LayerAfter_3_ST;
            #endif

            float4 _EdgeNoiseTex_ST;
            float4 _NoiseMap_ST;


            float4 _CenterRadius;
            float _ElapsedTime;
            float _FadeDuration;


            float _DisEdgeWidth;
            float _EdgeIntensity;

            float4 _DissolveEdgeColor1;
            float4 _DissolveEdgeColor2;

             float4 _DissolveEdgeColor;

            float4 _WorldMaskScale;
            
           // float4 _LightColor;
            half3 _CustomLightColor;
            float4 _LightDirection;
            half3 _CustomAmbientColor;
            float _AmbientIntensity;
            
            half4 _SpecularColor;
            half4 _Color0;
            half4 _Color1;
            half4 _Color2;
            half4 _Color3;
           // float _SpecularPower;

            half4 _Color0_After;
            half4 _Color1_After;
            half4 _Color2_After;
            half4 _Color3_After;

            float _EdgeContrast;
            float _EdgeGradientContrast;
            float  _EdgeGradientThreshold;

            float _Roughness;
            float _EdgeSoftness;
            
            float4 _LightDirectionTransform;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv0.xy = v.uv0;
                o.uv1.xy = TRANSFORM_TEX(v.uv0, _Layer0);
                o.uv1.zw = TRANSFORM_TEX(v.uv0, _Layer1);

                #if _LAYERMODE_LAYER3 || _LAYERMODE_LAYER4
                o.uv2.xy = TRANSFORM_TEX(v.uv0, _Layer2);
                #endif

                #if _LAYERMODE_LAYER4
                o.uv2.zw = TRANSFORM_TEX(v.uv0, _Layer3);
                #endif
                
                


                o.uv3.xy = TRANSFORM_TEX(v.uv0, _LayerAfter_0);
                o.uv3.zw = TRANSFORM_TEX(v.uv0, _LayerAfter_1);

                #if _AFTERLAYERMODE_AFTERLAYER3 || _AFTERLAYERMODE_AFTERLAYER4
                o.uv4.xy = TRANSFORM_TEX(v.uv0, _LayerAfter_2);
                #endif

                #if _AFTERLAYERMODE_AFTERLAYER4
                o.uv4.zw = TRANSFORM_TEX(v.uv0, _LayerAfter_3);
                #endif

                o.uv5.xy = TRANSFORM_TEX(v.uv0, _EdgeNoiseTex);
                //o.uv5.zw = TRANSFORM_TEX(v.uv0, _NoiseMap);
                
                
                // Calculate world space normal and position
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                // Sample all layers
                half4 layer0 = SAMPLE_TEXTURE2D(_Layer0, sampler_Layer0, i.uv1.xy) * _Color0;
                half4 layer1 = SAMPLE_TEXTURE2D(_Layer1, sampler_Layer1, i.uv1.zw) * _Color1;

                #if _LAYERMODE_LAYER3 || _LAYERMODE_LAYER4
                half4 layer2 = SAMPLE_TEXTURE2D(_Layer2, sampler_Layer2, i.uv2.xy) * _Color2;
                #endif

                #if _LAYERMODE_LAYER4
                half4 layer3 = SAMPLE_TEXTURE2D(_Layer3, sampler_Layer3, i.uv2.zw) * _Color3;
                #endif


                // Sample all layers
                half4 layerAfter_0 = SAMPLE_TEXTURE2D(_LayerAfter_0, sampler_LayerAfter_0, i.uv3.xy) * _Color0_After;
                half4 layerAfter_1 = SAMPLE_TEXTURE2D(_LayerAfter_1, sampler_LayerAfter_1, i.uv3.zw) * _Color1_After;

                #if _AFTERLAYERMODE_AFTERLAYER3 || _AFTERLAYERMODE_AFTERLAYER4
                half4 layerAfter_2 = SAMPLE_TEXTURE2D(_LayerAfter_2, sampler_LayerAfter_2, i.uv4.xy) * _Color2_After;
                #endif

                #if _AFTERLAYERMODE_AFTERLAYER4
                half4 layerAfter_3 = SAMPLE_TEXTURE2D(_LayerAfter_3, sampler_LayerAfter_3, i.uv4.zw) * _Color3_After;
                #endif

                // Blend layers using splat mask
                half4 mask = SAMPLE_TEXTURE2D(_SplatMask, sampler_SplatMask, i.uv0);


                #if _LAYERMODE_LAYER3
                    half4 albedo = mask.r * layer0 + mask.g * layer1 + mask.b * layer2;
                #elif _LAYERMODE_LAYER4
                    half4 albedo = mask.r * layer0 + mask.g * layer1 + mask.b * layer2 + mask.a * layer3;
                #else
                    half4 albedo = mask.r * layer0 + mask.g * layer1;
                #endif

                half4 maskAfter = SAMPLE_TEXTURE2D(_SplatMask_After, sampler_SplatMask_After, i.uv0);

                #if _AFTERLAYERMODE_AFTERLAYER3
                    half4 albedoAfter = maskAfter.r * layerAfter_0 + maskAfter.g * layerAfter_1 + maskAfter.b * layerAfter_2;
                #elif _AFTERLAYERMODE_AFTERLAYER4
                    half4 albedoAfter = maskAfter.r * layerAfter_0 + maskAfter.g * layerAfter_1 + maskAfter.b * layerAfter_2 + maskAfter.a * layerAfter_3;
                #else
                      half4 albedoAfter = maskAfter.r * layerAfter_0 + maskAfter.g * layerAfter_1;
                #endif

                float2 BlendUV = i.worldPos.xz * _WorldMaskScale.xy + _WorldMaskScale.zw;
                half4 BlendValue = SAMPLE_TEXTURE2D(_BlendMap, sampler_BlendMap, BlendUV);
               
               half EdgeNoiseValue = SAMPLE_TEXTURE2D(_EdgeNoiseTex, sampler_EdgeNoiseTex, i.uv5.xy);
               BlendValue.rg = smoothstep(saturate(_EdgeSoftness * EdgeNoiseValue), 1, BlendValue.rg);
               albedo = lerp(albedo, albedoAfter,BlendValue.r);//formal

                //dissolve feature
                #if _DissolveTerrain

                    float2 noiseUV = i.worldPos.xz * _NoiseMap_ST.xy + _NoiseMap_ST.zw;
                    float4 noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV);
                    float2 center = _CenterRadius.xy;
                    float distanceToCenter = distance(i.worldPos.xz ,center) * _CenterRadius.z * 2;
                    float dissolveProgress = _ElapsedTime / _FadeDuration;
                    float pattern = lerp(distanceToCenter,0,dissolveProgress);
                    float dissolvePattern =  BlendValue.g - lerp(0.01 ,pattern ,noise.r);
                    dissolvePattern = lerp(dissolvePattern,1,dissolveProgress);
                    float threshold = lerp(1- dissolveProgress,1- BlendValue.g,dissolveProgress);

                    
                    float dissolve =  step(threshold, dissolvePattern) * BlendValue.g; //formal

                
                    albedo = lerp(albedo,albedoAfter,dissolve); //dissolve 在blendmapG 通道
                
                    float edge = step(threshold ,dissolvePattern - _DisEdgeWidth);
                    edge = dissolve - edge;


                    //half3 gradientColor =  InterpolateInHSL(_DissolveEdgeColor1,_DissolveEdgeColor2,dissolveProgress);
                   
                   albedo.rgb = lerp(albedo.rgb, _DissolveEdgeColor * _EdgeIntensity , saturate(edge)); //edge 


                   //
                   half4 debugColor = BlendValue.g;
                   // // //debugColor.rgb = gradientColor;
                   // debugColor.a = 1;
                   // return debugColor;
                   // half4 debugColor = half4(BlendUV,0,1) * BlendValue.g;
                   //return debugColor;
                #endif
                

                
               // // Get roughness from albedo alpha
                half roughness = albedo.a * _Roughness;

                //half roughnessAfter = albedoAfter.a * _Roughness;
                
                // Normalize normal
                half3 worldNormal = normalize(i.worldNormal);
                
                // Use custom light direction (with transform if provided)
                half3 lightDir = normalize(_LightDirection.xyz);
                if (_LightDirectionTransform.w > 0) {
                    lightDir = normalize(float3(_LightDirectionTransform.x, _LightDirectionTransform.y, _LightDirectionTransform.z));
                }
                
                // // Calculate diffuse lighting
                half NdotL = max(0.5, dot(worldNormal, lightDir));

                
                
                // Calculate ambient
                half3 ambient = lerp(_CustomAmbientColor * _AmbientIntensity,_CustomLightColor,NdotL);
                
                // Calculate view direction
                half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                half3 halfDir = normalize(lightDir + viewDir);

                //Fresnel Term
                half fresnelFactor = pow(1.0 - max(0, dot(worldNormal, viewDir)), 5);
                
                // Blinn-Phong
                half NdotH = max(0, dot(worldNormal, halfDir));
                half specularIntensity = pow(NdotH, roughness * 256);
                half3 specular = max(0,_SpecularColor.rgb  * specularIntensity * fresnelFactor);
                
                // Combine final color
                half4 finalColor;
                finalColor.rgb = min(3,ambient * albedo + specular * _CustomLightColor);
                finalColor.a = 1.0;

              
//distanceToCenter *
               //finalColor.rgb =  BlendValue.r;//half3(noiseUV,0);//i.worldPos.x * _CenterRadius.z *  BlendValue.r;//BlendValue.r * dissolve;
              // finalColor = dissolveProgress;// * BlendValue.g;
               
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    CustomEditor "TileSplatShaderGUI"
    
    FallBack "Universal Render Pipeline/Unlit"
}