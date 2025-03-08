Shader "Unlit/VertexDotsUnlit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _AnimationTexture("Anim Texture", 2D) = "white" {}
        _AnimPlayState("Play State", Vector) = (0, 0, 0, 0)
        _AnimFrameData("Frame Data", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _AnimPlayState;
                float4 _AnimFrameData;
                float4 _AnimationTexture_TexelSize;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _AnimPlayState)
                    UNITY_DOTS_INSTANCED_PROP(float4, _AnimFrameData)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                #define _AnimPlayState UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _AnimPlayState)
                #define _AnimFrameData UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _AnimFrameData)
            #endif

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            #include "../../../../../Runtime/Implementation/Animation/Resource/Include/VertexAnimationDots.cginc"

            Varyings vert (Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input)
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                half3 objectPos = SampleVertexPosition(input.uv.z).xyz;
                output.positionCS = TransformObjectToHClip(objectPos);
                output.uv = input.uv.xy;
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 col = half4(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv));
                return col;
            }
            ENDHLSL
        }
    }
}
