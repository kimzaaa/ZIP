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

struct TessellationFactors {
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

struct TessellationVertexData
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct TessellationInterpolators {
	float4 vertex : INTERNALTESSPOS;
    float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float3 distanceToCamera : TEXCOORD3;
};

TessellationInterpolators TesselerVertexProgram (TessellationVertexData v)
{
    TessellationInterpolators t;
    t.vertex = v.vertex;
    t.positionCS = UnityObjectToClipPos(v.vertex);
    t.uv = v.uv;
    t.normal = v.normal;
    t.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    t.distanceToCamera = length(_WorldSpaceCameraPos - t.worldPos);
    return t;
}

#endif
