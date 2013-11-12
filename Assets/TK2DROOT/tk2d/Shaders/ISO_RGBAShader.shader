Shader "ISO_RGBAShader" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags {"Queue"="Transparent" "RenderType"="Transparent"}
		LOD 200
		ZWrite on
		Pass {
			Lighting Off
			
			Blend SrcAlpha OneMinusSrcAlpha
			
			SetTexture [_MainTex] { 
				combine texture 
			}
			
			SetTexture [_MainTex] {
				ConstantColor [_Color]
				Combine Previous * Constant
			}
		}
	}
}