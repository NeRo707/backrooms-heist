Shader "Custom/VHS_Mobile_URP_FullScreen"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _BleedAmount("Bleed Amount", Float) = 0.005
        _NoiseAmount("Noise Amount", Float) = 0.05
        _FisheyeBend("Fisheye Bend", Float) = 0.2
        _TimeSpeed("Time Speed", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "VHSFullScreenPass"
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float _BleedAmount;
            float _NoiseAmount;
            float _FisheyeBend;
            float _TimeSpeed;

            half4 frag(Varyings input) : SV_Target
            {
                // Blit.hlsl uses 'texcoord' instead of 'uv'
                float2 uv = input.texcoord;

                // Fisheye effect with black borders
                float2 centered = uv - 0.5;
                float len = length(centered);
                uv = 0.5 + centered * (1.0 + _FisheyeBend * len * len);

                // If uv goes outside the [0,1] range, make it black
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return half4(0, 0, 0, 1);
                }

                // Chromatic bleed
                float2 rUV = uv + float2(_BleedAmount, 0);
                float2 gUV = uv;
                float2 bUV = uv - float2(_BleedAmount, 0);

                // Sample using _BlitTexture and URP's built-in linear clamp sampler
                half r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, rUV).r;
                half g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, gUV).g;
                half b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, bUV).b;
                half4 col = half4(r, g, b, 1.0);

                // Noise
                float2 noiseUV = uv * 0.25;
                half n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV + _Time.y * _TimeSpeed).r;
                col.rgb += (n - 0.5) * _NoiseAmount;

                return col;
            }
            ENDHLSL
        }
    }
}
