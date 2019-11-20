#ifndef _SolarLandForwardPass_HLSL_
#define _SolarLandForwardPass_HLSL_


#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"


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
	float4 texcoord					: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
	float2 uv                       : TEXCOORD0;
	DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
	float3 diffuse                  : TEXCOORD2;

	half4 uvWS                      : TEXCOORD3;

	half3 viewDirWS                 : TEXCOORD4;
	half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light
#ifdef _MAIN_LIGHT_SHADOWS
	float4 shadowCoord              : TEXCOORD7;
#endif
	float4 positionCS               : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


UNITY_INSTANCING_BUFFER_START(Props)
	UNITY_DEFINE_INSTANCED_PROP(float4, _UVOffset)
UNITY_INSTANCING_BUFFER_END(Props)


//TODO: 先使用脚本上传 shadowMask的图, 后面调研是否可写入到lightmap贴图的a通道中
sampler2D lmShadowMask; // Modify By vanCopper
void InitializeInputData(VertexOutput input, half3 normalTS, out InputData inputData)
{
	inputData = (InputData)0;


	half3 viewDirWS = input.viewDirWS;
	//inputData.normalWS = input.normalWS;

	inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
	viewDirWS = SafeNormalize(viewDirWS);

	inputData.viewDirectionWS = viewDirWS;
#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
	inputData.shadowCoord = input.shadowCoord;
#else
	inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
	inputData.fogCoord = input.fogFactorAndVertexLight.x;
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
	inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);


	//inputData.shadowMask = 1;

}

/// uv = (randSeed,V,GrassLength,0)
float3 WaveGrass(float3 vertex, float4 uv, out float3 diffuse,out float4 uvWS)
{
	//Calculate World Position & Mesh Info
	
	half4 uvOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _UVOffset);
	float2 UVOS = (float2(uv.z, uv.w) / 8) + 0.5;
	float2 UVWS = UVOS * uvOffset.zw + uvOffset.xy;
	uvWS.xy = UVWS;
	uvWS.zw = UVOS;

	float4 heightUV = float4(UVWS.x , UVWS.y , 0, 0);
	float heightV = tex2Dlod(_HeightMap, heightUV).r;
	float height = heightV * _HeightMultiplier * 2;	//For some reason, We need scale by 2. maybe unity did some scale to the height map
	
	//diffuse = tex2Dlod(_DiffuseMap, float4(UVWS, 0, 0));
	diffuse = half3(0.55, 0.8, 0.3);

	half densityMap = tex2Dlod(_DensityMap, float4(UVWS.xy, 0, 0)).r;
	if (densityMap < 0.05)
		height -= 500;
	
	float3 piovt = float3(uv.z,0,uv.w);

	//half a = -1.52;
	float3 offsetOS = vertex - piovt;
	vertex = piovt + offsetOS * _SizeScale;
	piovt += offsetOS * _MinSize;
	vertex = lerp(piovt, vertex, densityMap);
	

	float3 vertWS = mul(UNITY_MATRIX_M, float4(vertex, 1)).xyz;

#if defined(_GRASSWIND)

	float3 pivotWS = float3(vertWS.x, UNITY_MATRIX_M[1].w, vertWS.z);
	float vertexHeight = vertWS.y - pivotWS.y;
	


	float2 offsetXZ =float2(0,0);

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

	float totalLength = vertexHeight /uv.y ;
	float waveOffset = dot(_WindDirection, float2(pivotWS.x, pivotWS.z));

	half waveSpeed = _WindSpeed;
	float c = 2 * sin(_WindWaveFrequency * (_Time.y * waveSpeed) - waveOffset) - 1;
	float e = 1 / (0.1 * (c - 1) * (c - 1) + 1);

	float bandLength = e * vertexHeight * uv.y * 0.2 * bendIntensity * _Softness;
	
	offsetXZ.x += _WindDirection.x  * bandLength;
	offsetXZ.y += _WindDirection.y  * bandLength;

	
	vertWS.x = pivotWS.x + offsetXZ.x;
	vertWS.z = pivotWS.z + offsetXZ.y;
	vertWS.y -= vertexHeight * e * 0.1f;

	//float ysqr = saturate((vertexHeight * vertexHeight) - (offsetXZ.x * offsetXZ.x + offsetXZ.y * offsetXZ.y));
	//float y = sqrt(ysqr);
	//vertWS.y = pivotWS.y + y;

