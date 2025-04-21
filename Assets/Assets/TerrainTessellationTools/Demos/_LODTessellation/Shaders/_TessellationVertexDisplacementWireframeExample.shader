Shader "TerrainTessellationTools/TesselationVertexDisplacementWireframeExample"
{
    Properties
    {
        _MinHeight("MinHeight", Range(-100, 100)) = -20
        _MaxHeight("MaxHeight", Range(-100, 100)) = 20

        _WireframeColor ("Wireframe Color", Color) = (0, 0, 0)
		_WireframeSmoothing ("Wireframe Smoothing", Range(0, 10)) = 0.5
		_WireframeThickness ("Wireframe Thickness", Range(0, 10)) = 0

        _Tolerance("CullingTolerance", Range(0,10)) = 3
        [Toggle(_LinearTesselation)] _LinearTesselation("_LinearTesselation", float) = 0
        [HideInInspector] _HeightMap("HeightMap", 2D) = "white" {}
        [HideInInspector] _TerrainForce("Terrain Detail Force", Float) = 0

        [CollapseCategory(Tessellation Odd Numbered Factors,First)] _TesselationFactors1 ("Tessellation Factors Min", Range(1,64)) = 1
        [CollapseCategory(Tessellation Odd Numbered Factors)] _TesselationFactors2 ("Tessellation Factors 2", Range(1,64)) = 5
        [CollapseCategory(Tessellation Odd Numbered Factors)] _TesselationFactors3 ("Tessellation Factors 3", Range(1,64)) = 9
        [CollapseCategory(Tessellation Odd Numbered Factors)] _TesselationFactors4 ("Tessellation Factors 4", Range(1,64)) = 13
        [CollapseCategory(Tessellation Odd Numbered Factors)] _TesselationFactors5 ("Tessellation Factors 5", Range(1,64)) = 17
        [CollapseCategory(Tessellation Odd Numbered Factors,Last)] _TesselationFactors6 ("Tessellation Factors Max", Range(1,64)) = 21

        [CollapseCategory(Tessellation Level Of Detail Distance To Camera,First)] _DistanceToCamera1 ("Distance To Camera Min", Float) = 25
        [CollapseCategory(Tessellation Level Of Detail Distance To Camera)] _DistanceToCamera2 ("Distance To Camera 2", Float) = 50          
        [CollapseCategory(Tessellation Level Of Detail Distance To Camera)] _DistanceToCamera3 ("Distance To Camera 3", Float) = 75         
        [CollapseCategory(Tessellation Level Of Detail Distance To Camera)] _DistanceToCamera4 ("Distance To Camera 4", Float) = 100         
        [CollapseCategory(Tessellation Level Of Detail Distance To Camera,Last)] _DistanceToCamera5 ("Distance To Camera Max", Float) = 125

        [HideInInspector] _MinX("_MinX", Float) = 0
        [HideInInspector] _MinZ("_MinZ", Float) = 0
        [HideInInspector] _MaxX("_MaxX", Float) = 0
        [HideInInspector] _MaxZ("_MaxZ", Float) = 0
        [HideInInspector] _TerrainRotation("_TerrainRotation", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            // You need to remove #pragma vertex YourVertexProgram. This is because the vertex program is made in TesselationInput.cginc. 
            // Dont worry, your vertex program will be called in the domain program. You most probably have to change the function name in the domain program to yours, inside Tessellation.cginc.
            #pragma fragment FragmentProgram
            #pragma shader_feature _LinearTesselation
            #include "UnityCG.cginc"

            struct VertexData
            {
                float4 vertex : POSITION;   //
                float2 uv : TEXCOORD0;      // These Properties are required for Tessellation
                float3 normal : NORMAL;     //
                float4 tangent : TANGENT;       // Any other properties from appdata (In this case named VertexData) required for the vertex program, must be integrated inside TesselationInputExample.cginc and TesselationExample.cginc, check inside of them.
            };

            struct InterpolatorsVertex
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 tangent : TEXCOORD2;     // This is a property from appdata, should have been added in the include files.
                float3 vertexPropertyExample : TEXCOORD3; // You can add as many properties vertex-fragment as you would like, if they just use another interpolated property, like worldPos.
            };

            #define MY_DEFINITION_EXAMPLE; // Recomended use of #define and #if defined to use multiple Properties without interfering with each other. That way you can use the same #include file for all your shaders.

            #include "TessellationVertexDisplacementInputExample.cginc"

            float _MinHeight;
            float _MaxHeight;

            InterpolatorsVertex VertexProgram (TessellationInterpolators v)
            {
                InterpolatorsVertex i;
                i.vertex = UnityObjectToClipPos(v.vertex);
                i.uv = v.uv;
                i.worldPos = v.worldPos;
                i.tangent = v.tangent; // Dont forget to get the appdata properties back for the fragment shader
                float heightValue = (i.worldPos.y - _MinHeight) / (_MaxHeight - _MinHeight);
                i.vertexPropertyExample = clamp(heightValue, 0, 1); // Calculate your properties as usual.
                return i;
            }

            #include "TessellationVertexDisplacementExample.cginc"
            #include "WireframeExample.cginc"

            float4 FragmentProgram (InterpolatorsWireframeGeometry i) : SV_Target
            {
                float3 col = ApplyWireframe(i, i.data.vertexPropertyExample); // InterpolatorsWireframeVertex with your properties is inside the data property
                //Uncomment bellow for proper appdata property integration verification
                //col = col+i.tangent;
                float4 colA = float4(col,1.0);
                return colA;
            }
            ENDCG
        }
    }
}
