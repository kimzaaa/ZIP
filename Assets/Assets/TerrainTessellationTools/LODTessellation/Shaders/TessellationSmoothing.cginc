#if !defined(Tessellation_INCLUDED) && defined(Tessellation_INPUT_INCLUDED)
#define Tessellation_INCLUDED

#pragma hull HullProgram
#pragma domain DomainProgram

float CalculateTessellation(float distanceToCamera) {
    if (distanceToCamera <= _DistanceToCamera1)         return _TessellationFactors6;
    else if (distanceToCamera <= _DistanceToCamera2)    return _TessellationFactors5;
    else if (distanceToCamera <= _DistanceToCamera3)    return _TessellationFactors4;
    else if (distanceToCamera <= _DistanceToCamera4)    return _TessellationFactors3;
    else if (distanceToCamera <= _DistanceToCamera5)    return _TessellationFactors2;
    else                                                return _TessellationFactors1;
}

float TessellationEdgeFactor (TessellationInterpolators t0, TessellationInterpolators t1) {
    #ifdef _LinearTessellation
        float distanceToCamera = max(t0.distanceToCamera , t1.distanceToCamera);
        float tessellationFactor = lerp(_TessellationFactors6, _TessellationFactors1, saturate((distanceToCamera - _DistanceToCamera1) / (_DistanceToCamera5 - _DistanceToCamera1)));
        return tessellationFactor;
    #else
	    return CalculateTessellation(max(t0.distanceToCamera , t1.distanceToCamera));
    #endif
}

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
        f.edge[0] = TessellationEdgeFactor(patch[1], patch[2]);
        f.edge[1] = TessellationEdgeFactor(patch[2], patch[0]);
        f.edge[2] = TessellationEdgeFactor(patch[0], patch[1]);
	    f.inside = max(max(f.edge[0], f.edge[1]), f.edge[2]);
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

float3 PhongProjectedPosition(float3 flatPositionWS, float3 cornerPositionWS, float3 normalWS) {
    return flatPositionWS - dot(flatPositionWS - cornerPositionWS, normalWS) * normalWS;
}

float3 BarycentricInterpolate(float3 bary, float3 p0PositionWS, float3 p1PositionWS, float3 p2PositionWS)
{
    float3 interpolatedPositionWS = bary.x * p0PositionWS + bary.y * p1PositionWS + bary.z * p2PositionWS;
    return interpolatedPositionWS;
}

float3 CalculatePhongPosition(float3 bary, float smoothing, float3 p0PositionWS, float3 p0NormalWS,
    float3 p1PositionWS, float3 p1NormalWS, float3 p2PositionWS, float3 p2NormalWS) {
    float3 flatPositionWS = BarycentricInterpolate(bary, p0PositionWS, p1PositionWS, p2PositionWS);
    float3 smoothedPositionWS =
        bary.x * PhongProjectedPosition(flatPositionWS, p0PositionWS, p0NormalWS) +
        bary.y * PhongProjectedPosition(flatPositionWS, p1PositionWS, p1NormalWS) +
        bary.z * PhongProjectedPosition(flatPositionWS, p2PositionWS, p2NormalWS);
    return lerp(flatPositionWS, smoothedPositionWS, smoothing);
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
	MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
	MY_DOMAIN_PROGRAM_INTERPOLATE(worldPos)
    data.vertex.xyz = CalculatePhongPosition(barycentricCoordinates,_Smoothing, patch[0].vertex, patch[0].normal,patch[1].vertex, patch[1].normal,patch[2].vertex, patch[2].normal);

	return VertexProgram(data);
}

#endif