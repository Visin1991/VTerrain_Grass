#ifndef _GGrassDepthOnly_HLSL_
#define _GGrassDepthOnly_HLSL_

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"


half2 _WindDirection;
half _WindSpeed;
half _WindWaveLength;
half _WindWaveFrequency;
half _Oscillation;
half _OscFrequency;

CBUFFER_START(UnityPerMaterial)
float4 _HeightMap_TexelSize;
half _HeightMultiplier;
half _DensityMultiplier;
half _Softness;
half _Bais;
CBUFFER_END

sampler2D _DensityMap;
sampler2D _HeightMap;



struct Attributes
{
	float4 position     : POSITION;
	float4 texcoord     : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS   : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

UNITY_INSTANCING_BUFFER_START(Props)
	UNITY_DEFINE_INSTANCED_PROP(float4, _UVOffset)
UNITY_INSTANCING_BUFFER_END(Props)



/// uv = (randSeed,V,GrassLength,0)
float3 WaveGrass(float3 vertex, float4 uv)
{
	//Calculate World Position & Mesh Info
	float3 vertWS = mul(UNITY_MATRIX_M, float4(vertex, 1)).xyz;

	float3 pivotWS = float3(vertWS.x, UNITY_MATRIX_M[1].w, vertWS.z);
	float vertexHeight = vertWS.y - pivotWS.y;

	//==============================================================================================
	float3 pivotWS_UV = mul(UNITY_MATRIX_M, float4(uv.w, UNITY_MATRIX_M[1].w, uv.z, 1)).xyz;
	half4 uvOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _UVOffset);
	float2 UVOS = (float2(pivotWS_UV.x - UNITY_MATRIX_M[0].w, pivotWS_UV.z - UNITY_MATRIX_M[2].w) / 8) + 0.5;
	float2 UVWS = UVOS * uvOffset.zw + uvOffset.xy;
	//half densityMap = tex2Dlod(_DensityMap, UVWS).r;
	float4 heightUV = float4(UVWS.x - _HeightMap_TexelSize.x * 0.5, UVWS.y + _HeightMap_TexelSize.y * 0.15, 0, 0);
	half height = tex2Dlod(_HeightMap, heightUV).r * _HeightMultiplier;
	vertWS.y += height;

	half densityMap = tex2Dlod(_DensityMap, float4(UVWS, 0, 0)).r;
	if (densityMap < 0.2)
		vertWS.y -= 500;
	//=========================================================================================


	/*float2 offsetXZ = float2(0, 0);

	float bendIntensity = (_WindSpeed / 50);

	//-----------------------------------------------------------
	//Random Bais Direction
	//-----------------------------------------------------------
	float2 offset = float2(cos(uv.x), sin(uv.x));
	offsetXZ.x += offset.x * vertexHeight * _Bais;
	offsetXZ.y += offset.y * vertexHeight * _Bais;
	//-----------------------------------------------------------

	//-----------------------------------------------------------
	//Rand Oscilliation
	//-----------------------------------------------------------
	float sinValue = sin((_Time.y * _OscFrequency * 5) + uv.x * uv.x);
	offsetXZ += offset * vertexHeight * _Oscillation * sinValue * 0.1f;
	////-----------------------------------------------------------

	//-----------------------------------------------------------
	//向风的方向倾斜
	//-----------------------------------------------------------
	offsetXZ += _WindDirection * vertexHeight * bendIntensity * 0.5f;
	//-----------------------------------------------------------

	//-----------------------------------------------------------
	//WindWave
	//---------------------------------------------------
	float offsetX = pivotWS.x * _WindDirection.x;
	float offsetZ = pivotWS.z * _WindDirection.y;

	float totalLength = vertexHeight / uv.y;
	float waveOffset = dot(_WindDirection, float2(pivotWS.x, pivotWS.z));

	half waveSpeed = _WindSpeed;
	float c = 2 * sin(_WindWaveFrequency * (_Time.y * waveSpeed) - waveOffset) - 1;
	float e = 1 / (0.1 * (c - 1) * (c - 1) + 1);

	float bandLength = e * vertexHeight * uv.y * 0.2 * bendIntensity * _Softness;

	offsetXZ.x += _WindDirection.x * bandLength;
	offsetXZ.y += _WindDirection.y * bandLength;*/

	float3 worldPos = vertWS;
	//worldPos.x = pivotWS.x + offsetXZ.x;
	//worldPos.z = pivotWS.z + offsetXZ.y;
	//worldPos.y -= vertexHeight * e * 0.1f;

	//float ysqr = saturate((vertexHeight * vertexHeight) - (offsetXZ.x * offsetXZ.x + offsetXZ.y * offsetXZ.y));
	//float y = sqrt(ysqr);
	//worldPos.y = pivotWS.y + y;
	//---------------------------------------------------

	return worldPos;
}


Varyings DepthOnlyVertex(Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);

	float3 positionWS = WaveGrass(input.position, input.texcoord);
	output.positionCS = TransformWorldToHClip(positionWS);
	return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
	return 0;
}

#endif
