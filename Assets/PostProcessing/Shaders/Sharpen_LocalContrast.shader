Shader "Custom/Sharpen_LocalContrast"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Amount ("Amount", Range(0,1)) = 0.3
        _Radius ("Radius", Range(0.5,100)) = 1
        _Brightness ("Brightness Compensation", Range(0,1)) = 0.08
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
            float _Brightness;

            float Luma(float3 c)
            {
                return dot(c, float3(0.2126, 0.7152, 0.0722));
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy * _Radius;

                float3 c = tex2D(_MainTex, i.uv).rgb;

                float l0 = Luma(c);
                float l1 = Luma(tex2D(_MainTex, i.uv + t).rgb);
                float l2 = Luma(tex2D(_MainTex, i.uv - t).rgb);
                float l3 = Luma(tex2D(_MainTex, i.uv + float2(t.x, -t.y)).rgb);
                float l4 = Luma(tex2D(_MainTex, i.uv + float2(-t.x, t.y)).rgb);

                float blur = (l1 + l2 + l3 + l4) * 0.25;
                float detail = l0 - blur;

                float3 result = c + detail * _Amount;

                // компенсация потери яркости
                result += _Brightness;

                return float4(saturate(result), 1);
            }
            ENDCG
        }
    }
}
