"""Small, traceable modification of the installed Unity URP Lit shader. No package edits."""
import pathlib,re,shutil
root=pathlib.Path(__file__).resolve().parents[2]
package=next((root/'Library/PackageCache').glob('com.unity.render-pipelines.universal@*'))
out=root/'Assets/YanYana/Art/Shaders';out.mkdir(exist_ok=True)
shader=(package/'Shaders/Lit.shader').read_text()
# The custom colour operation is a forward pass; ForwardOnly also works in deferred renderers.
gb=shader.rfind('        Pass',0,shader.index('Name "GBuffer"'))
assert gb>=0
depth=0;end=None
for i in range(shader.index('{',gb),len(shader)):
    depth+= (shader[i]=='{')-(shader[i]=='}')
    if depth==0:end=i+1;break
shader=shader[:gb]+shader[end:]
shader=shader.replace('Shader "Universal Render Pipeline/Lit"','Shader "YanYana/Approved Wardrobe"')
shader=shader.replace('"LightMode" = "UniversalForward"','"LightMode" = "UniversalForwardOnly"')
shader=shader.replace('        // Specular vs Metallic workflow','        _WardrobeColor("Garment colour", Color) = (1,1,1,1)\n        _GreyColor("Grey hair", Color) = (.65,.65,.6,1)\n        _WardrobeMode("0 navy / 1 coral fabric", Float) = 0\n        _GreyAmount("Grey hair amount", Range(0,1)) = 0\n        // Specular vs Metallic workflow')
shader=shader.replace('Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl','ApprovedWardrobeInput.hlsl').replace('Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl','ApprovedWardrobeForward.hlsl')
shader=shader.replace('        _WardrobeColor(', '        _MaskDebug("Show wardrobe regions", Float) = 0\n        _WardrobeColor(')
shader=shader.replace('        _WardrobeColor(', '        _WardrobeRegions("Authored garment and hair regions", 2D) = "black" {}\n        _WardrobeColor(')
shader=re.sub(r'^\s*CustomEditor .*$', '',shader,flags=re.M)
(out/'ApprovedWardrobe.shader').write_text(shader)
source=(package/'Shaders/LitInput.hlsl').read_text().replace('CBUFFER_END','half4 _WardrobeColor;\nhalf4 _GreyColor;\nhalf _WardrobeMode;\nhalf _GreyAmount;\nhalf _MaskDebug;\nCBUFFER_END',1)
(out/'ApprovedWardrobeInput.hlsl').write_text(source)
source=(package/'Shaders/LitForwardPass.hlsl').read_text()
source=source.replace('// keep this file in sync', 'TEXTURE2D(_WardrobeRegions); SAMPLER(sampler_WardrobeRegions);\n// keep this file in sync')
source=source.replace('InitializeStandardLitSurfaceData(input.uv, surfaceData);','''InitializeStandardLitSurfaceData(input.uv, surfaceData);
    // Regions baked from the authored geometry keep colour edits away from hands and faces. Texture-space
    // chroma then retains the exact textile/hair boundary, including antialiased texels.
    half3 rgb = LinearToSRGB(surfaceData.albedo);
    half navy = smoothstep(.005h,.025h,rgb.b-rgb.r) * smoothstep(.005h,.025h,rgb.b-rgb.g);
    half coral = smoothstep(.86h,.905h,rgb.b/max(.01h,rgb.g)) * smoothstep(1.08h,1.18h,rgb.r/max(.01h,rgb.g));
    half2 wardrobe = SAMPLE_TEXTURE2D(_WardrobeRegions,sampler_WardrobeRegions,input.uv).rg;
    half cloth = wardrobe.r * lerp(navy,coral,_WardrobeMode);
    half lum = dot(surfaceData.albedo,half3(.2126h,.7152h,.0722h));
    half textureShade = clamp(sqrt(lum / lerp(.016h,.24h,_WardrobeMode)),.35h,1.45h);
    half3 coloured = _WardrobeColor.rgb * textureShade;
    half grey = wardrobe.g * _GreyAmount * (1-smoothstep(.44h,.58h,rgb.r));
    surfaceData.albedo = lerp(surfaceData.albedo,coloured,cloth);
    surfaceData.albedo = lerp(surfaceData.albedo,_GreyColor.rgb*clamp(sqrt(lum/.055h),.24h,1.25h),grey);''')
source=source.replace('    InputData inputData;', '    if(_MaskDebug>.5h) surfaceData.albedo=half3(wardrobe,0);\n    InputData inputData;')
(out/'ApprovedWardrobeForward.hlsl').write_text(source)
license_path=package/'LICENSE.md'
if license_path.exists():shutil.copy2(license_path,out/'Unity-URP-LICENSE.md')
(out/'SOURCE.md').write_text('Based on the installed Unity URP package '+package.name+'. Lit lighting, shadows and depth passes are retained. Wardrobe regions are baked from our authored geometry into a UV0 texture; no extra GPU skin vertex streams are required. The forward albedo pass applies clothing and grey-hair colours. Source generator: Tools/YanYana/build_wardrobe_shader.py. Original package files are unchanged.\n')
print('WARDROBE SHADER WRITTEN',package.name)
