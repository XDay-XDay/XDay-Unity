Shader "XDay/StencilReader"
{
    Properties
    {
        _StencilRef ("Stencil Ref", float) = 1
        _MainTex("Main Tex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Stencil
            {
                Ref [_StencilRef]          // 设置为1的值
                Comp Equal    // 总是通过模板测试
                Pass Keep   // 用参考值替换缓冲区值
                Fail Keep      // 失败时保持原值
                ZFail Keep     // 深度失败时保持原值
                ReadMask 255   // 读取完整字节掩码
                WriteMask 255  // 写入完整字节掩码
            }
            ZTest Always

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
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 main = tex2D(_MainTex, i.uv);
				return main;
			}
            ENDCG
        }
    }
}
