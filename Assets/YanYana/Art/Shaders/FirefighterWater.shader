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
                float x=saturate(i.uv.x), flow=_DepremFxTime*12;
                float ripple=Noise(float2(x*29-flow,i.uv.y*5));
                float center=(ripple-.5)*.045*x;
                float y=abs((i.uv.y-.5)*2-center);
                float core=1-smoothstep(.26,lerp(.86,.71,x),y);
                float foam=Noise(float2(x*64-flow*2,i.uv.y*13));
                float wisps=(1-smoothstep(.4,1,y))*smoothstep(.55,.9,ripple)*x;
                float breakup=lerp(1,smoothstep(.16,.68,foam),smoothstep(.72,1,x)*.62);
                float alpha=(core*lerp(.48,.68,foam)+wisps*.10)*breakup*(1-smoothstep(.98,1,x));
                half3 tint=lerp(half3(.64,.87,.97),half3(.96,.99,1),core*.6+foam*.4);
                return half4(tint*i.color.rgb,alpha*i.color.a);
            }
            ENDHLSL
        }
    }
}
