Shader "Unlit/WorldSpaceNormals"
{
	Properties{
		_colorBalance ("colorBalance", Color) = (0,0,0,0)
	}

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target es3.0
            #include "UnityCG.cginc"
			fixed4 _colorBalance;

            struct v2f {
				fixed4 color : TEXCOORD2;
				
                half3 worldNormal : TEXCOORD0;
                float4 pos : SV_POSITION;
				float3 wPos : TEXCOORD1;
				float4 time : _Time;
            };

			float avg(float a, float b) {
				float res = 0.0;
				res = (a + b) / 2;
				return (float)res;
			}

            v2f vert (float4 vertex : POSITION, float3 normal : NORMAL, appdata_full v, uint vid : SV_VertexID)
            {
                v2f o;
				float f = (float)vid;
				o.color = half4(sin(f / 10), sin(f / 100), sin(f / 1000), 0) * 0.5 + 0.5;
                o.pos = mul(UNITY_MATRIX_MVP, vertex);
                o.worldNormal = UnityObjectToWorldNormal(normal);
				o.wPos = mul(_Object2World, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i, UNITY_VPOS_TYPE screenPos : SV_POSITION) : SV_Target
            {
                fixed4 c = 0;
				float worldposy = (i.wPos[1] / 90);
				float idy = (worldposy + i.color[1]) / 2;
				float smallidmod = i.color[1] / 20;
				int timeindex = (_colorBalance.rgb[1] > 0.5) ? 2 : 3;
				float t = _SinTime[timeindex] * (_colorBalance.rgb[1] * 3);

				c.rgb = fixed4(avg(i.worldNormal[1], _colorBalance.rgb[1]), _colorBalance.rgb[2] - worldposy, (t * idy), 0) * 0.5 + 0.5;

                return c;
            }
            ENDCG
        }
    }
}
