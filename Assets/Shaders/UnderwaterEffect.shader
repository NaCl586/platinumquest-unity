Shader "Hidden/UnderwaterEffect"
{
    Properties
    {
        _Intensity("Intensity", Range(0, 1)) = 0
        _Tint("Water Tint", Color) = (0.05, 0.35, 0.45, 1)

        _DistortionStrength("Distortion Strength", Range(0, 0.05)) = 0.008
        _DistortionSpeed("Distortion Speed", Range(0, 5)) = 1
        _DistortionScale("Distortion Scale", Range(1, 50)) = 12

        _Darkness("Darkness", Range(0, 1)) = 0.12
    }

        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Underwater"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float4 _BlitTexture_TexelSize;

            float _Intensity;
            float4 _Tint;

            float _DistortionStrength;
            float _DistortionSpeed;
            float _DistortionScale;
            float _Darkness;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float2 uv = float2(
                    (input.vertexID << 1) & 2,
                    input.vertexID & 2
                );

                output.positionCS =
                    float4(
                        uv * 2.0 - 1.0,
                        0.0,
                        1.0
                    );

                output.uv = uv;

                return output;
            }

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);

                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }

            float2 GetDistortion(float2 uv)
            {
                float time =
                    _Time.y * _DistortionSpeed;

                float2 p =
                    uv * _DistortionScale;

                float n1 =
                    noise(
                        p +
                        float2(
                            time * 0.35,
                            time * 0.15
                        )
                    );

                float n2 =
                    noise(
                        p * 1.35 +
                        float2(
                            -time * 0.2,
                            time * 0.3
                        )
                    );

                float2 distortion;

                distortion.x = n1 - 0.5;
                distortion.y = n2 - 0.5;

                float2 centered =
                    uv - 0.5;

                float edge =
                    saturate(
                        length(centered) * 1.8
                    );

                distortion *=
                    _DistortionStrength *
                    lerp(0.5, 1.0, edge);

                return distortion;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Unity 2022.3 renderer requires the screen texture
                // to be vertically flipped when sampled here.
                uv.y = 1.0 - uv.y;

                float2 distortion =
                    GetDistortion(uv);

                float2 distortedUV =
                    uv + distortion * _Intensity;

                half4 sceneColor =
                    SAMPLE_TEXTURE2D(
                        _BlitTexture,
                        sampler_BlitTexture,
                        distortedUV
                    );

                float tintStrength =
                    0.35 * _Intensity;

                sceneColor.rgb =
                    lerp(
                        sceneColor.rgb,
                        sceneColor.rgb * _Tint.rgb * 2.0,
                        tintStrength
                    );

                sceneColor.rgb *=
                    lerp(
                        1.0,
                        1.0 - _Darkness,
                        _Intensity
                    );

                return sceneColor;
            }

            ENDHLSL
        }
    }
}