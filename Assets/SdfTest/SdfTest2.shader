﻿Shader "SdfTest2" 
{
    SubShader
    {
        Pass
        {

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float m_step_size;
            float m_min_distance;
            float m_cell_radius;
            float3 m_max_density;


            sampler3D _LiquidTex;
            float3 _WorldSizeInMeters;
            float3 _WorldSizeInCells;
            struct RaymarchResult
            {
                bool m_hit_surface;
                float m_distance;
                float3 m_normal;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 world_pos : TEXCOORD1;                
                float2 uv : TEXCOORD2;
                float3 view_dir : TEXCOORD3;
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
                o.view_dir = normalize(o.world_pos.xyz - _WorldSpaceCameraPos.xyz);
                return o;
            }

            float BoxDistance2(float3 field_pos)
            {
                float3 field_uv = field_pos / _WorldSizeInCells;
                float normalized_density = tex3D(_LiquidTex, field_uv).r;
                float3 density = float3(normalized_density * m_max_density.x, normalized_density * m_max_density.y, normalized_density * m_max_density.z);


                float3 center = floor(field_pos) + float3(0.5, 0.5, 0.5);
                float3 local_pos = field_pos - center;
                float3 abs_local_pos = abs(local_pos);
                float3 delta = float3(abs_local_pos.x - density.x, abs_local_pos.y - density.y, abs_local_pos.z - density.z);
                float3 max_delta = max(delta, float3(0, 0, 0));
                float max_delta_comp = max(max(delta.x, delta.y), delta.z);
                float penetration = min(max_delta_comp, 0);
                return length(max_delta) + penetration;
            }

            float BoxDistance(float3 field_pos)
            {
                float3 field_uv = field_pos / _WorldSizeInCells;
                float normalized_density = tex3D(_LiquidTex, field_uv).r;
                float3 density = float3(normalized_density * m_max_density.x, normalized_density * m_max_density.y, normalized_density * m_max_density.z);
                

                float3 center = floor(field_pos) + float3(0.5, 0.5, 0.5);
                float3 local_pos = field_pos - center;                
                float3 abs_local_pos = abs(local_pos);                
                float3 delta = float3(abs_local_pos.x - density.x, abs_local_pos.y - density.y, abs_local_pos.z - density.z);
                float3 max_delta = max(delta, float3(0, 0, 0));
                float max_delta_comp = max(max(delta.x, delta.y), delta.z);
                float penetration = min(max_delta_comp, 0);
                return length(max_delta) + penetration;
            }

            float SphereDistance(float3 field_pos)
            {
                float3 field_uv = field_pos / (_WorldSizeInMeters / (m_cell_radius * 2));
                float distance = tex3D(_LiquidTex, field_uv).r;

                float3 corner = floor(field_pos);
                float3 center = corner + float3(0.5, 0.5, 0.5);

                float3 delta = field_pos - center;
                return 1 - (1 - (length(delta) - 0.5)) * (1 - distance);
                return length(delta)-0.6;
                
                //return distance;

                /*
                float3 world_uv = world_pos / _WorldSizeInMeters;

                float distance = tex3D(_LiquidTex, world_uv).r;
                distance = distance * m_step_size;

                float3 center = float3(((int)(world_pos.x / m_cell_radius)) * m_cell_radius + m_cell_radius, ((int)(world_pos.y / m_cell_radius)) * m_cell_radius + m_cell_radius, ((int)(world_pos.z / m_cell_radius))* m_cell_radius + m_cell_radius);
                float3 delta = float3(world_pos.x - center.x, world_pos.y - center.y, world_pos.z - center.z);
                return length(delta);
                float radius_mult = max(length(delta) / m_cell_radius, 1);
                distance = distance * radius_mult;

                return distance;
                
                float3 center = float3((int)world_pos.x + m_cell_radius, (int)world_pos.y + m_cell_radius, (int)world_pos.z + m_cell_radius);
                float3 delta = float3(world_pos.x - center.x, world_pos.y - center.y, world_pos.z - center.z);
                float density = tex3D(_LiquidTex, world_uv).r;                
                float distance = max((length(delta) / m_cell_radius), 1)  * density;
                return distance;
                */
            }

            float3 CalcNormal(float3 world_pos)
            {
                const float eps = 0.001;

                float delta_x = BoxDistance(world_pos + float3(eps, 0, 0)) - BoxDistance(world_pos - float3(eps, 0, 0));
                float delta_y = BoxDistance(world_pos + float3(0, eps, 0)) - BoxDistance(world_pos - float3(0, eps, 0));
                float delta_z = BoxDistance(world_pos + float3(0, 0, eps)) - BoxDistance(world_pos - float3(0, 0, eps));
                return normalize(float3(delta_x, delta_y, delta_z));
            }

            RaymarchResult RayMarch(float3 pos, float3 dir)
            {
                #define STEPS 64                

                float3 original_pos = pos;
                for (int i = 0; i < STEPS; i++)
                {
                    float distance = BoxDistance(pos);
                    if (distance < m_min_distance)
                    {
                        RaymarchResult result;
                        result.m_hit_surface = true;
                        result.m_distance = length(pos - original_pos);
                        result.m_normal = CalcNormal(pos);
                        return result;
                    }

                    pos += dir * m_step_size;
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

                float3 camera_field_pos = _WorldSpaceCameraPos.xyz / (m_cell_radius * 2);
                float3 field_pos = i.world_pos.xyz / (m_cell_radius * 2);

                float3 camera_dir = normalize(field_pos - camera_field_pos);
                RaymarchResult result = RayMarch(field_pos, camera_dir);
                if (result.m_hit_surface)
                {
                    //return float4(0.5, 0.5, 0.5, 1);
                    float3 light_dir = normalize(float3(1, 1, 0));
                    float3 normal = result.m_normal;
                    return dot(light_dir, normal) * 0.5 + 0.5;
                    //return result.m_distance / 4;
                }
                else
                {
                    //discard;
                    return float4(1, 0, 0, 1);
                }
            }
            ENDCG

        }
    }
}