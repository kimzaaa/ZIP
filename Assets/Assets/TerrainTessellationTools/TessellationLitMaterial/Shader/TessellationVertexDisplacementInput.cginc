// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#if !defined(Tessellation_INPUT_INCLUDED)
#define Tessellation_INPUT_INCLUDED

#pragma vertex TesselerVertexProgram

float _Tolerance;

float _TessellationFactors1;
float _TessellationFactors2;
float _TessellationFactors3;
float _TessellationFactors4;
float _TessellationFactors5;
float _TessellationFactors6;

float _DistanceToCamera1;
float _DistanceToCamera2;
float _DistanceToCamera3;
float _DistanceToCamera4;
float _DistanceToCamera5;

sampler2D _HeightMap;

float _MinX;
float _MinZ;
float _MaxX;
float _MaxZ;
float _TerrainRotation;
float _TerrainForce;

struct TessellationFactors {
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

struct TessellationVertexData
{
    float4 vertex : POSITION;
    #ifdef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
        float3 normalOS     : NORMAL;
        float4 tangentOS    : TANGENT;
        float2 texcoord     : TEXCOORD0;
        float2 staticLightmapUV   : TEXCOORD1;
        float2 dynamicLightmapUV  : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
    #ifdef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
        float3 normalOS     : NORMAL;
        float2 texcoord     : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
    #ifdef UNIVERSAL_LIT_GBUFFER_PASS_INCLUDED
        float3 normalOS     : NORMAL;
        float4 tangentOS    : TANGENT;
        float2 texcoord     : TEXCOORD0;
        float2 staticLightmapUV   : TEXCOORD1;
        float2 dynamicLightmapUV  : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
    #ifdef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
        float2 texcoord     : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
    #ifdef UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED
        float4 tangentOS      : TANGENT;
        float2 texcoord     : TEXCOORD0;
        float3 normal       : NORMAL;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
};

struct TessellationInterpolators {
    float4 vertex : INTERNALTESSPOS;
    float4 positionCS : SV_POSITION;
    float3 worldPos : TEXCOORD8;
    float3 distanceToCamera : TEXCOORD9;
    #ifdef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
        float3 normalOS     : NORMAL;
        float4 tangentOS    : TANGENT;
        float2 texcoord     : TEXCOORD0;
        float2 staticLightmapUV   : TEXCOORD1;
        float2 dynamicLightmapUV  : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
    #ifdef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
        float3 normalOS     : NORMAL;
        float2 texcoord     : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
    #ifdef UNIVERSAL_LIT_GBUFFER_PASS_INCLUDED
        float3 normalOS     : NORMAL;
        float4 tangentOS    : TANGENT;
        float2 texcoord     : TEXCOORD0;
        float2 staticLightmapUV   : TEXCOORD1;
        float2 dynamicLightmapUV  : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
    #ifdef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
        float2 texcoord     : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
    #ifdef UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED
        float4 tangentOS      : TANGENT;
        float2 texcoord     : TEXCOORD0;
        float3 normal       : NORMAL;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
};

float4 UnityObjectToClipPos( in float3 pos )
{
	return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
}

TessellationInterpolators TesselerVertexProgram (TessellationVertexData v)
{
    TessellationInterpolators t;
    t.vertex = v.vertex;
    t.positionCS = UnityObjectToClipPos(v.vertex);
    t.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    t.distanceToCamera = length(_WorldSpaceCameraPos - t.worldPos);
    #ifdef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
        t.normalOS = v.normalOS;
        t.tangentOS = v.tangentOS;
        t.texcoord = v.texcoord;
        t.staticLightmapUV = v.staticLightmapUV;
        t.dynamicLightmapUV = v.dynamicLightmapUV;
    #endif
    #ifdef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
        t.normalOS = v.normalOS;
        t.texcoord = v.texcoord;
    #endif
    #ifdef UNIVERSAL_LIT_GBUFFER_PASS_INCLUDED
        t.normalOS = v.normalOS;
        t.tangentOS = v.tangentOS;
        t.texcoord = v.texcoord;
        t.staticLightmapUV = v.staticLightmapUV;
        t.dynamicLightmapUV = v.dynamicLightmapUV;
    #endif
    #ifdef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
        t.texcoord = v.texcoord;
    #endif
    #ifdef UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED
        t.tangentOS = v.tangentOS;
        t.texcoord = v.texcoord;
        t.normal = v.normal;
    #endif
    return t;
}

#endif
