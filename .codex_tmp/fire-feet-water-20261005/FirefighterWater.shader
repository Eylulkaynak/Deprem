Shader "Deprem/Firefighter Water"
{
    Properties { _BaseColor ("Tint", Color) = (.86,.96,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            float _DepremFxTime;
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color*_BaseColor; return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 a=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
            }
            half4 Frag(Varyings i):SV_Target
            {
                float x=saturate(i.uv.x), flow=_DepremFxTime*17;
                float ripple=Noise(float2(x*37-flow,i.uv.y*6));
                float center=(ripple-.5)*.11*x;
                float y=abs((i.uv.y-.5)*2-center);
                float core=1-smoothstep(lerp(.46,.18,x),lerp(.91,.55,x),y);
                float foam=Noise(float2(x*72-flow*2.3,i.uv.y*19));
                float wisps=(1-smoothstep(.3,1,y))*smoothstep(.35,.85,ripple)*x;
                float alpha=(core*lerp(.72,.88,foam)+wisps*.22)*(1-smoothstep(.97,1,x));
                half3 tint=lerp(half3(.48,.79,.92),half3(.94,.99,1),core*.65+foam*.35);
                return half4(tint*i.color.rgb,alpha*i.color.a);
            }
            ENDHLSL
        }
    }
}
