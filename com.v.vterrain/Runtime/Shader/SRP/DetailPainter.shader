Shader "Hidden/V/DetailPainter"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Brush("Brush", 2D) = "white" {}
		_HitPos("_HitPos", Vector) = (0,0,0,0)
	}
		SubShader
		{
			// No culling or depth
			Cull Off ZWrite Off ZTest Always

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					return o;
				}

				sampler2D _MainTex;
				sampler2D _Brush;

				float4 _HitPos;	//z : BrushSize / Terrain size.z in uv space

				fixed4 frag(v2f i) : SV_Target
				{
					float4 col = tex2D(_MainTex, i.uv);
					half2 uvOffset = i.uv - half2(_HitPos.x, _HitPos.y);
					half2 uv = uvOffset / _HitPos.z;						// [-x,+x]
					uv = clamp(uv, half2(-1, -1), half2(1, 1));
					half intensity = 1.0 - saturate(length(uv));

					uv = (uv + half2(1, 1)) * 0.5f;
					half maskIntensity = tex2D(_Brush, uv);
					
					intensity *= _HitPos.w * maskIntensity;
					col += half4(intensity, 0, 0, intensity);
					return col;
				}
				ENDCG
			}
		}
}
