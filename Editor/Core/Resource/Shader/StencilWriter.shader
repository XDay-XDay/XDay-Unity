Shader "XDay/StencilWriter"
{
    Properties
    {
        _StencilRef ("Stencil Ref", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-100" }
        LOD 100

        Pass
        {
            ColorMask 0
            ZWrite Off
            Stencil
            {
                Ref [_StencilRef]          // 设置为1的值
                Comp Always    // 总是通过模板测试
                Pass Replace   // 用参考值替换缓冲区值
                Fail Keep      // 失败时保持原值
                ZFail Keep     // 深度失败时保持原值
                ReadMask 255   // 读取完整字节掩码
                WriteMask 255  // 写入完整字节掩码
            }

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
