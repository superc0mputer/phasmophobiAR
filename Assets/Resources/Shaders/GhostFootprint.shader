Shader "PhasmophobiAR/Ghost Footprint"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.62, 0.9, 1.0, 0.48)
        _GlowColor ("Glow Color", Color) = (0.78, 0.98, 1.0, 0.32)
        _GlowStrength ("Glow Strength", Range(0.0, 1.0)) = 0.08
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 localPos : TEXCOORD0;
            };
            half4 _BaseColor;
            half4 _GlowColor;
            half _GlowStrength;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.localPos = input.positionOS.xz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half radial = saturate(1.0h - length(input.localPos * half2(1.55h, 0.92h)));
                half glow = smoothstep(0.0h, 0.42h, radial) * (1.0h - smoothstep(0.48h, 0.9h, radial));
                half4 color = _BaseColor;
                color.rgb += glow * _GlowColor.rgb * _GlowStrength;
                return color;
            }
            ENDHLSL
        }
    }
}
