Shader "Hidden/RedPreservePostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RedThreshold ("Red Threshold", Range(0, 1)) = 0.1
        _RedSoftness ("Red Softness", Range(0.01, 0.5)) = 0.1
    }
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
            float _RedThreshold;
            float _RedSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // ������� �������� RGB � HSV
            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // 1. ������� ��� �����-������
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));

                // 2. ��������� � HSV
                float3 hsv = RGBtoHSV(col.rgb);
                float hue = hsv.x * 360.0; // ������� � �������� (0-360)
                float sat = hsv.y;
                float val = hsv.z;

                // 3. Select hues close to red, wrapping around 0/360 degrees.
                float distToRed = min(hue, 360.0 - hue);
                float hueMask = 1.0 - smoothstep(15.0, 20.0, distToRed);
                float satMask = smoothstep(_RedThreshold, _RedThreshold + _RedSoftness, sat);

                float redMask = hueMask * satMask;

                // 4. ���������: ������� ��������, ��������� � �����
                fixed3 finalColor = lerp(fixed3(gray, gray, gray), col.rgb, redMask);

                return fixed4(finalColor, col.a);
            }
            ENDCG
        }
    }
}