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
            float Turbulence(float2 p)
            {
                float value=Noise(p)*.56;
                p=float2(p.x*1.76-p.y*.58,p.x*.58+p.y*1.76)+17.3;
                value+=Noise(p)*.29;
                p=float2(p.x*1.76-p.y*.58,p.x*.58+p.y*1.76)+23.1;
                return value+Noise(p)*.15;
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
                float time=_DepremFxTime*1.7+i.phase;
                // Advect irregular pockets upward, then curl their outlines.
                // The noise changes the silhouette, rather than tinting a straight blade.
                float2 flow=float2(x*2.2,v*3.0-time);
                float curl=Turbulence(flow+float2(4.1,9.3));
                flow.x+=(curl-.5)*(.6+v*1.1);
                float heatNoise=Turbulence(flow);
                float lateral=x+sin(v*7.2-time)*v*v*.30+(curl-.5)*v*.35;
                float field=1.04-abs(lateral)*1.08-v*.96+(heatNoise-.5)*(.32+v*1.04);
                float alpha=smoothstep(.08,.23,field);
                alpha*=smoothstep(0,.065,v)*(1-smoothstep(.87,1,v));
                float hot=saturate(field*1.28-v*.24);
                half3 color=lerp(half3(.88,.065,.008),half3(1.28,.39,.018),smoothstep(.08,.56,hot));
                color=lerp(color,half3(1.55,1.02,.20),smoothstep(.57,.93,hot));
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
