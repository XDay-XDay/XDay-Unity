Shader "Unlit/RigUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AnimationTexture("Anim Texture", 2D) = "white" {}
        _AnimPlayState("Play Info", Vector) = (0, 0, 0, 0)
        _AnimFrameData("Frame Info", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Back
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(_Buffer)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AnimPlayState)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AnimFrameData)
            UNITY_INSTANCING_BUFFER_END(_Buffer)

            #include "../../../../../Runtime/Implementation/Animation/Resource/Include/RigAnimation.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
                RIG_ANIMATION_VERTEX_DATA
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                float3 objectPos = GPUSkin(v.vertex, v.weights, v.indices);
                o.vertex = UnityObjectToClipPos(objectPos);
                o.uv = v.uv;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
