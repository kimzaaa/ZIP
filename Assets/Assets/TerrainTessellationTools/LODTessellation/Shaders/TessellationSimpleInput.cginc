#if !defined(Tessellation_INPUT_INCLUDED)
#define Tessellation_INPUT_INCLUDED

#pragma vertex TesselerVertexProgram

float _Tolerance;

float _TessellationFactors;

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
};

TessellationInterpolators TesselerVertexProgram (TessellationVertexData v)
{
    TessellationInterpolators t;
    t.vertex = v.vertex;
    t.positionCS = UnityObjectToClipPos(v.vertex);
    t.uv = v.uv;
    t.normal = v.normal;
    return t;
}

#endif