Shader "Custom/Sharpen_UnsharpDual"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Amount ("Amount", Range(0,2)) = 0.5
        _SmallRadius ("Small Radius", Range(0.5,2)) = 0.8
        _LargeRadius ("Large Radius", Range(1,4)) = 2
    }

    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Amount;
            float _SmallRadius;
            float _LargeRadius;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;

                float2 t1 = _MainTex_TexelSize.xy * _SmallRadius;
                float2 t2 = _MainTex_TexelSize.xy * _LargeRadius;

                float3 blurSmall =
                    tex2D(_MainTex, i.uv + t1).rgb +
                    tex2D(_MainTex, i.uv - t1).rgb;
                blurSmall *= 0.5;

                float3 blurLarge =
                    tex2D(_MainTex, i.uv + t2).rgb +
                    tex2D(_MainTex, i.uv - t2).rgb;
                blurLarge *= 0.5;

                float3 detail = (c - blurSmall) * 0.7 + (c - blurLarge) * 0.3;
                return float4(c + detail * _Amount, 1);
            }
            ENDCG
        }
    }
}