#else
	

#endif
	vertWS.y += height;
	float3 worldPos = vertWS;
	return worldPos;
}


VertexPositionInputs GrassGetVertexPositionInputs(float3 positionOS,float4 uv,out float3 diffuse,out float4 uvWS)
{
	VertexPositionInputs input;
	input.positionWS = WaveGrass(positionOS,uv, diffuse, uvWS);
	//input.positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1));
	input.positionVS = TransformWorldToView(input.positionWS);
	input.positionCS = TransformWorldToHClip(input.positionWS);

	float4 ndc = input.positionCS * 0.5f;
	input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
	input.positionNDC.zw = input.positionCS.zw;

	return input;
}

inline void Grass_InitializeStandardLitSurfaceData(VertexOutput input,float2 uv, out SurfaceData outSurfaceData)
{
	outSurfaceData.alpha = 1;
	outSurfaceData.albedo = input.diffuse;


	outSurfaceData.metallic = 0;
	outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);

	outSurfaceData.smoothness = _Smoothness;
	outSurfaceData.normalTS = half3(0,1,0);
	outSurfaceData.occlusion = 1;
	outSurfaceData.emission = half3(0,0,0);
}



void Grass_InitializeInputData(VertexOutput input, out InputData inputData)
{
	inputData = (InputData)0;



	half3 viewDirWS = input.viewDirWS;
	
	half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_Normal, input.uvWS));
	inputData.normalWS = normalTS;

	//inputData.normalWS = TransformTangentToWorld(normalTS,
	//	half3x3(half3(1,0,0), half3(0,0,1), half3(0,1,0)));

	

	//inputData.normalWS = half3(0,1,0);

	viewDirWS = SafeNormalize(viewDirWS);

	inputData.viewDirectionWS = viewDirWS;

#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
	inputData.shadowCoord = input.shadowCoord;
#else
	inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

	inputData.fogCoord = input.fogFactorAndVertexLight.x;
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
	inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
}

half3 GRASS_DirectBDRF(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS,half y)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
	half3 halfDir = SafeNormalize(lightDirectionWS + viewDirectionWS);

	half NoH = saturate(dot(normalWS, halfDir));
	half LoH = saturate(dot(lightDirectionWS, halfDir));

	// GGX Distribution multiplied by combined approximation of Visibility and Fresnel
	// BRDFspec = (D * V * F) / 4.0
	// D = roughness² / ( NoH² * (roughness² - 1) + 1 )²
	// V * F = 1.0 / ( LoH² * (roughness + 0.5) )
	// See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
	// https://community.arm.com/events/1155

	// Final BRDFspec = roughness² / ( NoH² * (roughness² - 1) + 1 )² * (LoH² * (roughness + 0.5) * 4.0)
	// We further optimize a few light invariant terms
	// brdfData.normalizationTerm = (roughness + 0.5) * 4.0 rewritten as roughness * 4.0 + 2.0 to a fit a MAD.
	half d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001h;

	half LoH2 = LoH * LoH;
	half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

	// on mobiles (where half actually means something) denominator have risk of overflow
	// clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
	// sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined (SHADER_API_MOBILE)
	specularTerm = specularTerm - HALF_MIN;
	specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif

	half3 color = specularTerm * (_Specular) * y *y + brdfData.diffuse;
	//half3 color = specularTerm * brdfData.specular * y +brdfData.diffuse;

	return color;
#else
	return brdfData.diffuse;
#endif
}

