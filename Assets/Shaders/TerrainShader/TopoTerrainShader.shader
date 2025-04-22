Shader "Custom/HeightBasedWithToonShadowsURP"
{
    Properties
    {
        _Color1 ("Base Height Color", Color) = (0, 0.5, 0, 1) 
        _Color2 ("Mid Height Color", Color) = (0.5, 0.25, 0, 1)
        _Color3 ("Upper Height Color", Color) = (0.6, 0.6, 0.6, 1) 
        _Color4 ("Top Height Color", Color) = (1, 1, 1, 1)         
        _ShadowColor ("Shadow Color", Color) = (0.2, 0.2, 0.2, 1)  
        _Height1 ("Height 1 Threshold", Float) = 2.0
        _Height2 ("Height 2 Threshold", Float) = 5.0
        _Height3 ("Height 3 Threshold", Float) = 8.0
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _ToonThreshold ("Toon Threshold", Range(0, 1)) = 0.5      

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
                float4 shadowCoord : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color1, _Color2, _Color3, _Color4, _ShadowColor;
                float _Height1, _Height2, _Height3;
                float _ShadowThreshold;
                float _ToonThreshold;
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
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.worldPos);
                OUT.uv = IN.uv * _CloudScale;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS); 
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float height = IN.worldPos.y;
                half4 baseColor;
                if (height <= _Height1) 
                    baseColor = _Color1;
                else 
                    baseColor = _Color2;
                    Light mainLight = GetMainLight(IN.shadowCoord);
                    float shadowAtten = mainLight.shadowAttenuation;
                    float NdotL = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                    half toonLighting = step(_ToonThreshold, NdotL);
                    half4 shadowedColor = (shadowAtten < _ShadowThreshold) ? lerp(baseColor, _ShadowColor, 1.0 - toonLighting) : baseColor;
                    half4 finalBase;
                    if (height > _Height3)
                    {
                        finalBase = lerp(_Color3, _Color4, toonLighting); 
                    }
                    else if (height > _Height2)
                    {
                        finalBase = lerp(shadowedColor, _Color3, toonLighting);
                    }
                    else
                    {
                        finalBase = shadowedColor;
                    }
                float2 cloudDir = normalize(float2(1.0, 1.0));
                float2 uv1 = (IN.worldPos.xz / _CloudScale) + cloudDir * _CloudSpeed1 * _Time.y;
                float2 uv2 = (IN.worldPos.xz / _CloudScale) + cloudDir * _CloudSpeed2 * _Time.y;
                float cloud1 = SAMPLE_TEXTURE2D(_CloudTex1, sampler_CloudTex1, uv1).r;
                float cloud2 = SAMPLE_TEXTURE2D(_CloudTex2, sampler_CloudTex2, uv2).r;
                float cloudValue = saturate(cloud1 * cloud2);

                half4 cloudColor = lerp(_CloudShadowColor, _CloudShadowColor2, cloudValue);
                half4 cloudAffected = (cloudValue < _CloudShadowThreshold) ? cloudColor : finalBase;
                float cloudIntensity = (height > _Height2) ? 1.0 : _CloudIntensity; 
                half4 finalColor = lerp(finalBase, cloudAffected, cloudIntensity);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}