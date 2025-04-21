Shader "ProceduralGrass/Grass"
{
       Properties
    {
        [Header(Grass Properties)]
        _BaseMap("Albedo Texture", 2D) = "white" {}
        _TaperAmount("Taper Tip Amount", Range(0.0, 1.0)) = 0
        _TaperInverseAmount("Taper Base Amount", Range(0.0, 1.0)) = 0
        _BlendSurfaceNormalDistLower("Normal Blending Distance Min", Float) = 10
        _BlendSurfaceNormalDistUpper("Normal Blending Distance Max", Float) = 20

        [Header(Wind)]
        [Toggle(_StaticWind)] _StaticWind("Static Wind", Float) = 0
        _WindAmplitude("Wind Amplitude", Float) = 2
        _WindSpeed("Wind Speed", Float) = 100
        _WindPower("Wind Power", Range(0.0, 1.0)) = 1
        _SinOffset("Wind Sin Offset", Range(0.0, 3.0)) = 1
        _PushTipOscillationForward("Tip Forward", Float) = 1

        [Header(Illumination)]
        [Toggle(_Unlit)] _Unlit("_Unlit", Float) = 0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _CurvedNormalDegrees("Grass Normal Curving Degrees", Float) = 0
        _AmbientStrength("Ambient Strength", Float) = 1
        _ColorAlbedoStrength("Color Albedo Strength", Range(0.0, 1.0)) = 0.9
        [ToggleOff] _SpecularHighlights("Regular Specular Highlights", Float) = 0.0
        [Toggle(_WhiteAmbient)] _WhiteAmbient("White Ambient", Float) = 1
        [Toggle(_ExtraSpecular)] _ExtraSpecular("Extra Specular", Float) = 1
        [Toggle(_ExtraSpecularMonoColor)] _ExtraSpecularMonoColor("Extra Specular Single Color", Float) = 0
        _ExtraSpecStrength("Extra Specular Strength", Float) = 3
        _ExtraSpecColor("Extra Specular Color", Color) = (1, 1, 1)
        _ExtraSpecMap("Extra Specular Texture", 2D) = "white" {}

        _RenderFaces("Face Rendering", Float) = 0.0 // TODO UI

        [Toggle] _ReceiveShadows("Receive Shadows", Float) = 1.0 // TODO

        [HideInInspector] _GrassBladesPerPoint ("Grass Blades Per Point", Int) = 1
        [HideInInspector] _VerticesPerBlade ("Vertices Per Blade", Int) = 0
        [HideInInspector] _Exponent ("Vertices Per Blade", Int) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull[_RenderFaces]

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma shader_feature _Unlit
            #pragma shader_feature _WhiteAmbient
            #pragma shader_feature _ExtraSpecular
            #pragma shader_feature _ExtraSpecularMonoColor
            #pragma shader_feature _StaticWind

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float3 worldPos : TEXCOORD4;
            };

            struct GrassBlade {
                float3 position;
                float height;
                float width;
                float rotationAngle;
                float rotationAngleCurrent;
                float hash;
                float tilt;
                float bend;
                float windStrength;
                float3 surfaceNorm;
                float clumpBaseToSecondaryRatio;
            };
            struct GrassInteractorPoint {
                float3 position; 
                float radius;
                float force;
            };
            StructuredBuffer<GrassBlade> _GrassBlades;
            StructuredBuffer<GrassInteractorPoint> _GrassInteractorPoints;
            StructuredBuffer<float4> _DefaultColors;
            StructuredBuffer<float4> _SecondaryColors;
            uint _GrassBladesPerPoint;
            uint _VerticesPerBlade;
            uint _GrassSegments;

            float _WindAmplitude;
            float _WindSpeed;
            float _WindPower;
            float _SinOffset;
            float _PushTipOscillationForward;
            float _TaperAmount;
            float _TaperInverseAmount;
            float _BlendSurfaceNormalDistUpper;
            float _BlendSurfaceNormalDistLower;
            float4 _ExtraSpecColor;
            float _Metallic;
            float _Smoothness;
            float _AmbientStrength;
            float _ExtraSpecStrength;
            float _CurvedNormalDegrees;
            float _Exponent;
            float _ColorAlbedoStrength;
            float _MaxInteractors;

            float3 _WSpaceCameraPos;
            sampler2D _BaseMap;
            sampler2D _ExtraSpecMap;

            float3x3 AngleAxis3x3(float angle, float3 axis) //Returns a Rotation matrix of a angle in a axis.
	        {
		        float c, s;
		        sincos(angle, s, c);

		        float t = 1 - c;
		        float x = axis.x;
		        float y = axis.y;
		        float z = axis.z;

		        return float3x3(
			        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
			        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
			        t * x * z - s * y, t * y * z + s * x, t * z * z + c
			        );
	        }

            float3 cubicBezier(float3 p0, float3 p1, float3 p2, float3 p3, float t ){ // Returns the position of a point of a bezier curve t (0-1 of the curve) of 4 points.
                float3 a = lerp(p0, p1, t);
                float3 b = lerp(p2, p3, t);
                float3 c = lerp(p1, p2, t);
                float3 d = lerp(a, c, t);
                float3 e = lerp(c, b, t);
                return lerp(d,e,t); 
            }
            float3 bezierTangent(float3 p0, float3 p1, float3 p2, float3 p3, float t ){
            
                float omt = 1-t;
                float omt2 = omt*omt;
                float t2= t*t;

                float3 tangent = 
                    p0* (-omt2) +
                    p1 * (3 * omt2 - 2 *omt) +
                    p2 * (-3 * t2 + 2 * t) +
                    p3 * (t2);
                     
                return normalize(tangent);
            }
            v2f vert(uint svVertexID: SV_VertexID, uint svInstanceID : SV_InstanceID) // asumes only one command. error otherwise.
            {
                // Init Blade
                InitIndirectDrawArgs(0); //this might be useless, was in documentation
                v2f o;
                float3 pos;
                uint bladeIndex = svInstanceID * _GrassBladesPerPoint + (svVertexID/_VerticesPerBlade);
                uint vertexBladeIndex = svVertexID % _VerticesPerBlade;
                GrassBlade blade = _GrassBlades[bladeIndex];

                // Calculate Exponential vertex height
                float linearValue = (vertexBladeIndex/2)/(float)(_GrassSegments);
                float exponentialValue = pow(linearValue, _Exponent);
                float vertexHeight0_1 = exponentialValue;
                


                //Calculate Bezier Points
                float tilt = blade.tilt;
                bool continueProcessing = true;

                // Tilt depending on interactors            
                for (int i = 0; i < _MaxInteractors && continueProcessing; ++i) {
                    GrassInteractorPoint interactor = _GrassInteractorPoints[i];

                    // Check if the interactor has a non-zero radius
                    if (interactor.radius > 0.0) {
                        float distance = length(blade.position - interactor.position);

                        if (distance <= interactor.radius) {
                            // Calculate a linear falloff factor based on distance
                            float falloffFactor = 1.0 - (distance / interactor.radius);

                            // Apply the adjusted force based on falloff
                            tilt += interactor.force * falloffFactor;
                        }
                    } else {
                        // If radius is 0, set continueProcessing to false to break out of the loop
                        continueProcessing = false;
                    }
                }
                if (tilt >= 1) tilt = 1;
                float height = blade.height;
                float bend = blade.bend;
                float p3x = tilt * height;
                float p3y = height - tilt * height;
                float3 p3 =  float3(p3x, p3y, 0);



                float2 bladeDir = normalize(p3);
                float3 bezCtrlOffsetDir = normalize(float3(-bladeDir.y, bladeDir.x,0)); // Direction of bend, based on tilt.

                float3 p0 = float3(0,0,0); // blade origin
                float3 p1 = 0.33 * p3; // same direction as p3, smaller values
                float3 p2 = 0.66 * p3; // same direction as p3, smaller values

                p1 += bezCtrlOffsetDir * bend; // Might Add Flexibility, so p1 and p2 have different bends.
                p2 += bezCtrlOffsetDir * bend;

                float p1Weight = 0.33;
                float p2Weight = 0.66;
                float p3Weight = 1;

                float hash= blade.hash;
                float mult = 1-bend;
                
                float windStrength = blade.windStrength;

                #ifdef _StaticWind
                    float p1ffset = pow(p1Weight,_WindPower)*(_WindAmplitude/100) * sin((_Time+hash*2*3.1415)*_WindSpeed +p1Weight*2*3.1415*_SinOffset); 
                    float p2ffset = pow(p2Weight,_WindPower)*(_WindAmplitude/100) * sin((_Time+hash*2*3.1415)*_WindSpeed +p2Weight*2*3.1415*_SinOffset); 
                    float p3ffset = pow(p3Weight,_WindPower)*(_WindAmplitude/100) * sin((_Time+hash*2*3.1415)*_WindSpeed +p3Weight*2*3.1415*_SinOffset); 
                #else
                    float p1ffset = pow(p1Weight,_WindPower)*(_WindAmplitude/100) * sin((_Time+hash*2*3.1415)*_WindSpeed +p1Weight*2*3.1415*_SinOffset) * windStrength; 
                    float p2ffset = pow(p2Weight,_WindPower)*(_WindAmplitude/100) * sin((_Time+hash*2*3.1415)*_WindSpeed +p2Weight*2*3.1415*_SinOffset) * windStrength; 
                    float p3ffset = pow(p3Weight,_WindPower)*(_WindAmplitude/100) * sin((_Time+hash*2*3.1415)*_WindSpeed +p3Weight*2*3.1415*_SinOffset) * windStrength; 
                #endif

                p3ffset = (p3ffset) -  _PushTipOscillationForward*mult*(pow(p3Weight,_WindPower)*_WindAmplitude/100)/2;
                p1 += bezCtrlOffsetDir*  p1ffset;
                p2 += bezCtrlOffsetDir*  p2ffset;
                p3 += bezCtrlOffsetDir*  p3ffset;

                float width = (blade.width) * (1-_TaperAmount*vertexHeight0_1);
                width = width * (1 - _TaperInverseAmount * (1 - vertexHeight0_1));

                float3 vertexPos = cubicBezier(p0,p1,p2,p3,vertexHeight0_1);
                pos = vertexPos;
                if (vertexBladeIndex % 2 == 0){
                    pos.z += width/2;
                }else{
                    pos.z -= width/2;
                }
                o.uv = float2((vertexBladeIndex+1) % 2,vertexHeight0_1);
                if(vertexBladeIndex == _VerticesPerBlade-1){
                    pos.z = 0;
                    o.uv.x = 0.5f;
                }

                float3x3 rotMatrix = AngleAxis3x3(radians(blade.rotationAngleCurrent), float3(0,1,0));
                float3x3 rotMatrixNormalRight = AngleAxis3x3(radians(-blade.rotationAngleCurrent)+_CurvedNormalDegrees, float3(0,1,0));
                float3x3 rotMatrixNormalLeft = AngleAxis3x3(radians(-blade.rotationAngleCurrent)-_CurvedNormalDegrees, float3(0,1,0));
                pos = mul(rotMatrix, pos);
                pos += blade.position;
                o.worldPos = pos;
                o.pos = mul(UNITY_MATRIX_VP, float4(pos,1));
                o.color = lerp(_DefaultColors[vertexBladeIndex/2],_SecondaryColors[vertexBladeIndex/2],blade.clumpBaseToSecondaryRatio);

                float3 tangent = normalize(bezierTangent(p0, p1,p2,p3, vertexHeight0_1));
                float3 curvedNormal = normalize(cross(tangent, float3(0,0,-1))) ;      
                float3 surfaceNorm = blade.surfaceNorm;

                if (vertexBladeIndex % 2 == 0){
                    curvedNormal = mul(curvedNormal,rotMatrixNormalRight);
                }else{
                    curvedNormal = mul(curvedNormal,rotMatrixNormalLeft);
                }
                float distToCam = distance(pos, _WSpaceCameraPos);
                float surfaceNormalBlendSmoothstep = smoothstep(_BlendSurfaceNormalDistLower,_BlendSurfaceNormalDistUpper, distToCam);
                o.normal = normalize(lerp(curvedNormal, surfaceNorm,surfaceNormalBlendSmoothstep));

                return o;
            }

            float4 frag(v2f i, float facing : VFACE) : SV_Target
            {
                #ifdef _Unlit
                    return i.color * tex2D(_BaseMap, i.uv);
                #endif
                InputData inputData;

                inputData.positionWS = i.worldPos;
                inputData.positionCS = i.pos;
                inputData.normalWS = i.normal;
                inputData.viewDirectionWS = normalize(_WSpaceCameraPos - i.worldPos);
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                inputData.fogCoord = 0.0;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = half3(0, 0, 0);
                inputData.normalizedScreenSpaceUV = float2(0, 0);
                inputData.shadowMask = half4(0, 0, 0, 0);
                inputData.tangentToWorld = half3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);

                SurfaceData surfaceData;

                surfaceData.albedo =  lerp(float4(1, 1, 1, 1), i.color, _ColorAlbedoStrength) * tex2D(_BaseMap, i.uv);
                surfaceData.specular = float4(1,1,1,1);
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = 0.0;
                surfaceData.alpha = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                if(facing == 0){
                    inputData.normalWS = -inputData.normalWS;
                }
                float4 ambientColor = float4(0,0,0,1);
                #ifdef _WhiteAmbient
                    ambientColor.rgb = max(unity_SHAr.w, max(unity_SHAg.w,unity_SHAb.w));
                #else
                    ambientColor = half4(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w,1);
                #endif
                ambientColor = ambientColor * i.color * tex2D(_BaseMap, i.uv) * _AmbientStrength;
                #ifdef _ExtraSpecular
                    float gloss = tex2D(_ExtraSpecMap, i.uv);
                    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
                    Light mainLight = GetMainLight(inputData, inputData.shadowMask, aoFactor);
                    float3 l = normalize(mainLight.direction);
                    float3 r = normalize(reflect(-l,i.normal));
                    float3 r2 = normalize(reflect(-l,-i.normal));
                    float3 v = normalize(_WSpaceCameraPos - i.worldPos);
                    float shininess = lerp(0, 1, gloss);
                    float spec = saturate(dot(r,v)) * shininess * _ExtraSpecStrength;
                    float4 specColor = float4(0,0,0,1);
                    #ifdef _ExtraSpecularMonoColor
                        specColor = (spec * _ExtraSpecColor * tex2D(_BaseMap, i.uv));
                    #else
                        specColor = (spec * i.color * tex2D(_BaseMap, i.uv));
                    #endif
                    half4 finalColor =  max(UniversalFragmentPBR(inputData, surfaceData),ambientColor);
                    if (any(finalColor.rgb != ambientColor.rgb)) {
                        finalColor += finalColor * specColor;
                    }
                    return finalColor;
                #endif
                return max(UniversalFragmentPBR(inputData, surfaceData),ambientColor);
            }
            ENDHLSL
        }
    }
}