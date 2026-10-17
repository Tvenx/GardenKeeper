Shader "Custom/Sharpen_Unsharp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Amount ("Amount", Range(0,2)) = 0.5
        _BlurSize ("Blur Size", Range(0.5,100)) = 1
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
            float _BlurSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy * _BlurSize;

                float3 c = tex2D(_MainTex, i.uv).rgb;
                float3 blur =
                    tex2D(_MainTex, i.uv + t).rgb +
                    tex2D(_MainTex, i.uv - t).rgb +
                    tex2D(_MainTex, i.uv + float2(t.x, -t.y)).rgb +
                    tex2D(_MainTex, i.uv + float2(-t.x, t.y)).rgb;

                blur *= 0.25;

                float3 sharpen = c + (c - blur) * _Amount;
                return float4(sharpen, 1);
            }
            ENDCG
        }
    }
}
