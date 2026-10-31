Shader "CG/S17_MyTextureAbs"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _BaseMap;

            void vert(float3 positionOS : POSITION,
                      float2 uv : TEXCOORD0,
                      out float2 uvOut : TEXCOORD0,
                      out float4 positionCS : SV_POSITION)
            {
                uvOut = uv;
                positionCS = TransformObjectToHClip(positionOS);
            }

            float4 frag(float2 uv : TEXCOORD0) : SV_Target
            {
                // Only the Quad's upper-left UV quadrant receives the texture.
                if (uv.x > 0.5 || uv.y < 0.5)
                    return float4(0.62, 0.62, 0.62, 1);

                // Move that quadrant to the texture's lower-right quadrant.
                // (0, 0.5)..(0.5, 1) becomes (0.5, 0)..(1, 0.5).
                float2 sampleUV = uv + float2(0.5, -0.5);
                return tex2D(_BaseMap, sampleUV);
            }
            ENDHLSL
        }
    }
}