Shader "Deprem/Adventure Particles"
{
    Properties
    {
        [HDR] _BaseColor ("Tint", Color) = (1,1,1,1)
        _Kind ("0 Flame, 1 Smoke, 2 Dust, 3 Water, 4 Wet ground", Float) = 0
        _SoftDistance ("Surface fade distance", Float) = 0.1
    }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Kind;
            float _SoftDistance;
            CBUFFER_END
            float _DepremFxTime;
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; float eye:TEXCOORD1; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                float3 world=TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(world); o.eye=-TransformWorldToView(world).z;
                o.color=i.color*_BaseColor; o.uv=i.uv; return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 a=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.uv*2-1; float v=saturate(i.uv.y);
                float grain=Noise(i.uv*7.1)*.7+Noise(i.uv*15.3)*.3;
                half4 c=i.color;
                if (_Kind<.5)
                {
                    float center=sin(v*5.8)*.13*v;
                    float width=lerp(.70,.04,pow(v,.8));
                    float tongue=1-smoothstep(width*.35,width,abs(p.x-center));
                    c.a*=tongue*smoothstep(0,.12,v)*(1-smoothstep(.60,1,v));
                    c.rgb*=lerp(half3(2.1,1.15,.28),half3(1.45,.20,.035),v);
                }
                else if (_Kind<2.5)
                {
                    float r=length(p*float2(1,.88));
                    c.a*=(1-smoothstep(.12,1.05,r+(.5-grain)*.16))*lerp(.66,1,grain);
                }
                else if (_Kind<3.5)
                {
                    c.a*=1-smoothstep(.08,.49,abs(i.uv.y-.5));
                    c.rgb+=half3(.18,.22,.24)*(1-smoothstep(.02,.16,abs(i.uv.y-.50)));
                    c.rgb+=half3(.09,.12,.13)*saturate(sin(i.uv.x*110-_DepremFxTime*52));
                }
                else
                {
                    c.a*=1-smoothstep(.4,1,length(p)+(.5-grain)*.12);
                }
                if (_Kind<3.5)
                {
                    float depth=SampleSceneDepth(i.positionCS.xy/_ScaledScreenParams.xy);
                    float eye=LinearEyeDepth(depth,_ZBufferParams);
                    if (unity_OrthoParams.w>.5)
                    {
                        #if UNITY_REVERSED_Z
                        depth=1-depth;
                        #endif
                        eye=lerp(_ProjectionParams.y,_ProjectionParams.z,depth);
                    }
                    c.a*=saturate((eye-i.eye)/max(.001,_SoftDistance));
                }
                return c;
            }
            ENDHLSL
        }
    }
}