half3 GRASS_LightingPhysicallyBased(BRDFData brdfData, half3 lightColor, half3 lightDirectionWS, half lightAttenuation, half3 normalWS, half3 viewDirectionWS,half y)
{
	half NdotL = abs(dot(normalWS, lightDirectionWS));
	half3 radiance = lightColor * (lightAttenuation * NdotL);
	return GRASS_DirectBDRF(brdfData, normalWS, lightDirectionWS, viewDirectionWS,y) * radiance;
}

half3 GRASS_LightingPhysicallyBased(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS,half y)
{
	return GRASS_LightingPhysicallyBased(brdfData, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS,y);
}

inline void Grass_InitializeBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, half alpha, out BRDFData outBRDFData)
{
#ifdef _SPECULAR_SETUP
	half reflectivity = ReflectivitySpecular(specular);
	half oneMinusReflectivity = 1.0 - reflectivity;

	outBRDFData.diffuse = albedo * (half3(1.0h, 1.0h, 1.0h) - specular);
	outBRDFData.specular = specular;
#else

	half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
	half reflectivity = 1.0 - oneMinusReflectivity;

	outBRDFData.diffuse = albedo * oneMinusReflectivity;
	outBRDFData.specular = lerp(kDieletricSpec.rgb, albedo, metallic);
#endif

	outBRDFData.grazingTerm = saturate(smoothness + reflectivity);
	outBRDFData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
	outBRDFData.roughness = PerceptualRoughnessToRoughness(outBRDFData.perceptualRoughness);
	outBRDFData.roughness2 = outBRDFData.roughness * outBRDFData.roughness;

	outBRDFData.normalizationTerm = outBRDFData.roughness * 4.0h + 2.0h;
	outBRDFData.roughness2MinusOne = outBRDFData.roughness2 - 1.0h;

#ifdef _ALPHAPREMULTIPLY_ON
	outBRDFData.diffuse *= alpha;
	alpha = alpha * oneMinusReflectivity + reflectivity;
#endif
}

half4 LWPR_GrassFragmentPBR(InputData inputData, half3 albedo, half metallic, half3 specular,
	half smoothness, half occlusion, half3 emission, half alpha,half y)
{
	BRDFData brdfData;
	Grass_InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);

	Light mainLight = GetMainLight(inputData.shadowCoord);
	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));

	half3 color = GlobalIllumination(brdfData, inputData.bakedGI, occlusion, inputData.normalWS, inputData.viewDirectionWS);
	color += GRASS_LightingPhysicallyBased(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS,y);

	color += emission;
	return half4(color,alpha);
}


VertexOutput vert(VertexInput input)
{
	VertexOutput output = (VertexOutput)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);



	half3 normalWS = half3(0, 1, 0);
	VertexPositionInputs vertexInput = GrassGetVertexPositionInputs(input.positionOS.xyz, input.texcoord,output.diffuse,output.uvWS);
	
	half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
	half3 vertexLight = VertexLighting(vertexInput.positionWS, normalWS);
	half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
	//--------------------------------
	output.uv = input.texcoord.xy;
	output.viewDirWS = viewDirWS;
	//--------------------------------

	OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
	OUTPUT_SH(normalWS, output.vertexSH);

	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
	output.shadowCoord = GetShadowCoord(vertexInput);
#endif

	output.positionCS = vertexInput.positionCS;

	return output;
}


// Used in Standard (Physically Based) shader
half4 frag(VertexOutput input) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	//Alpha,Albedo,Metalic,Smoothness,occlusion,emission
	SurfaceData surfaceData;
	Grass_InitializeStandardLitSurfaceData(input,input.uv, surfaceData);

	InputData inputData;
	Grass_InitializeInputData(input, inputData);

	surfaceData.albedo = lerp(_BaseColor, _BaseColor2, input.uv.y).rgb;


	half4 color = LWPR_GrassFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha,input.uv.y);
	
	color.rgb = lerp(surfaceData.albedo, color,input.uv.y);

	color.rgb = MixFog(color.rgb, inputData.fogCoord);
	//color.r = input.uvWS.z;
	//color.r = 0;
	//color.g = input.uvWS.w;
	//color.b = 0;
	//color.g = input.uvWS.w;


	return color;
}










#endif
