$ErrorActionPreference = 'Stop'
$presentationRoot = (Get-Location).Path
$presentationStage = Join-Path $presentationRoot '.codex_tmp\runner-presentation-20261006'
function Save-PresentationSource([string]$relative,[string]$content) {
    $destination = Join-Path $presentationRoot $relative
    $id = [Guid]::NewGuid().ToString('N')
    $temporary = Join-Path $presentationStage ($id + '.tmp')
    [IO.File]::WriteAllText($temporary,$content,(New-Object Text.UTF8Encoding($false)))
    if (Test-Path -LiteralPath $destination) { [IO.File]::Replace($temporary,$destination,(Join-Path $presentationStage ($id + '.backup'))) }
    else { [IO.File]::Move($temporary,$destination) }
}
function Replace-Required([string]$source,[string]$before,[string]$after) {
    if (-not $source.Contains($before)) { throw ('Expected source missing: ' + $before) }
    return $source.Replace($before,$after)
}
$builder = [IO.File]::ReadAllText((Join-Path $presentationRoot 'Assets/Editor/StoryFiretruckRunnerSceneBuilder.cs'))
$builder = Replace-Required $builder 'public static class StoryFiretruckRunnerSceneBuilder' 'public static partial class StoryFiretruckRunnerSceneBuilder'
$builder = Replace-Required $builder 'StoryChapterBuilderCommon.LoadPlayfulStoryFonts(out regular, out _, out bold);' 'LoadRunnerPresentationAssets();'
$builder = Replace-Required $builder 'BuildApo(root, manager);' "BuildApo(root, manager);`r`n        ApplyRunnerPresentation(root, manager);"
$builder = Replace-Required $builder 'importer.textureType=TextureImporterType.Sprite;importer.spriteBorder=' 'importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder='
$builder = Replace-Required $builder 'for (int div = -1; div <= 1; div += 2)' 'if (side < 0) for (int div = -1; div <= 1; div += 2)'
$hudStart = $builder.IndexOf('    private static void BuildHud(')
$hudEnd = $builder.IndexOf('    [MenuItem("Tools/Deprem Story/Refresh Apo Guide")]', $hudStart)
if ($hudStart -lt 0 -or $hudEnd -lt 0) { throw 'HUD method boundaries missing.' }
$builder = $builder.Remove($hudStart,$hudEnd-$hudStart)
# Keep the portrait-only refresh compatible with the compact guide layout.
$builder = Replace-Required $builder 'BuildApo(m.transform.root,m);' "BuildApo(m.transform.root,m);`r`n        var oldWindow=m.guidePanel.transform.Find(`"PortraitWindow`");`r`n        if(oldWindow!=null)Object.DestroyImmediate(oldWindow.gameObject);`r`n        PlaceGuidePortrait(m,m.guidePanel.GetComponentInChildren<UnityEngine.UI.RawImage>(true));"
$manager = [IO.File]::ReadAllText((Join-Path $presentationRoot 'Assets/Scripts/Story/FiretruckRunnerManager.cs'))
$manager = Replace-Required $manager 'recycledSections, tipIndex, lastBankedCoins' 'recycledSections, lastBankedCoins'
$manager = Replace-Required $manager 'guideUntil, nextTip;' 'guideUntil;'
$manager = Replace-Required $manager '            Say("Açık şeridi takip et, İMO coin topla. Hazırsan yola çıkalım!", 8f);' ''
$manager = Replace-Required $manager 'guidePanel != null && state == RunState.Driving && clock > guideUntil' 'guidePanel != null && clock > guideUntil'
$manager = Replace-Required $manager ' : "HADİ!";' ' : "Başla";'
$manager = Replace-Required $manager '{ SetState(RunState.Driving); nextTip = clock + 18f; }' '{ SetState(RunState.Driving); }'
$periodicStart = $manager.IndexOf('            if (clock >= nextTip && guideTips.Length > 0)')
$periodicEnd = $manager.IndexOf('            if (clock >= nextHud)', $periodicStart)
if ($periodicStart -lt 0 -or $periodicEnd -lt 0) { throw 'Periodic guide block missing.' }
$manager = $manager.Remove($periodicStart,$periodicEnd-$periodicStart)
$copyChanges = @(
    @('Say("Sağa / sola kaydır veya A / D kullan. Her turda bir TURBO hazır!", 6f);','Say("Şerit değiştirmek için kaydır veya ← → kullan.", 4f);'),
    @('Say(TurboActive ? "Turbo yolu açtı!" : "Kalkan darbeyi karşıladı!", 2.5f);','Say("Darbe engellendi.", 1.8f);'),
    @('shakeUntil = clock + .4f;','shakeUntil = clock + .22f;'),
    @('"Dikkat! Açık şeride geç. " + health + " canın kaldı."','health + " can kaldı. Boş şeride geç."'),
    @('"Güzel deneme! Garajda aracını güçlendirip tekrar çıkabiliriz.", 5f);','"Tur bitti.", 2.5f);'),
    @('Say("KALKAN! Bir darbeyi hasarsız karşılar.", 3f);','Say("Kalkan açık. Bir darbeyi karşılar.", 2.5f);'),
    @('Say("MIKNATIS! Yakındaki coinler sana geliyor.", 3f);','Say("Mıknatıs açık. Madalyalar sana gelir.", 2.5f);'),
    @('Say("TURBO! Hızlan ve engelleri aş!", 3f);','Say("Turbo açık. Bu sürede darbe almazsın.", 2.5f);'),
    @('Say("GÖREV TAMAM! +" + reward + " İMO garajına eklendi.", 4f);','Say("Hedef tamamlandı. +" + reward + " İMO", 2.5f);'),
    @('"YENİ REKOR!" : "BİR TUR DAHA?"','"Yeni rekor" : "Bu tur"'),
    @('"</size> PUAN\n"','"</size> puan\n"'),
    @('            Say("Coinlerini kalkan, mıknatıs ve turbo süresini uzatmak için kullanabilirsin.", 8f);',''),
    @('"Bu yükseltme için " + (cost - save.wallet) + " İMO daha topla."','(cost - save.wallet) + " İMO daha gerekiyor."'),
    @('"Yükseltme tamam! Sonraki turda güçlendirmelerin daha uzun sürer."','"Geliştirildi. Sonraki turda daha uzun sürer."'),
    @('"REKOR  " + save.bestScore','"Rekor  " + save.bestScore'),
    @('"Her turda 3 can + 1 hazır turbo. Coin topla, aracını geliştir!"','"3 can · 1 turbo hakkı. Topladığın madalyalarla süreleri uzat."'),
    @('"SV " + level + "/5  •  "','"Seviye " + level + "  ·  "'),
    @('"TAMAMLANDI" : UpgradeCost(level) + " İMO  •  GELİŞTİR"','"En üst seviye" : UpgradeCost(level) + " İMO  ·  Geliştir"'),
    @('streak >= 5 ? "SERİ " + streak : "AÇIK ŞERİDİ TAKİP ET"','streak >= 5 ? streak + " seri" : ""'),
    @('"KALKAN " + Mathf.CeilToInt(shieldUntil - clock) + "  "','"Kalkan " + Mathf.CeilToInt(shieldUntil - clock) + " sn   "'),
    @('"MIKNATIS " + Mathf.CeilToInt(magnetUntil - clock) + "  "','"Mıknatıs " + Mathf.CeilToInt(magnetUntil - clock) + " sn   "'),
    @('"TURBO " + Mathf.CeilToInt(turboUntil - clock)','"Turbo " + Mathf.CeilToInt(turboUntil - clock) + " sn"'),
    @('"GÖREV " + (challenge + 1) + "  •  " + ',''),
    @('" m yol al"','" m"'),
    @('"/15 coin topla"','" / 15 madalya"'),
    @('"/2 güçlendirme al"','" / 2 güçlendirme"'),
    @(') * .09f;',') * .045f;'),
    @('TurboActive ? 68f : 59f','TurboActive ? 63f : 59f')
)
foreach ($copyChange in $copyChanges) { $manager = Replace-Required $manager $copyChange[0] $copyChange[1] }
$manager = Replace-Required $manager '            state = value; pointerTracking = false;' "            state = value; pointerTracking = false;`r`n            if (guidePanel != null && value != RunState.Driving && value != RunState.Countdown) guidePanel.SetActive(false);"
$qa = [IO.File]::ReadAllText((Join-Path $presentationRoot 'Assets/Editor/FiretruckRunnerQA.cs'))
$qa = Replace-Required $qa 'Call(m,"Say","Ben Apo! Açık şeridi takip et; engeller yaklaşmadan yönünü seç.",10f);' 'Call(m,"Say","Kalkan açık. Bir darbeyi karşılar.",2.5f);'
$qa = Replace-Required $qa 'RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32)' 'RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4)'
$qa = Replace-Required $qa '        Capture("garage",1920,1080);' @'
        Need(!m.guidePanel.activeSelf,"Garage has no unsolicited guide speech",r);
        Need(m.missionFill.sprite!=null,"Mission progress has an imported sprite",r);
        Need(m.guidePanel.transform.Find("ApoName")==null,"Guide has no name or nickname label",r);
        Need(m.guidePanel.GetComponentInChildren<UnityEngine.UI.Mask>(true)!=null,"3D guide portrait is clipped to its frame",r);
        Need(m.shieldVisual.GetComponentsInChildren<LineRenderer>(true).Length==3&&m.turboVisual.GetComponentsInChildren<LineRenderer>(true).Length==2,"Shield and turbo use authored transparent ribbons",r);
        Capture("garage",1920,1080);
'@
$qa = Replace-Required $qa '        Capture("apo-guide",1920,1080);' @'
        Capture("apo-guide",1920,1080);Capture("guide-portrait",1080,1920);
        Call(m,"ApplyPower",FiretruckRunnerManager.PowerKind.Shield);
        Call(m,"ApplyPower",FiretruckRunnerManager.PowerKind.Turbo);
        Call(m,"AnimateScene",.016f);
        Capture("power-effects",1920,1080);Capture("power-effects-portrait",1080,1920);
        Set(m,"clock",Get<float>(m,"clock")+3f);Call(m,"Update");
        Need(!m.guidePanel.activeSelf,"Contextual guide closes after its short duration",r);
'@
Save-PresentationSource 'Assets/Editor/StoryFiretruckRunnerSceneBuilder.cs' $builder
Save-PresentationSource 'Assets/Scripts/Story/FiretruckRunnerManager.cs' $manager
Save-PresentationSource 'Assets/Editor/FiretruckRunnerQA.cs' $qa
Save-PresentationSource 'Assets/Editor/StoryFiretruckRunnerPresentation.cs' ([IO.File]::ReadAllText((Join-Path $presentationStage 'StoryFiretruckRunnerPresentation.cs')))
Write-Output 'Runner presentation sources updated.'
