#if !defined(Tessellation_INCLUDED) && defined(Tessellation_INPUT_INCLUDED)
#define Tessellation_INCLUDED

#pragma hull HullProgram
#pragma domain DomainProgram

bool IsOutOfBounds(float3 p, float3 lower, float3 higher) {
    return p.x < lower.x || p.x > higher.x || p.y < lower.y || p.y > higher.y || p.z < lower.z || p.z > higher.z;
}

bool IsPointOutOfFrustum(float4 positionCS, float tolerance) {
    float3 culling = positionCS.xyz;
    float w = positionCS.w;
    float3 lowerBounds = float3(-w - tolerance, -w - tolerance, -w - tolerance);
    float3 higherBounds = float3(w + tolerance, w + tolerance, w + tolerance);
    return IsOutOfBounds(culling, lowerBounds, higherBounds);
}

bool ShouldClipPatch(float4 p0PositionCS, float4 p1PositionCS, float4 p2PositionCS) {
    bool allOutside =   IsPointOutOfFrustum(p0PositionCS,_Tolerance) &&
                        IsPointOutOfFrustum(p1PositionCS,_Tolerance) &&
                        IsPointOutOfFrustum(p2PositionCS,_Tolerance);
    return allOutside;
}

TessellationFactors TessellationPatchConstant (InputPatch<TessellationInterpolators, 3> patch) {
	TessellationFactors f;
    if (ShouldClipPatch(patch[0].positionCS, patch[1].positionCS, patch[2].positionCS)){
        f.edge[0] = 0;
        f.edge[1] = 0;
        f.edge[2] = 0;
	    f.inside = 0;
    } else {
        f.edge[0] = _TessellationFactors;
        f.edge[1] = _TessellationFactors;
        f.edge[2] = _TessellationFactors;
	    f.inside = _TessellationFactors;
    }
	return f;
}

[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("fractional_odd")]
[UNITY_patchconstantfunc("TessellationPatchConstant")]
TessellationInterpolators  HullProgram(InputPatch<TessellationInterpolators, 3> patch, uint id : SV_OutputControlPointID){
	return patch[id];
}

[UNITY_domain("tri")]
InterpolatorsVertex  DomainProgram (TessellationFactors factors, OutputPatch<TessellationInterpolators, 3> patch, float3 barycentricCoordinates : SV_DomainLocation) {
   	TessellationInterpolators data;
	#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) data.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

	MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
	MY_DOMAIN_PROGRAM_INTERPOLATE(uv)

	return VertexProgram(data);
}

#endif