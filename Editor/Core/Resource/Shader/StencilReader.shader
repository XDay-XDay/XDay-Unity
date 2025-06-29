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
                Ref [_StencilRef]          // ����Ϊ1��ֵ
                Comp Equal    // ����ͨ��ģ�����
                Pass Keep   // �òο�ֵ�滻������ֵ
                Fail Keep      // ʧ��ʱ����ԭֵ
                ZFail Keep     // ���ʧ��ʱ����ԭֵ
                ReadMask 255   // ��ȡ�����ֽ�����
                WriteMask 255  // д�������ֽ�����
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
