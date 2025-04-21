Shader "TerrainTessellationTools/TessellationSimple"
{
    Properties
    {
        _Tolerance("CullingTolerance", Range(0,10)) = 3
        _TessellationFactors ("Tessellation Odd Factors", Range(1,64)) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            #include "TessellationSimpleInput.cginc"

            InterpolatorsVertex VertexProgram (TessellationInterpolators v)
            {
                InterpolatorsVertex i;
                i.vertex = UnityObjectToClipPos(v.vertex);
                i.uv = v.uv;
                return i;
            }

            #include "TessellationSimple.cginc"

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
