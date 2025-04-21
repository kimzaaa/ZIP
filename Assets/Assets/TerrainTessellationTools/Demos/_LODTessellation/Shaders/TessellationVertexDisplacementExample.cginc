#if !defined(TESSELATION_INCLUDED) && defined(TESSELATION_INPUT_INCLUDED)
#define TESSELATION_INCLUDED

#pragma hull HullProgram
#pragma domain DomainProgram

float CalculateTesselation(float distanceToCamera) {
    if (distanceToCamera <= _DistanceToCamera1)         return _TesselationFactors6;
    else if (distanceToCamera <= _DistanceToCamera2)    return _TesselationFactors5;
    else if (distanceToCamera <= _DistanceToCamera3)    return _TesselationFactors4;
    else if (distanceToCamera <= _DistanceToCamera4)    return _TesselationFactors3;
    else if (distanceToCamera <= _DistanceToCamera5)    return _TesselationFactors2;
    else                                                return _TesselationFactors1;
}

float TessellationEdgeFactor (TessellationInterpolators t0, TessellationInterpolators t1) {
    #ifdef _LinearTesselation
        float distanceToCamera = max(t0.distanceToCamera , t1.distanceToCamera);
        float tessellationFactor = lerp(_TesselationFactors6, _TesselationFactors1, saturate((distanceToCamera - _DistanceToCamera1) / (_DistanceToCamera5 - _DistanceToCamera1)));
        return tessellationFactor;
    #else
	    return CalculateTesselation(max(t0.distanceToCamera , t1.distanceToCamera));
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

TessellationFactors TesselationPatchConstant (InputPatch<TessellationInterpolators, 3> patch) {
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
[UNITY_patchconstantfunc("TesselationPatchConstant")]
TessellationInterpolators  HullProgram(InputPatch<TessellationInterpolators, 3> patch, uint id : SV_OutputControlPointID){
	return patch[id];
}

float2 RotateUV(float2 uv, float angleDegrees) {
    float angleRadians = radians(angleDegrees);
    float cosA = cos(angleRadians);
    float sinA = sin(angleRadians);
    uv -= 0.5;
    return float2(uv.x * cosA - uv.y * sinA, uv.x * sinA + uv.y * cosA) + 0.5;
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

    // Your appdata Properties Here. They need to be interpolated like above for accurate new values after tesselation. Example:
	#if defined(MY_DEFINITION_EXAMPLE)
	MY_DOMAIN_PROGRAM_INTERPOLATE(tangent)
	#endif
    //

    float2 minWorldPos = float2(_MinX, _MinZ);
    float2 maxWorldPos = float2(_MaxX, _MaxZ);
    float2 birdViewUV = (data.worldPos.xz - minWorldPos) / (maxWorldPos - minWorldPos);
    birdViewUV = RotateUV(birdViewUV, _TerrainRotation);
    birdViewUV = saturate(birdViewUV);
    float height = tex2Dlod(_HeightMap, float4(birdViewUV.x,birdViewUV.y,0,0));
    data.vertex.y = height * _TerrainForce;
	return VertexProgram(data);
}

#endif