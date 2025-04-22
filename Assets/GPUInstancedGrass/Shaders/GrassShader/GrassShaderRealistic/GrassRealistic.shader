Shader "Custom/GrassShaderInstancedWithShadowsAndSwayAndClouds"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}                
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _ShadowColor ("Shadow Color", Color) = (0.2, 0.2, 0.2, 1)  
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _SwaySpeed ("Sway Speed", Float) = 1.0
        _SwayAmplitude ("Sway Amplitude", Float) = 0.1
        _SwayFrequency ("Sway Frequency", Float) = 1.0

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
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CloudTex1); SAMPLER(sampler_CloudTex1);
            TEXTURE2D(_CloudTex2); SAMPLER(sampler_CloudTex2);

            float4 _MainTex_ST;
            float _Cutoff;
            float4 _ShadowColor;
            float _ShadowThreshold;
            float _SwaySpeed;
            float _SwayAmplitude;
            float _SwayFrequency;

            float _CloudSpeed1, _CloudSpeed2;
            float _CloudScale;
            float _CloudIntensity;
            float _CloudShadowThreshold;
            float4 _CloudShadowColor;
            float4 _CloudShadowColor2;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceData) 
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionOS = input.positionOS.xyz;
                float time = _Time.y * _SwaySpeed;
                float sway = sin(time + positionOS.x * _SwayFrequency + positionOS.z * _SwayFrequency) * _SwayAmplitude;
                positionOS.x += sway;
                positionOS.z += sway * 0.5;

                float3 positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1.0)).xyz;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 grassColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(grassColor.a - _Cutoff);

                Light mainLight = GetMainLight(input.shadowCoord);
                float shadowAtten = mainLight.shadowAttenuation;
                half4 finalBase = (shadowAtten < _ShadowThreshold) ? _ShadowColor : grassColor;

                float2 cloudDir = normalize(float2(1.0, 1.0));
                float2 uv1 = (input.positionWS.xz / _CloudScale) + cloudDir * _CloudSpeed1 * _Time.y;
                float2 uv2 = (input.positionWS.xz / _CloudScale) + cloudDir * _CloudSpeed2 * _Time.y;
                float cloud1 = SAMPLE_TEXTURE2D(_CloudTex1, sampler_CloudTex1, uv1).r;
                float cloud2 = SAMPLE_TEXTURE2D(_CloudTex2, sampler_CloudTex2, uv2).r;
                float cloudValue = saturate(cloud1 * cloud2);

                half4 cloudColor = lerp(_CloudShadowColor, _CloudShadowColor2, cloudValue);
                half4 cloudAffected = (cloudValue < _CloudShadowThreshold) ? cloudColor : finalBase;
                half4 finalColor = lerp(finalBase, cloudAffected, _CloudIntensity);

                return half4(finalColor.rgb, grassColor.a);
            }
            ENDHLSL
        }
    }
}