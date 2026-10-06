Shader "Deprem/Firefighter Hose"
{
    Properties { _BaseColor ("Rubber",Color)=(.12,.15,.15,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct A { float4 position:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; };
            struct V { float4 position:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; };
            V Vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;o.color=i.color;return o;}
            half4 Frag(V i):SV_Target
            {
                float across=i.uv.y*2-1;
                float rounded=sqrt(saturate(1-across*across));
                float weave=.94+.06*sin(i.uv.x*230);
                float highlight=pow(saturate(1-abs(across+.25)),12)*.075;
                return half4(_BaseColor.rgb*i.color.rgb*(.28+rounded*.72)*weave+highlight,1);
            }
            ENDHLSL
        }
    }
}
