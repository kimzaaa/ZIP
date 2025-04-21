#if !defined(WIREFRAME_INCLUDED)
#define WIREFRAME_INCLUDED

#pragma geometry GeometryProgram

float3 _WireframeColor;
float _WireframeSmoothing;
float _WireframeThickness;


struct InterpolatorsWireframeVertex // Shaderlab will cast your Vertex Program class to this one, they just need the same parameters. You could use your own directly, but this way multiple shaders can use this.
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;

	// Your VertexProgram Properties Here. Example:
	#if defined(MY_DEFINITION_EXAMPLE)
	float3 vertexPropertyExample : TEXCOORD3;
	#endif
	//
};

struct InterpolatorsWireframeGeometry {
	InterpolatorsWireframeVertex data;
	float3 barycentricCoordinates : TEXCOORD9;
};

float3 ApplyWireframe (InterpolatorsWireframeGeometry i, float3 albedo) {
	float3 barys;
	barys = i.barycentricCoordinates;
	float3 deltas = fwidth(barys);
	float3 smoothing = deltas * _WireframeSmoothing;
	float3 thickness = deltas * _WireframeThickness;
	barys = smoothstep(thickness, thickness + smoothing, barys);
	float minBary = min(barys.x, min(barys.y, barys.z));
	return lerp(_WireframeColor, albedo, minBary);
}

[maxvertexcount(3)]
void GeometryProgram (triangle InterpolatorsWireframeVertex i[3], inout TriangleStream<InterpolatorsWireframeGeometry> stream) {
    float3 p0 = i[0].worldPos.xyz;
	float3 p1 = i[1].worldPos.xyz;
	float3 p2 = i[2].worldPos.xyz;

	float3 triangleNormal = normalize(cross(p1 - p0, p2 - p0));
	i[0].normal = triangleNormal;
	i[1].normal = triangleNormal;
	i[2].normal = triangleNormal;

	InterpolatorsWireframeGeometry g0, g1, g2;
	g0.data = i[0];
	g1.data = i[1];
	g2.data = i[2];
	g0.barycentricCoordinates = float3(1, 0, 0);
	g1.barycentricCoordinates = float3(0, 1, 0);
	g2.barycentricCoordinates = float3(0, 0, 1);

	stream.Append(g0);
	stream.Append(g1);
	stream.Append(g2);
}

#endif