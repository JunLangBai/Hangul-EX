Shader "Custom/ObjectPaint"
{
   Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _DrawTex ("Draw Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        sampler2D _MainTex;
        sampler2D _DrawTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseCol = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 drawCol = tex2D(_DrawTex, IN.uv_MainTex);
            o.Albedo = lerp(baseCol.rgb, drawCol.rgb, drawCol.a);
            o.Alpha = 1;
        }
        ENDCG
    }
}
