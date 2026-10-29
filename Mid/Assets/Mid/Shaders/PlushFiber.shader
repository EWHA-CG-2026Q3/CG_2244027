Shader "Mid/PlushFiber"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        fixed4 _Color;
        struct Input { float4 color : COLOR; };
        void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.color=v.color; }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo=IN.color.rgb*_Color.rgb;
            o.Metallic=0; o.Smoothness=0.04; o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
