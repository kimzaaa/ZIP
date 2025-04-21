Shader "TerrainTessellationTools/DisplayTangents" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t {
                float4 vertex : POSITION;
                float3 tangent : TANGENT;
            };
            
            struct v2f {
                float3 tangent : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.tangent = v.tangent;
                return o;
            }
            
            half4 frag (v2f i) : SV_Target {
                return half4(i.tangent * 0.5 + 0.5, 1);
            }
            ENDCG
        }
    }
}
