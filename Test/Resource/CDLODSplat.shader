Shader "Ground/CDLODSplat"
{
    Properties
    {
        _SplatMask ("Mask Texture", 2D) = "white" {}
        _Layer0 ("Layer 0", 2D) = "white" {}
        _Layer1 ("Layer 1", 2D) = "white" {}
        _Layer2 ("Layer 2", 2D) = "white" {}
        _Layer3 ("Layer 3", 2D) = "white" {}

        _HeightMap ("Height Map", 2D) = "white" {}
        _MapSize ("Map Size", Vector) = (0,0,0,0)
        //x = node width, y = node height, z = end / (end-start), w = 1.0f / (end-start)
        _MorphParameters ("Morph Parameters", Vector) = (0,0,0,0)
        // x = meshGridResolution (32), y = meshGridResolution / 2, z = 2/meshGridResolution
        _MeshParameters ("Mesh Parameters", Vector) = (0,0,0,0)
        //x = tile width, y = tile height, z = tile position.x, w = tile.position.y
        _TileBounds ("Tile Bounds", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _SplatMask;
            sampler2D _Layer0;
            sampler2D _Layer1;
            sampler2D _Layer2;
            sampler2D _Layer3;
            sampler2D_float _HeightMap;

            //global parameter
            float4 _CameraWorldPos;

            CBUFFER_START(UnityPerMaterial)
            float4 _Layer0_ST;
            float4 _Layer1_ST;
            float4 _Layer2_ST;
            float4 _Layer3_ST;
            float4 _MapSize;
            float4 _MeshParameters;
            float4 _MorphParameters;
            float4 _TileBounds;
            CBUFFER_END

            float getMorphValue(float distance)
            {
                //z = end / (end-start), w = 1.0f / (end-start)
                float vv = _MorphParameters.z - distance * _MorphParameters.w;
                float morphLerpK  = 1.0f - clamp( vv, 0.0, 1.0 );   
                return morphLerpK ; 
            }

            float2 morphVertex( float4 objectSpacePos, float2 worldPos, float morphLerpK )
            {
                float2 fracPart = frac(objectSpacePos.xz * _MeshParameters.yy) * _MeshParameters.zz * _MorphParameters.xy;
                return worldPos.xy - fracPart * morphLerpK;
            }

            v2f vert (appdata v)
            {
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                worldPos.xz = min( worldPos.xz, _MapSize.xy );

                float texU = worldPos.x / _MapSize.x;
                float texV = worldPos.z / _MapSize.y;

                float4 heightData = tex2Dlod(_HeightMap, float4(texU, texV, 0, 0));
                
                //先采样一次高度图计算顶点与相机的距离
                worldPos.y = heightData.x;
                float cameraToVertexDistance = distance( worldPos.xyz, _CameraWorldPos.xyz);
                float morphLerpK = getMorphValue(cameraToVertexDistance);
                
                worldPos.xz = morphVertex(v.vertex, worldPos.xz, morphLerpK);

                //再计算最终顶点坐标
                texU = worldPos.x / _MapSize.x;
                texV = worldPos.z / _MapSize.y;
                heightData = tex2Dlod(_HeightMap, float4(texU, texV, 0, 0));
                worldPos.y = heightData.x;

                v2f o;
                o.vertex = UnityWorldToClipPos(worldPos);

                float2 localPosition = worldPos.xz - _TileBounds.zw;
                o.uv = float2(localPosition.x / _TileBounds.x, localPosition.y / _TileBounds.y);

                o.uv1.xy = TRANSFORM_TEX(o.uv, _Layer0);
                o.uv1.zw = TRANSFORM_TEX(o.uv, _Layer1);
                o.uv2.xy = TRANSFORM_TEX(o.uv, _Layer2);
                o.uv2.zw = TRANSFORM_TEX(o.uv, _Layer3);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_SplatMask, i.uv);
                fixed4 splat0 = tex2D(_Layer0, i.uv1.xy);
                fixed4 splat1 = tex2D(_Layer1, i.uv1.zw);
                fixed4 splat2 = tex2D(_Layer2, i.uv2.xy);
                fixed4 splat3 = tex2D(_Layer3, i.uv2.zw);

                fixed4 col = mask.r * splat0 + mask.g * splat1 + mask.b * splat2 + mask.a * splat3;
                return col;
            }
            ENDCG
        }
    }
}
