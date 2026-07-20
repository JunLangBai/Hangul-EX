Shader "Unlit/GridShader"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (1,1,1,1)
        _BackgroundColor ("Background Color", Color) = (0,0,0,1)
        _GridSize ("Grid Size", Float) = 1.0
        _GridThickness ("Grid Thickness", Range(0.0, 0.1)) = 0.01
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _GridColor;
            fixed4 _BackgroundColor;
            float _GridSize;
            float _GridThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float halfThickness = _GridThickness / 2.0;

                // 计算世界坐标下的网格线
                float lineX = abs(frac(i.worldPos.x / _GridSize) - 0.5);
                float lineZ = abs(frac(i.worldPos.z / _GridSize) - 0.5);

                // 判断像素是否在网格线上
                if (lineX < halfThickness || lineZ < halfThickness)
                {
                    return _GridColor;
                }
                else
                {
                    return _BackgroundColor;
                }
            }
            ENDCG
        }
    }
}