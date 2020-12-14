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
                float2 uv : TEXCOORD2;
            };

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.world_pos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.uv;
                return o;
            }

            bool SphereHit(float3 world_pos, float iso_dist)
            {
                return distance(world_pos, float3(0, 0, 0)) < iso_dist;
            }

            bool Raycast(float3 pos, float3 dir, float iso_dist)
            {
                #define STEPS 64
                #define STEP_SIZE 0.05

                for (int i = 0; i < STEPS; i++)
                {
                    if (SphereHit(pos, iso_dist))
                    {
                        return true;
                    }

                    pos += dir * STEP_SIZE;
                }

                return false;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 camera_dir = normalize(i.world_pos.xyz -_WorldSpaceCameraPos.xyz);
                float iso_dist = i.uv.x;
                if (Raycast(i.world_pos.xyz, camera_dir, iso_dist))
                {
                    float3 light_dir = normalize(float3(1, 1, 0));
                    float3 normal = normalize(i.normal);
                    return dot(light_dir, normal) * 0.5 + 0.5;
                }
                else
                {
                    return float4(1, 0, 0, 1);
                }
                //return float4(camera_dir.xyz, 1);
                //return float4(i.world_pos, 1);
                //mul(unity_ObjectToWorld, v.vertex);

            }
            ENDCG

        }
    }
}