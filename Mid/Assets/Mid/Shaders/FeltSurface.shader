Shader "Mid/FeltSurface"
{
    Properties
    {
        _Color("Wool colour",Color)=(1,1,1,1)
        _MainTex("Seamless fibres",2D)="white"{}
        _BumpMap("Micro normal",2D)="bump"{}
        _Scale("World weave scale",Float)=7
        _BumpStrength("Soft relief",Range(0,1))=.35
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap;
        fixed4 _Color; float _Scale, _BumpStrength;
        struct Input {float3 worldPos; float3 worldNormal; INTERNAL_DATA};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 weights=pow(abs(WorldNormalVector(IN,float3(0,0,1))),4);
            weights/=max(dot(weights,float3(1,1,1)),.0001);
            float3 p=IN.worldPos*_Scale;
            fixed3 weave=tex2D(_MainTex,p.yz).rgb*weights.x+tex2D(_MainTex,p.xz).rgb*weights.y+tex2D(_MainTex,p.xy).rgb*weights.z;
            float3 bump=UnpackNormal(tex2D(_BumpMap,p.yz))*weights.x+UnpackNormal(tex2D(_BumpMap,p.xz))*weights.y+UnpackNormal(tex2D(_BumpMap,p.xy))*weights.z;
            o.Albedo=_Color.rgb*weave;
            o.Normal=normalize(float3(bump.xy*_BumpStrength,1));
            o.Smoothness=.025; o.Metallic=0; o.Occlusion=.98; o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
