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
                Ref [_StencilRef]          // ����Ϊ1��ֵ
                Comp Always    // ����ͨ��ģ�����
                Pass Replace   // �òο�ֵ�滻������ֵ
                Fail Keep      // ʧ��ʱ����ԭֵ
                ZFail Keep     // ���ʧ��ʱ����ԭֵ
                ReadMask 255   // ��ȡ�����ֽ�����
                WriteMask 255  // д�������ֽ�����
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
