Shader "Custom/ToonShaderWithCloudShadowsURP"
{
    Properties
    {
        _Color1 ("Base Lit Color", Color) = (0, 0.5, 0, 1)
        _Color2 ("Mid Lit Color", Color) = (0.5, 0.25, 0, 1)
        _Color3 ("Low Lit Color", Color) = (0.6, 0.6, 0.6, 1)
        _Color4 ("Shadowed Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Toon Shadow Color", Color) = (0.2, 0.2, 0.2, 1)
        _LightThreshold1 ("Light Threshold 1", Range(0, 1)) = 0.8
        _LightThreshold2 ("Light Threshold 2", Range(0, 1)) = 0.5
        _LightThreshold3 ("Light Threshold 3", Range(0, 1)) = 0.2
        _ShadowThreshold ("Toon Shadow Threshold", Range(0, 1)) = 0.5

        // Cloud properties
        _CloudTex1 ("Cloud Texture 1", 2D) = "white" {}
        _CloudTex2 ("Cloud Texture 2", 2D) = "white" {}
        _CloudSpeed1 ("Cloud Speed 1", Float) = 0.1
        _CloudSpeed2 ("Cloud Speed 2", Float) = 0.05
        _CloudScale ("Cloud Scale", Float) = 1.0
        _CloudIntensity ("Cloud Shadow Intensity", Range(0, 1)) = 0.5
        _CloudShadowThreshold ("Cloud Shadow Toon Threshold", Range(0, 1)) = 0.5
        _CloudShadowColor ("Cloud Shadow Toon Color 1", Color) = (0.5, 0.5, 0.5, 1)
        _CloudShadowColor2 ("Cloud Shadow Toon Color 2", Color) = (0.3, 0.3, 0.4, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color1, _Color2, _Color3, _Color4, _ShadowColor;
                float _LightThreshold1, _LightThreshold2, _LightThreshold3;
                float _ShadowThreshold;
                float4 _CloudTex1_ST;
                float4 _CloudTex2_ST;
                float _CloudSpeed1, _CloudSpeed2;
                float _CloudScale;
                float _CloudIntensity;
                float _CloudShadowThreshold;
                float4 _CloudShadowColor;
                float4 _CloudShadowColor2;
            CBUFFER_END

            TEXTURE2D(_CloudTex1); SAMPLER(sampler_CloudTex1);
            TEXTURE2D(_CloudTex2); SAMPLER(sampler_CloudTex2);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.worldPos);
                OUT.uv = IN.uv * _CloudScale;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Normalize normal
                float3 normalWS = normalize(IN.normalWS);

                // Get main light
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);

                // Calculate NdotL for toon shading
                float NdotL = max(0, dot(normalWS, lightDir));
                float lightIntensity = NdotL * mainLight.distanceAttenuation;

                // Base color based on light intensity (toon banding)
                half4 baseColor;
                if (lightIntensity >= _LightThreshold1) baseColor = _Color1;
                else if (lightIntensity >= _LightThreshold2) baseColor = _Color2;
                else if (lightIntensity >= _LightThreshold3) baseColor = _Color3;
                else baseColor = _Color4;

                // Cloud shadows
                float2 cloudDir = float2(1.0, 1.0);
                float2 uv1 = IN.uv + normalize(cloudDir) * _CloudSpeed1 * _Time.y;
                float2 uv2 = IN.uv + normalize(cloudDir) * _CloudSpeed2 * _Time.y;
                float cloud1 = SAMPLE_TEXTURE2D(_CloudTex1, sampler_CloudTex1, uv1).r;
                float cloud2 = SAMPLE_TEXTURE2D(_CloudTex2, sampler_CloudTex2, uv2).r;
                float cloudValue = saturate(cloud1 * cloud2);

                // Apply cloud toon effect
                half4 cloudColor = lerp(_CloudShadowColor, _CloudShadowColor2, cloudValue);
                half4 cloudAffected = (cloudValue < _CloudShadowThreshold) ? cloudColor : baseColor;

                // Blend cloud effect with base color
                float lightIntensityFactor = (lightIntensity > _LightThreshold2) ? 1.0 : 0.5;
                half4 finalBase = lerp(baseColor, cloudAffected, lightIntensityFactor * _CloudIntensity);

                // Main shadow calculation for toon effect
                float shadowAtten = mainLight.shadowAttenuation;

                // Apply toon shadow
                half4 finalColor;
                if (shadowAtten < _ShadowThreshold || NdotL < _LightThreshold3)
                {
                    if (lightIntensity <= _LightThreshold2)
                    {
                        // Blend shadow color with base color at 0.5 intensity for mid/low light
                        finalColor = lerp(finalBase, _ShadowColor, 0.5);
                    }
                    else
                    {
                        // Full shadow intensity for very low light or shadowed areas
                        finalColor = _ShadowColor;
                    }
                }
                else
                {
                    finalColor = finalBase;
                }

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}