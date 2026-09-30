Shader "Deprem/Volumetric Fire"
{
    Properties { _Intensity ("Warmth", Float)=1.15 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Pass
        {
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Intensity;
            CBUFFER_END
            float _DepremFxTime;
            struct A { float4 pos:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2; };
            V Vert(A i)
            {
                V o;float3 p=TransformObjectToWorld(i.pos.xyz);float h=saturate(i.uv.y);
                float phase=dot(GetObjectToWorldMatrix()._m03_m13_m23,float3(1.7,.6,2.3));
                float bend=h*h*.072;
                p.x+=sin(_DepremFxTime*3.0+h*4.8+phase)*bend;
                p.z+=cos(_DepremFxTime*2.3+h*4.1+phase)*bend*.65;
                p.y+=sin(_DepremFxTime*4.1-h*3+phase)*h*.046;
                o.pos=TransformWorldToHClip(p);o.world=p;o.normal=TransformObjectToWorldNormal(i.normal);o.uv=i.uv;return o;
            }
            half4 Frag(V i):SV_Target
            {
                float facing=saturate(dot(normalize(i.normal),normalize(GetWorldSpaceViewDir(i.world))));
                float h=saturate(i.uv.y);
                half3 edge=half3(.82,.075,.012),body=half3(1.45,.31,.015),core=half3(1.85,.92,.10);
                half3 c=lerp(edge,body,.35+.65*facing);
                c=lerp(c,core,pow(facing,3)*(1-smoothstep(.10,.78,h))*.85);
                c*=.86+.14*saturate(dot(normalize(i.normal),normalize(float3(-.5,.6,-.3))));
                float pulse=1+.045*sin(_DepremFxTime*4.2+i.world.x*3+i.world.z*2);
                return half4(c*_Intensity*pulse,1);
            }
            ENDHLSL
        }
    }
}
