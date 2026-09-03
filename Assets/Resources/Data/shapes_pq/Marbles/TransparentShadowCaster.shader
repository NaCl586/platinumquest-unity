Shader "Custom/TransparentShadowCaster"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,0.5)
        _ShadowCutoff("Shadow Cutoff", Range(0,1)) = 0.01
    }

        SubShader
        {
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
            }

            // ============================================================
            // VISIBLE TRANSPARENT PASS
            // ============================================================

            Pass
            {
                Name "Forward"
                Tags
                {
                    "LightMode" = "UniversalForward"
                }

                Blend SrcAlpha OneMinusSrcAlpha
                ZWrite Off
                Cull Back

                HLSLPROGRAM

                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionHCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                TEXTURE2D(_BaseMap);
                SAMPLER(sampler_BaseMap);

                CBUFFER_START(UnityPerMaterial)
                    float4 _BaseMap_ST;
                    float4 _Color;
                    float _ShadowCutoff;
                CBUFFER_END

                Varyings vert(Attributes input)
                {
                    Varyings output;

                    output.positionHCS =
                        TransformObjectToHClip(input.positionOS.xyz);

                    output.uv =
                        TRANSFORM_TEX(input.uv, _BaseMap);

                    return output;
                }

                half4 frag(Varyings input) : SV_Target
                {
                    half4 tex =
                        SAMPLE_TEXTURE2D(
                            _BaseMap,
                            sampler_BaseMap,
                            input.uv
                        );

                    return tex * _Color;
                }

                ENDHLSL
            }

            // ============================================================
            // SHADOW CASTER
            // ============================================================

            Pass
            {
                Name "ShadowCaster"

                Tags
                {
                    "LightMode" = "ShadowCaster"
                }

                ZWrite On
                ZTest LEqual
                Cull Back

                HLSLPROGRAM

                #pragma vertex vertShadow
                #pragma fragment fragShadow

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normalOS : NORMAL;
                    float2 uv : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                TEXTURE2D(_BaseMap);
                SAMPLER(sampler_BaseMap);

                CBUFFER_START(UnityPerMaterial)
                    float4 _BaseMap_ST;
                    float4 _Color;
                    float _ShadowCutoff;
                CBUFFER_END

                Varyings vertShadow(Attributes input)
                {
                    Varyings output;

                    output.positionCS =
                        TransformObjectToHClip(input.positionOS.xyz);

                    output.uv =
                        TRANSFORM_TEX(input.uv, _BaseMap);

                    return output;
                }

                half4 fragShadow(Varyings input) : SV_Target
                {
                    half4 tex =
                        SAMPLE_TEXTURE2D(
                            _BaseMap,
                            sampler_BaseMap,
                            input.uv
                        );

                    clip(tex.a* _Color.a - _ShadowCutoff);

                    return 0;
                }

                ENDHLSL
            }
        }
}