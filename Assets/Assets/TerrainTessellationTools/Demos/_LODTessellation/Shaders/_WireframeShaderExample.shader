Shader "TerrainTessellationTools/WireframeExample"
{
    Properties
    {
        _MinHeight("MinHeight", Range(-100, 100)) = -20
        _MaxHeight("MaxHeight", Range(-100, 100)) = 20
        _WireframeColor ("Wireframe Color", Color) = (0, 0, 0)
		_WireframeSmoothing ("Wireframe Smoothing", Range(0, 10)) = 0.5
		_WireframeThickness ("Wireframe Thickness", Range(0, 10)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram

            #include "UnityCG.cginc"

            struct VertexData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct InterpolatorsVertex
            {
                float4 vertex : SV_POSITION;    //
                float2 uv : TEXCOORD0;          // These Properties are required for Wireframe
                float3 normal : TEXCOORD1;      //
                float3 worldPos : TEXCOORD2;    //
                float3 vertexPropertyExample : TEXCOORD3; // Extra Properties must be placed in Wireframe.cginc:  InterpolatorsWireframeVertex
            };

            #define MY_DEFINITION_EXAMPLE; // Recomended use of #define and #if defined to use multiple Properties without interfering with each other. That way you can use the same #include file for all your shaders.

            float _MinHeight;
            float _MaxHeight;

            InterpolatorsVertex VertexProgram (VertexData v)
            {
                InterpolatorsVertex i;
                i.vertex = UnityObjectToClipPos(v.vertex);
                i.uv = v.uv;
                i.normal = UnityObjectToWorldNormal(v.normal);
                i.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float heightValue = (i.worldPos.y - _MinHeight) / (_MaxHeight - _MinHeight);
                i.vertexPropertyExample = clamp(heightValue, 0, 1); // Calculate your properties as usual.
                return i;
            }

            #include "WireframeExample.cginc"

            float4 FragmentProgram (InterpolatorsWireframeGeometry i) : SV_Target // You need your fragment program to receive the InterpolatorsWireframeGeometry class
            {
                float3 col = ApplyWireframe(i, i.data.vertexPropertyExample); // InterpolatorsWireframeVertex with your properties is inside the data property
                float4 colA = float4(col,1.0);
                return colA;
            }
            ENDCG
        }
    }
}
