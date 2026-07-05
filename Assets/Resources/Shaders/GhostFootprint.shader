Shader "PhasmophobiAR/Ghost Footprint"
{
    Properties
    {
        _BaseMap ("Footprint", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.08, 0.09, 0.12, 0.72)
        _Contrast ("Contrast", Range(0.5, 4.0)) = 2.2
        _OpacityBoost ("Opacity Boost", Range(0.5, 3.0)) = 1.6
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.6)) = 0.16
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "GhostFootprint"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Contrast;
            half _OpacityBoost;
            half _EdgeSoftness;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 mask = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half centered = saturate((mask.a - (0.5h - _EdgeSoftness)) / max(0.0001h, _EdgeSoftness * 2.0h));
                half shaped = pow(centered, max(0.01h, 1.0h / _Contrast));
                half4 color = _BaseColor;
                color.rgb *= lerp(0.82h, 1.18h, shaped);
                color.a *= saturate(shaped * _OpacityBoost);
                return color;
            }
            ENDHLSL
        }
    }
}
