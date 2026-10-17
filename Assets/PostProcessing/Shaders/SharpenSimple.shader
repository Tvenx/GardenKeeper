Shader "Custom/SharpenSimple"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Amount ("Amount", Range(0,2)) = 0.5
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

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy;

                float3 c = tex2D(_MainTex, i.uv).rgb;
                float3 n = tex2D(_MainTex, i.uv + float2(0, t.y)).rgb;
                float3 s = tex2D(_MainTex, i.uv - float2(0, t.y)).rgb;
                float3 e = tex2D(_MainTex, i.uv + float2(t.x, 0)).rgb;
                float3 w = tex2D(_MainTex, i.uv - float2(t.x, 0)).rgb;

                float3 edge = (n + s + e + w - 4 * c);
                return float4(c - edge * _Amount, 1);
            }
            ENDCG
        }
    }
}
