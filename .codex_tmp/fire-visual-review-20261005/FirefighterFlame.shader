Shader "Deprem/Firefighter Flame"
{
    Properties
    {
        [HDR] _BaseColor ("Tint", Color) = (1,1,1,1)
        _SoftDistance ("Contact fade", Float) = 0.035
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
            float _SoftDistance;
            CBUFFER_END
            float _DepremFxTime;
            struct A { float4 position:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct V { float4 position:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; float eye:TEXCOORD1; float phase:TEXCOORD2; };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 a=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
            }
            V Vert(A i)
            {
                V o; float3 world=TransformObjectToWorld(i.position.xyz);
                o.position=TransformWorldToHClip(world); o.eye=-TransformWorldToView(world).z;
                o.color=i.color*_BaseColor; o.uv=i.uv;
                o.phase=dot(GetObjectToWorldMatrix()._m03_m13_m23,float3(3.17,.7,2.41)); return o;
            }
            half4 Frag(V i):SV_Target
            {
                float v=saturate(i.uv.y), x=i.uv.x*2-1;
                float time=_DepremFxTime*2.7+i.phase;
                float n=Noise(float2(x*3.1+time*.22,v*5.2-time));
                float detail=Noise(float2(x*6.4-time*.13,v*11-time*1.7));
                float center=sin(v*6-time*1.4)*v*v*.21+(n-.5)*v*.24;
                float width=pow(1-v,.72)*(.73+.17*n);
                float tongue=1-smoothstep(width*.48,width+.015,abs(x-center));
                float alpha=tongue*smoothstep(0,.09,v)*(1-smoothstep(.83,1,v));
                alpha*=lerp(.78,1,detail);
                float hot=saturate((1-v)*.88+(1-abs(x-center))*.25);
                half3 color=lerp(half3(1.05,.13,.015),half3(1.55,.66,.055),hot);
                color=lerp(color,half3(1.85,1.30,.49),pow(hot,4)*.75);
                float depth=SampleSceneDepth(i.position.xy/_ScaledScreenParams.xy);
                float eye=LinearEyeDepth(depth,_ZBufferParams);
                if(unity_OrthoParams.w>.5)
                {
                    #if UNITY_REVERSED_Z
                    depth=1-depth;
                    #endif
                    eye=lerp(_ProjectionParams.y,_ProjectionParams.z,depth);
                }
                alpha*=saturate((eye-i.eye)/max(.001,_SoftDistance));
                return half4(color*i.color.rgb,alpha*i.color.a);
            }
            ENDHLSL
        }
    }
}
