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

[domain("tri")]
[outputcontrolpoints(3)]
[outputtopology("triangle_cw")]
[partitioning("fractional_odd")]
[patchconstantfunc("TessellationPatchConstant")]
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

Varyings Output(TessellationInterpolators input)
{
    Attributes output;

    #ifdef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
        output.positionOS = input.vertex;
        output.normalOS = input.normalOS;
        output.tangentOS = input.tangentOS;
        output.texcoord = input.texcoord;
        output.staticLightmapUV = input.staticLightmapUV;
        output.dynamicLightmapUV = input.dynamicLightmapUV;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        return LitPassVertex(output);
    #endif
    #ifdef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
        output.positionOS = input.vertex;
        output.normalOS = input.normalOS;
        output.texcoord = input.texcoord;
        return ShadowPassVertex(output);
    #endif
    #ifdef UNIVERSAL_LIT_GBUFFER_PASS_INCLUDED
        output.positionOS = input.vertex;
        output.normalOS = input.normalOS;
        output.tangentOS = input.tangentOS;
        output.texcoord = input.texcoord;
        output.staticLightmapUV = input.staticLightmapUV;
        output.dynamicLightmapUV = input.dynamicLightmapUV;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        return LitGBufferPassVertex(output);
    #endif
    #ifdef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
        output.position = input.vertex;
        output.texcoord = input.texcoord;
        return DepthOnlyVertex(output);
    #endif
    #ifdef UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED
        output.positionOS = input.vertex;
        output.tangentOS = input.tangentOS;
        output.texcoord = input.texcoord;
        output.normal = input.normal;
        return DepthNormalsVertex(output);
    #endif
}

[domain("tri")]
Varyings  DomainProgram (TessellationFactors factors, OutputPatch<TessellationInterpolators, 3> patch, float3 barycentricCoordinates : SV_DomainLocation) {
   	TessellationInterpolators data;
	#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) data.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

    MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
	MY_DOMAIN_PROGRAM_INTERPOLATE(worldPos)
	MY_DOMAIN_PROGRAM_INTERPOLATE(positionCS)
    #ifdef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
        MY_DOMAIN_PROGRAM_INTERPOLATE(normalOS)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(tangentOS)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(texcoord)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(staticLightmapUV)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(dynamicLightmapUV)
    #endif
    #ifdef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
	    MY_DOMAIN_PROGRAM_INTERPOLATE(normalOS)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(texcoord)
    #endif
    #ifdef UNIVERSAL_LIT_GBUFFER_PASS_INCLUDED
        MY_DOMAIN_PROGRAM_INTERPOLATE(normalOS)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(tangentOS)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(texcoord)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(staticLightmapUV)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(dynamicLightmapUV)
    #endif
    #ifdef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
	    MY_DOMAIN_PROGRAM_INTERPOLATE(texcoord)
    #endif
    #ifdef UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED
	    MY_DOMAIN_PROGRAM_INTERPOLATE(tangentOS)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(texcoord)
	    MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
    #endif

    float2 minWorldPos = float2(_MinX, _MinZ);
    float2 maxWorldPos = float2(_MaxX, _MaxZ);
    float2 birdViewUV = (data.worldPos.xz - minWorldPos) / (maxWorldPos - minWorldPos);
    birdViewUV = RotateUV(birdViewUV, _TerrainRotation);
    birdViewUV = saturate(birdViewUV);
    float height = tex2Dlod(_HeightMap, float4(birdViewUV.x,birdViewUV.y,0,0));
    data.vertex.y = height * _TerrainForce;
	return Output(data);
}

#endif