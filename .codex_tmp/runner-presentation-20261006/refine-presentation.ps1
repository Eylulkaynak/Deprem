$ErrorActionPreference='Stop'
$refineRoot=(Get-Location).Path
$refineStage=Join-Path $refineRoot '.codex_tmp\runner-presentation-20261006'
function Save-Refinement([string]$file,[string]$content) {
    $id=[Guid]::NewGuid().ToString('N')
    $temp=Join-Path $refineStage ($id+'.tmp')
    [IO.File]::WriteAllText($temp,$content,(New-Object Text.UTF8Encoding($false)))
    [IO.File]::Replace($temp,(Join-Path $refineRoot $file),(Join-Path $refineStage ($id+'.backup')))
}
$qa=[IO.File]::ReadAllText((Join-Path $refineRoot 'Assets/Editor/FiretruckRunnerQA.cs'))
$qa=$qa.Replace('using UnityEngine.UI;',"using UnityEngine.UI;`r`nusing UnityEngine.Rendering.Universal;")
$qa=$qa.Replace('        var previous=RenderTexture.active;var prior=camera.targetTexture;',@'
        var previous=RenderTexture.active;var prior=camera.targetTexture;
        var uiObjects=canvases.SelectMany(c=>c.GetComponentsInChildren<Transform>(true)).Distinct().ToArray();
        var originalLayers=uiObjects.Select(t=>t.gameObject.layer).ToArray();
        var originalMask=camera.cullingMask;
        Camera uiCamera=null;
'@)
$qa=$qa.Replace('            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}',@'
            // Match the live Overlay UI: render it after world grading, with no post-processing of text or panels.
            uiCamera=new GameObject("Runner QA overlay camera").AddComponent<Camera>();
            uiCamera.CopyFrom(camera);uiCamera.targetTexture=null;uiCamera.cullingMask=1<<31;
            uiCamera.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);
            var uiData=uiCamera.GetUniversalAdditionalCameraData();uiData.renderType=CameraRenderType.Overlay;uiData.renderPostProcessing=false;uiData.renderShadows=false;
            camera.GetUniversalAdditionalCameraData().cameraStack.Add(uiCamera);
            camera.cullingMask&=~(1<<31);
            foreach(var t in uiObjects)t.gameObject.layer=31;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=uiCamera;canvas.planeDistance=1;}
            foreach(var button in canvases.SelectMany(c=>c.GetComponentsInChildren<Button>(true)))
                button.targetGraphic.CrossFadeColor((button.IsInteractable()?button.colors.normalColor:button.colors.disabledColor)*button.colors.colorMultiplier,0,true,true);
'@)
$qa=$qa.Replace('            camera.targetTexture=prior;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);',@'
            if(uiCamera!=null){camera.GetUniversalAdditionalCameraData().cameraStack.Remove(uiCamera);Object.DestroyImmediate(uiCamera.gameObject);}
            camera.cullingMask=originalMask;
            for(int i=0;i<uiObjects.Length;i++)if(uiObjects[i]!=null)uiObjects[i].gameObject.layer=originalLayers[i];
            camera.targetTexture=prior;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
'@)
Save-Refinement 'Assets/Editor/FiretruckRunnerQA.cs' $qa
Save-Refinement 'Assets/Editor/StoryFiretruckRunnerPresentation.cs' ([IO.File]::ReadAllText((Join-Path $refineStage 'StoryFiretruckRunnerPresentation.cs')))
Write-Output 'Presentation refinements staged in Unity.'
