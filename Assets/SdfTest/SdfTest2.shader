Shader "SftTest2" 
{
    SubShader
    {
        Pass
        {

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler3D _LiquidTex;
            float3 _WorldSizeInMeters;
            struct RaymarchResult
            {
                bool m_hit_surface;
                float m_distance;
            };
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

            float SphereDistance(float3 world_pos)
            {
                #define STEP_SIZE 0.02

                float3 world_uv = (world_pos / _WorldSizeInMeters) * 0.5 + 0.5;
                if (world_uv.x > 1 || world_uv.x < 0) return STEP_SIZE;
                if (world_uv.y > 1 || world_uv.y < 0) return STEP_SIZE;
                if (world_uv.z > 1 || world_uv.z < 0) return STEP_SIZE;
                float distance = tex3D(_LiquidTex, world_uv).r;
                distance = distance * STEP_SIZE;
                return distance;
            }

            RaymarchResult RayMarch(float3 pos, float3 dir)
            {
                #define MIN_DISTANCE 0.01
                #define STEPS 128                

                float3 original_pos = pos;
                for (int i = 0; i < STEPS; i++)
                {
                    float distance = SphereDistance(pos);
                    if (distance < MIN_DISTANCE)
                    {
                        RaymarchResult result;
                        result.m_hit_surface = true;
                        result.m_distance = length(pos - original_pos);
                        return result;
                    }

                    pos += dir * distance;
                }


                RaymarchResult result;
                result.m_hit_surface = false;
                return result;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //float3 world_uv = (i.world_pos.xyz / _WorldSizeInMeters.xyz) * 0.5 + 0.5;
                //float radius = tex3D(_LiquidTex, world_uv).r;
                //return float4(radius.xxx, 1);

                float3 camera_dir = normalize(i.world_pos.xyz -_WorldSpaceCameraPos.xyz);
                RaymarchResult result = RayMarch(i.world_pos.xyz, camera_dir);
                if (result.m_hit_surface)
                {
                    float3 light_dir = normalize(float3(1, 1, 0));
                    float3 normal = normalize(i.normal);
                    return result.m_distance / 4;// dot(light_dir, normal) * 0.5 + 0.5;
                }
                else
                {
                    discard;
                    return float4(1, 0, 0, 1);
                }
            }
            ENDCG

        }
    }
}