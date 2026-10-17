Shader "Hidden/Custom/Sharpen"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    ENDHLSL

    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _Amount;

            float4 Frag(VaryingsDefault i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy;

                float3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).rgb;
                float3 n = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + float2(0, t.y)).rgb;
                float3 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord - float2(0, t.y)).rgb;
                float3 e = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + float2(t.x, 0)).rgb;
                float3 w = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord - float2(t.x, 0)).rgb;

                float3 edge = (n + s + e + w - 4 * c);
                return float4(c - edge * _Amount, 1);
            }
            ENDHLSL
        }
    }
}
