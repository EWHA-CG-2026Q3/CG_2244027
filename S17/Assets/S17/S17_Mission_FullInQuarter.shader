Shader "CG/S17_Mission_FullInQuarter"
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
                // The whole image fills the lower-left quarter of each surface.
                float2 p = uv * 2;
                if (p.x > 1 || p.y > 1)
                    return float4(0.8, 0.8, 0.8, 1);
                return tex2D(_BaseMap, p);
            }
            ENDHLSL
        }
    }
}
