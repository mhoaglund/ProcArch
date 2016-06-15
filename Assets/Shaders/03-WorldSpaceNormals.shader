Shader "Unlit/WorldSpaceNormals"
{
    // no Properties block this time!
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                half3 worldNormal : TEXCOORD0;
                float4 pos : SV_POSITION;
				float3 wPos : TEXCOORD1;
				float4 time : _Time;
            };

            v2f vert (float4 vertex : POSITION, float3 normal : NORMAL, appdata_full v)
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_MVP, vertex);
                o.worldNormal = UnityObjectToWorldNormal(normal);
				o.wPos = mul(_Object2World, v.vertex).xyz;

                return o;
            }

			/*float rand(vec2 co) {
				return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
			}*/
            
            fixed4 frag (v2f i, UNITY_VPOS_TYPE screenPos : SV_POSITION) : SV_Target
            {
                fixed4 c = 0;
				float worldposy = (i.wPos[1] / 25);
				c.rgb = float4(i.worldNormal[1], worldposy, (_SinTime[3] * worldposy), 1.0) * 0.5 + 0.5;
				/*screenPos.xy = floor(screenPos.xy * 0.25) * 0.5;
				float checker = -frac(screenPos.r + screenPos.g);
				c.a = checker;*/
                return c;
            }
            ENDCG
        }
    }
}
