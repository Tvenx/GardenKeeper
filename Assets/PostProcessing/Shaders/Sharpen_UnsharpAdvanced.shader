Shader "Custom/Sharpen_UnsharpAdvanced"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Amount ("Amount", Range(0,2)) = 0.5
        _Radius ("Radius", Range(0.5,3)) = 1
        _Threshold ("Threshold", Range(0,0.2)) = 0.05
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
            float _Radius;
            float _Threshold;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy * _Radius;

                float3 c = tex2D(_MainTex, i.uv).rgb;
                float3 blur =
                    tex2D(_MainTex, i.uv + t).rgb +
                    tex2D(_MainTex, i.uv - t).rgb +
                    tex2D(_MainTex, i.uv + float2(t.x, -t.y)).rgb +
                    tex2D(_MainTex, i.uv + float2(-t.x, t.y)).rgb;

                blur *= 0.25;

                float3 diff = c - blur;
                float mask = step(_Threshold, length(diff));

                return float4(c + diff * mask * _Amount, 1);
            }
            ENDCG
        }
    }
}
