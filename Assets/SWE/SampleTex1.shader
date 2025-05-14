Shader "Custom/DisplacedSampleTex"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Displacement ("Displacement Strength", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Displacement;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            float getHeight(float2 uv)
            {
                float H = tex2Dlod(_MainTex, float4(uv, 0, 0)).r;
                float h = tex2Dlod(_MainTex, float4(uv, 0, 0)).g;
                return H + h;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv = uv;

                float height = getHeight(uv);
                float3 displacedPos = v.vertex.xyz + float3(0, 1, 0) * height * _Displacement;

                // Approximate normal from height map gradient
                float eps = 0.01;

                float heightX1 = getHeight(uv + float2(eps, 0));
                float heightX2 = getHeight(uv - float2(eps, 0));
                float heightY1 = getHeight(uv + float2(0, eps));
                float heightY2 = getHeight(uv - float2(0, eps));

                float3 dx = float3(2 * eps, (heightX1 - heightX2) * _Displacement, 0);
                float3 dy = float3(0, (heightY1 - heightY2) * _Displacement, 2 * eps);
                float3 normal = normalize(cross(dy, dx));

                o.normal = normal;
                o.pos = UnityObjectToClipPos(float4(displacedPos, 1.0));
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, i.uv);
                half4 color = half4(texColor.x,texColor.x,texColor.x,1.0);

                if(texColor.y >0.001)
                {
                   color = half4(i.normal * 0.5 + 0.5, 1.0);
                }
                return color;
            }
            ENDCG
        }
    }
}
