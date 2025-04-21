Shader "TerrainTessellationTools/TessellationVertexDisplacement"
{
    Properties
    {
        _Tolerance("CullingTolerance", Range(0,10)) = 3
        [Toggle(_LinearTessellation)] _LinearTessellation("_LinearTessellation", float) = 0
        [HideInInspector] _HeightMap("HeightMap", 2D) = "white" {}
        [HideInInspector] _TerrainForce("Terrain Detail Force", Float) = 0

        [CollapseCategory(Tessellation Odd Numbered Factors,First)] _TessellationFactors1 ("Tessellation Factors Min", Range(1,64)) = 1
        [CollapseCategory(Tessellation Odd Numbered Factors)] _TessellationFactors2 ("Tessellation Factors 2", Range(1,64)) = 5
        [CollapseCategory(Tessellation Odd Numbered Factors)] _TessellationFactors3 ("Tessellation Factors 3", Range(1,64)) = 9
        [CollapseCategory(Tessellation Odd Numbered Factors)] _TessellationFactors4 ("Tessellation Factors 4", Range(1,64)) = 13
        [CollapseCategory(Tessellation Odd Numbered Factors)] _TessellationFactors5 ("Tessellation Factors 5", Range(1,64)) = 17
        [CollapseCategory(Tessellation Odd Numbered Factors,Last)] _TessellationFactors6 ("Tessellation Factors Max", Range(1,64)) = 21

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
            #pragma fragment FragmentProgram
            #pragma shader_feature _LinearTessellation
            #include "UnityCG.cginc"

            struct VertexData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct InterpolatorsVertex
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            #include "TessellationVertexDisplacementInput.cginc"

            InterpolatorsVertex VertexProgram (TessellationInterpolators v)
            {
                InterpolatorsVertex i;
                i.vertex = UnityObjectToClipPos(v.vertex);
                i.uv = v.uv;
                return i;
            }

            #include "TessellationVertexDisplacement.cginc"

            float4 FragmentProgram (InterpolatorsVertex i) : SV_Target
            {
                float3 col = float3(0,0,0);
                float4 colA = float4(col,1.0);
                return colA;
            }
            ENDCG
        }
    }
}
