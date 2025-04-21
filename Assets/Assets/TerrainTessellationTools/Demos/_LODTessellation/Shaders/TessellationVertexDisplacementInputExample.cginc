#if !defined(TESSELATION_INPUT_INCLUDED)
#define TESSELATION_INPUT_INCLUDED

#pragma vertex TesselerVertexProgram

float _Tolerance;

float _TesselationFactors1;
float _TesselationFactors2;
float _TesselationFactors3;
float _TesselationFactors4;
float _TesselationFactors5;
float _TesselationFactors6;

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

struct TesselationVertexData // Shaderlab will cast your appdata class to this one, they just need the same parameters. You could use your own directly, but this way multiple shaders can use this.
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;

	// Your appdata Properties Here. Example:
	#if defined(MY_DEFINITION_EXAMPLE)
	float4 tangent : TANGENT; 
	#endif
    //
};

struct TessellationInterpolators { // Same as above, need to be done twice since appdata and Vertex Program output are different classes.
	float4 vertex : INTERNALTESSPOS;
    float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float3 distanceToCamera : TEXCOORD3;

	// Your appdata Properties Here. Example:
	#if defined(MY_DEFINITION_EXAMPLE)
	float4 tangent : TEXCOORD4; 
	#endif
    //
};

TessellationInterpolators TesselerVertexProgram (TesselationVertexData v)
{
    TessellationInterpolators t;
    t.vertex = v.vertex;
    t.positionCS = UnityObjectToClipPos(v.vertex);
    t.uv = v.uv;
    t.normal = v.normal;
    t.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    t.distanceToCamera = length(_WorldSpaceCameraPos - t.worldPos);
    // Dont forget to also add them to the Vertex Program Example:
	#if defined(MY_DEFINITION_EXAMPLE)
	t.tangent = v.tangent;
	#endif
    //
    return t;
}

#endif
