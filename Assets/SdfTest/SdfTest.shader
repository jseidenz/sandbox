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
                float3 world_pos : TEXCOORD1;
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
                o.world_pos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            bool Raycast(float3 world_pos, float3 view_dir)
            {
                return true;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 camera_dir = normalize(i.world_pos.xyz -_WorldSpaceCameraPos.xyz);
                if (Raycast(i.world_pos.xyz, camera_dir))
                {
                    float3 light_dir = normalize(float3(1, 1, 0));
                    float3 normal = normalize(i.normal);
                    return dot(light_dir, normal) * 0.5 + 0.5;
                }
                else
                {
                    discard;
                }
                //return float4(camera_dir.xyz, 1);
                //return float4(i.world_pos, 1);
                //mul(unity_ObjectToWorld, v.vertex);

            }
            ENDCG

        }
    }
}