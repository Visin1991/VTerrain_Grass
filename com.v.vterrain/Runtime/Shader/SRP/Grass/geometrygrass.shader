Shader "Test/GeometryGrass"
{
	Properties
	{
		//_DiffuseMap("Diffuse Map(R)", 2D) = "white" {}
		_UVOffset("World UV Offset", Vector) = (0, 0, 0, 0)
		_DensityMap("Density Map(R)", 2D) = "white" {}
		_Normal("Normal Map", 2D) = "white" {}
		_HeightMap("Terrain Heightmap", 2D) = "black" {}
		_HeightMultiplier("Terrain Height Multiplier", Float) = 500
		//_DensityMultiplier("Density Threshold", range(0, 16)) = 16

		_BaseColor2("草尖--颜色", Color) = (0.55,0.8,0.3,1)
		[MainColor] _BaseColor("Main Color", Color) = (0.55,0.8,0.3,1)
		
		_Specular("Specular Color", Color) = (0.2,0.3,0.1,1)		
		_SizeScale("大小", Range(0.1,5)) = 3
		_MinSize("大小最小值", Range(0.1,2)) = 0.1
		_Bais("偏移强度", Range(0.01,0.3)) = 0.3
		_Softness("植物柔软度", Range(0,5)) = 2

		_Smoothness("光滑度",Range(0,1)) = 0.65
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque+50" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True" }
		LOD 300
		Pass
		{
			Name "ForwardLit"
			Tags{"LightMode" = "LightweightForward"}
			Cull Off

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			// -------------------------------------
			 // Lightweight Pipeline keywords
			 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			 #pragma multi_compile _ _SHADOWS_SOFT
			 #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog


			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			//---------------------------------------
			//Grass Multi-Compile
			#define _GRASSWIND 1

			#pragma vertex vert
			#pragma fragment frag

			#include "GrassForwardPass.hlsl"


			ENDHLSL
		}
		/*
		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			ColorMask 0
			Cull Off

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			#include "GrassDepthOnly.hlsl"
			ENDHLSL
		}
		*/
	}
	FallBack "Hidden/InternalErrorShader"
}
