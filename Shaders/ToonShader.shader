// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.
//
// -Eric. Based on Toon shader from the Standard Unity shader: http://docs.unity3d.com/Manual/SL-SurfaceShaderExamples.html
Shader "Toon/Mobile/Shaded: Single Light" {
	Properties {
		_MainTex ( "Base (RGB)", 2D ) = "white" {}

		_Ramp ( "Ramp Threshold", Range( 0.01, 1.0 ) ) = 0.5
		_Smoothing ( "Ramp Falloff", Range( 0.01, 1.0 ) ) = 0.5

		_HighlightColor ( "Highlight Color", Color ) = ( 1.0, 1.0, 1.0 )
		_ShadowColor ( "Shadow Color", Color ) = ( 0.0, 0.0, 0.0 )
	}

	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 150

		CGPROGRAM
		#pragma surface surf Ramp

		fixed _Ramp;
		fixed _Smoothing;

		fixed3 _HighlightColor;
		fixed3 _ShadowColor;

		half4 LightingRamp( SurfaceOutput s, half3 lightDir, half atten ) {
			half NdotL = dot( s.Normal, lightDir );
			half diff = NdotL * 0.5 + 0.5;
			half4 c;
			fixed s = _Smoothing / 2.0;

			c.rgb = lerp (
				s.Albedo * _ShadowColor,
				s.Albedo * _LightColor0.rgb * atten *_HighlightColor,
				smoothstep( _Ramp - s, _Ramp + s, diff )
			);

			c.a = s.Alpha;

			return c;
		}

		struct Input {
			float2 uv_MainTex;
		};

		sampler2D _MainTex;

		void surf( Input IN, inout SurfaceOutput o ) {
			o.Albedo = tex2D( _MainTex, IN.uv_MainTex ).rgb;
		}
		ENDCG
	}

	Fallback "Mobile/VertexLit"
}
