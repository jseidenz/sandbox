Shader "SftTest" 
{
    SubShader
    {
        Pass 
        {

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 light_dir = normalize(float3(1, 1, 0));
                float3 normal = normalize(i.normal);
                return dot(light_dir, normal) * 0.5 + 0.5;
            }
            ENDCG

        }
    }
}