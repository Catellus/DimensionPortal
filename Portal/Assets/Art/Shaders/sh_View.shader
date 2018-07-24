Shader "Unlit/View"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityStandardUtils.cginc"

            struct VertIn
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct FragIn
            {
                float2 uv : TEXCOORD0;
                float3 screen_uv : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            FragIn vert (VertIn v)
            {
                FragIn o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.vertex.z = 0;
                o.uv = v.uv;
                o.screen_uv = float3((o.vertex.xy + o.vertex.w) * 0.5, o.vertex.w);

                return o;
            }

            fixed4 frag (FragIn i) : SV_Target
            {
                // sample the texture
                float2 screen_uv = i.screen_uv.xy / i.screen_uv.z;
                fixed4 col = tex2D(_MainTex, screen_uv);
                return col;
            }
            ENDCG
        }
    }
}
