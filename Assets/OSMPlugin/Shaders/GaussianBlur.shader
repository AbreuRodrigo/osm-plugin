﻿Shader "TSG/OSM/GaussianBlur"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		radius("Radius", Range(0,30)) = 30
		resolution("Resolution", float) = 800
		hstep("HorizontalStep", Range(0,1)) = 0.5
		vstep("VerticalStep", Range(0,1)) = 0.5
	}

	SubShader
	{
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "true" "RenderType" = "Transparent"}
		ZWrite Off Blend SrcAlpha OneMinusSrcAlpha Cull Off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};
			struct v2f
			{
				half2 texcoord  : TEXCOORD0;
				float4 vertex   : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float radius;
			float resolution;
			float hstep;
			float vstep;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color;
				return OUT;
			}

			float4 frag(v2f i) : COLOR
			{
				float2 uv = i.texcoord.xy;
				float4 sum = float4(0.0, 0.0, 0.0, 0.0);
				float2 tc = uv;

				//blur radius in pixels
				float blur = radius / resolution / 4;

				sum += tex2D(_MainTex, float2(tc.x - 4.0*blur*hstep, tc.y - 4.0*blur*vstep)) * 0.0162162162;
				sum += tex2D(_MainTex, float2(tc.x - 3.0*blur*hstep, tc.y - 3.0*blur*vstep)) * 0.0540540541;
				sum += tex2D(_MainTex, float2(tc.x - 2.0*blur*hstep, tc.y - 2.0*blur*vstep)) * 0.1216216216;
				sum += tex2D(_MainTex, float2(tc.x - 1.0*blur*hstep, tc.y - 1.0*blur*vstep)) * 0.1945945946;

				sum += tex2D(_MainTex, float2(tc.x, tc.y)) * 0.2270270270;

				sum += tex2D(_MainTex, float2(tc.x + 1.0*blur*hstep, tc.y + 1.0*blur*vstep)) * 0.1945945946;
				sum += tex2D(_MainTex, float2(tc.x + 2.0*blur*hstep, tc.y + 2.0*blur*vstep)) * 0.1216216216;
				sum += tex2D(_MainTex, float2(tc.x + 3.0*blur*hstep, tc.y + 3.0*blur*vstep)) * 0.0540540541;
				sum += tex2D(_MainTex, float2(tc.x + 4.0*blur*hstep, tc.y + 4.0*blur*vstep)) * 0.0162162162;
				return float4(sum.rgb, 1);
			}
			ENDCG
		}
	}

	Fallback "Sprites/Default"
}