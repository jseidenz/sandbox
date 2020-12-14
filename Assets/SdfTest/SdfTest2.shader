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

                float3 world_uv = world_pos / _WorldSizeInMeters;
                float distance = tex3D(_LiquidTex, world_uv).r;
                distance = distance * STEP_SIZE;
                return distance;
            }

            bool Raycast(float3 pos, float3 dir)
            {
                #define MIN_DISTANCE 0.01
                #define STEPS 128                

                for (int i = 0; i < STEPS; i++)
                {
                    float distance = SphereDistance(pos);
                    if (distance < MIN_DISTANCE)
                    {
                        return true;
                    }

                    pos += dir * distance;
                }

                return false;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //float3 world_uv = (i.world_pos.xyz / _WorldSizeInMeters.xyz) * 0.5 + 0.5;
                //float radius = tex3D(_LiquidTex, world_uv).r;
                //return float4(radius.xxx, 1);

                float3 camera_dir = normalize(i.world_pos.xyz -_WorldSpaceCameraPos.xyz);
                if (Raycast(i.world_pos.xyz, camera_dir))
                {
                    float3 light_dir = normalize(float3(1, 1, 0));
                    float3 normal = normalize(i.normal);
                    return dot(light_dir, normal) * 0.5 + 0.5;
                }
                else
                {
                    return float4(1, 0, 0, 1);
                }
            }
            ENDCG

        }
    }
}