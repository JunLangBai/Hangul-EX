Shader "Hidden/BrushBlit"
{
    Properties { }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BrushTex;
            float4 _Coord; // (u,v,size,size)
            fixed4 _Color;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float2 diff = abs(i.uv - _Coord.xy);
                float mask = step(diff.x, _Coord.z / 2) * step(diff.y, _Coord.w / 2);

                if (mask > 0)
                {
                    float2 brushUV = (i.uv - (_Coord.xy - _Coord.zw / 2)) / _Coord.zw;
                    fixed4 brush = tex2D(_BrushTex, brushUV) * _Color;
                    col = lerp(col, brush, brush.a);
                }

                return col;
            }
            ENDCG
        }
    }
}
