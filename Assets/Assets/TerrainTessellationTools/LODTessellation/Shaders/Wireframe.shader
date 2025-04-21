Shader "TerrainTessellationTools/Wireframe"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1)
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            InterpolatorsVertex VertexProgram (VertexData v)
            {
                InterpolatorsVertex i;
                i.vertex = UnityObjectToClipPos(v.vertex);
                i.uv = v.uv;
                i.normal = UnityObjectToWorldNormal(v.normal);
                i.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return i;
            }

            #include "Wireframe.cginc"

            float3 _Color;

            float4 FragmentProgram (InterpolatorsWireframeGeometry i) : SV_Target
            {
                float3 col =ApplyWireframe(i, _Color);
                float4 colA = float4(col,1.0);
                return colA;
            }
            ENDCG
        }
    }
}
