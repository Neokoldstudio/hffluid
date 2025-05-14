Shader "Custom/DisplacedSampleTex"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Displacement ("Displacement Strength", Range(0, 1)) = 0.1
        _SurfaceColor ("Surface Color", Color) = (0.3, 0.5, 1, 1)
        _SpecularColor ("Specular Color", Color) = (0.3, 0.5, 1, 1)
        _Shininess ("Shininess", Range(1, 128)) = 64
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "RenderType" = "Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Material properties
            CBUFFER_START(UnityPerMaterial)
                float _Displacement;
                float4 _SurfaceColor;
                float4 _SpecularColor;
                float _Shininess;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            float getHeight(float2 uv)
            {
                float H = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).r;
                float h = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).g;
                return H + h;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float2 uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uv = uv;

                float height = getHeight(uv);
                float3 displacedPosOS = IN.positionOS.xyz + float3(0, 1, 0) * height * _Displacement;

                // Approximate normal
                float eps = 0.01;
                float heightX1 = getHeight(uv + float2(eps, 0));
                float heightX2 = getHeight(uv - float2(eps, 0));
                float heightY1 = getHeight(uv + float2(0, eps));
                float heightY2 = getHeight(uv - float2(0, eps));

                float3 dx = float3(2 * eps, (heightX1 - heightX2) * _Displacement, 0);
                float3 dy = float3(0, (heightY1 - heightY2) * _Displacement, 2 * eps);
                float3 normalOS = normalize(cross(dy, dx));

                OUT.normalWS = TransformObjectToWorldNormal(normalOS);
                OUT.positionWS = TransformObjectToWorld(displacedPosOS);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 color = float4(texColor.r, texColor.r, texColor.r, 1.0);

                if (texColor.g > 0.001)
                {
                    // Lighting
                    Light mainLight = GetMainLight();

                    float3 N = normalize(IN.normalWS);
                    float3 L = normalize(mainLight.direction);
                    float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                    float3 H = normalize(L + V);

                    float diff = saturate(dot(N, L));
                    float3 diffuseColor = _SurfaceColor.rgb * diff; // bluish diffuse

                    // Specular term
                    float spec = pow(saturate(dot(N, H)), _Shininess);
                    float3 specularColor = _SpecularColor.rgb * spec;

                    color.rgb = (diffuseColor + specularColor) * mainLight.color;
                }

                return color;
            }

            ENDHLSL
        }
    }
}
