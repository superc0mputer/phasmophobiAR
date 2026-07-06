Shader "PhasmophobiAR/ParanormalLensOverlay"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.18, 0.45, 0.3, 1)
        _Visibility ("Visibility", Range(0.25, 3)) = 1.6
        _Interference ("Interference", Range(0, 1)) = 0.1
        _Flicker ("Flicker", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Visibility;
                half _Interference;
                half _Flicker;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float timeStep = floor(_Time.y * 24.0);
                float grain = Hash(input.uv * _ScreenParams.xy + timeStep);
                float scan = pow(saturate(sin(input.uv.y * _ScreenParams.y * 1.57) * .5 + .5), 14.0);
                float2 centered = input.uv * 2.0 - 1.0;
                float edge = smoothstep(.46, 1.22, dot(centered, centered));
                float glitchBand = step(.965, Hash(float2(timeStep, floor(input.uv.y * 18.0))));
                float alpha = edge * .24 + scan * (.035 + _Interference * .045)
                    + grain * (.025 + _Interference * .075) + glitchBand * _Interference * .1 + _Flicker;
                alpha *= _Visibility;
                return half4(saturate(_Tint.rgb * 1.18 + grain * .12), saturate(alpha));
            }
            ENDHLSL
        }
    }
}
