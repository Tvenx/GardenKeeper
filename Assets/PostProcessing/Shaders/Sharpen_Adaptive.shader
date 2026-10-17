Shader "Custom/Sharpen_Adaptive"
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
                float3 blur =
                    tex2D(_MainTex, i.uv + t).rgb +
                    tex2D(_MainTex, i.uv - t).rgb +
                    tex2D(_MainTex, i.uv + float2(t.x, -t.y)).rgb +
                    tex2D(_MainTex, i.uv + float2(-t.x, t.y)).rgb;

                blur *= 0.25;

                float edge = length(c - blur);
                float mask = smoothstep(0.02, 0.2, edge);

                float3 sharpen = c + (c - blur) * mask * _Amount;
                return float4(sharpen, 1);
            }
            ENDCG
        }
    }
}
