#ifndef _VTerrainForwardPass_HLSL_
#define _VTerrainForwardPass_HLSL_


#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"



half _GridSize;
half _LineSize;


CBUFFER_START(UnityPerMaterial)
	float4 _HeightMap_TexelSize;
	half _HeightMultiplier;
	half _DensityMultiplier;
	half4 _BaseColor;
	half4 _BaseColor2;
	half3 _Specular;
	half _MinSize;
	half _SizeScale;
	half _Softness;
	half _Bais;
	half _Smoothness;
CBUFFER_END

sampler2D _DiffuseMap;
sampler2D _DensityMap;
sampler2D _HeightMap;


TEXTURE2D(_Normal); SAMPLER(sampler_Normal);


inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
	outSurfaceData.alpha = 1;
	outSurfaceData.albedo = _BaseColor.rgb;

	outSurfaceData.metallic = 0;
	outSurfaceData.specular = half3(0.0h, 1.0h, 0.0h);

	outSurfaceData.smoothness = 0.85;
	outSurfaceData.normalTS = half3(0, 1, 0);
	outSurfaceData.occlusion = 1;
	outSurfaceData.emission = half3(0, 0, 0);
}

//===========================================================================================================
struct VertexInput
{
	float4 positionOS				: POSITION;
	float2 texcoord					: TEXCOORD0;
	float3 normal                   : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
	float4 uv                       : TEXCOORD0;
	float3 positionWS               : TEXCOORD1;
	float4 positionCS               : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


UNITY_INSTANCING_BUFFER_START(Props)
	UNITY_DEFINE_INSTANCED_PROP(float4, _UVOffset)
UNITY_INSTANCING_BUFFER_END(Props)


/// uv = (randSeed,V,GrassLength,0)
float3 ApplyTerrainHeight(float3 vertex,float3 normal, float2 uv,out float heightValue)
{
	//Calculate World Position & Mesh Info
	
	half4 uvOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _UVOffset);
	float4 heightUV = float4(uv.x * uvOffset.x + uvOffset.z, uv.y * uvOffset.y + uvOffset.w, 0, 0);

	float heightV = tex2Dlod(_HeightMap, heightUV).r;
	heightValue = heightV;
	float height = heightV * _HeightMultiplier * 2;	//For some reason, We need scale by 2. maybe unity did some scale to the height map
	
	vertex += normal * 1.0f;
	float3 vertWS = mul(UNITY_MATRIX_M, float4(vertex, 1)).xyz;

	vertWS.y += height;
	float3 worldPos = vertWS;
	return worldPos;
}


VertexPositionInputs GetTerrainVertexPosition(float3 positionOS,float3 normal,float2 uv,out float heightValue)
{
	VertexPositionInputs input;
	input.positionWS = ApplyTerrainHeight(positionOS,normal,uv,heightValue);
	//input.positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1));
	input.positionVS = TransformWorldToView(input.positionWS);
	input.positionCS = TransformWorldToHClip(input.positionWS);

	float4 ndc = input.positionCS * 0.5f;
	input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
	input.positionNDC.zw = input.positionCS.zw;

	return input;
}


VertexOutput vert(VertexInput input)
{
	VertexOutput output = (VertexOutput)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	VertexPositionInputs vertexInput = GetTerrainVertexPosition(input.positionOS.xyz,input.normal, input.texcoord.xy, output.uv.z);
	//--------------------------------
	output.uv.xy = input.texcoord.xy;
	
	//--------------------------------
	output.positionWS = vertexInput.positionWS;
	output.positionCS = vertexInput.positionCS;

	return output;
}


#define PULSE(a,b,x) (step((a),(x)) - step((b),(x)))
float mod(float a, float b)
{
	int n = (int)(a / b);
	a -= n * b;
	if (a < 0)
		a += b;
	return a;
}

half4 Grid(VertexOutput input)
{
	float gridSize = _GridSize;
	float vX = PULSE(0.0, _LineSize, mod(input.positionWS.x, gridSize)/ gridSize);
	float vZ = PULSE(0.0, _LineSize, mod(input.positionWS.z, gridSize)/ gridSize);
	float v = max(vX, vZ);
	return half4(v, 0, 0, v * 0.9f);
}

// Used in Standard (Physically Based) shader
half4 frag(VertexOutput input) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	

	//return Grid(input);

	half4 uvOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _UVOffset);
	float2 g_UV = float2(input.uv.x * uvOffset.x + uvOffset.z, input.uv.y * uvOffset.y + uvOffset.w);
	return tex2D(_DensityMap, g_UV);
}










#endif
