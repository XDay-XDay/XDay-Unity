Shader "XDay/Texture"
{
	Properties
	{
		_MainTex("Main Tex", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 100

		Pass
		{
			ZTest LEqual

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
			fixed4 _Color;

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
				return main * _Color;
			}
			ENDCG
		}
	}
}