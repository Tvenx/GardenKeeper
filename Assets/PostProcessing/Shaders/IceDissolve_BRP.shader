Shader "Custom/IceDissolve_BRP"
{
    Properties
    {
        _MainTex ("Ice Texture", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "white" {}

        _MeltAmount ("Melt", Range(0,1)) = 0
        _EdgeWidth ("Edge Width", Range(0,0.3)) = 0.08
        _EdgeColor ("Edge Color", Color) = (0.6,0.9,1,1)

        _Glossiness ("Smoothness", Range(0,1)) = 0.9
        _VerticalBias ("Vertical Melt", Range(0,2)) = 0

        _Opacity ("Opacity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        CGPROGRAM
        #pragma surface surf Standard alpha:fade

        sampler2D _MainTex;
        sampler2D _NoiseTex;

        float _MeltAmount;
        float _EdgeWidth;
        float4 _EdgeColor;
        float _Glossiness;
        float _VerticalBias;
        float _Opacity;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float noise = tex2D(_NoiseTex, IN.uv_NoiseTex).r;

            // вертикальное таяние (сверху вниз)
            noise -= IN.uv_MainTex.y * _VerticalBias;

            float melt = _MeltAmount;

            float edge0 = melt;
            float edge1 = melt + _EdgeWidth;

            float alpha = smoothstep(edge0, edge1, noise);

            float edge =
                smoothstep(edge0, edge0 + _EdgeWidth, noise) -
                smoothstep(edge1, edge1 + _EdgeWidth, noise);

            fixed4 baseCol = tex2D(_MainTex, IN.uv_MainTex);

            o.Albedo = lerp(baseCol.rgb, _EdgeColor.rgb, edge);
            o.Alpha = baseCol.a * alpha * _Opacity;
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
        }
        ENDCG
    }

    FallBack "Transparent/Diffuse"
}