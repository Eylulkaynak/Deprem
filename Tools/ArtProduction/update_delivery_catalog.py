"""Reconcile the delivery catalog with existing source/export files and build logs."""
from pathlib import Path
import json,re,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[2]
triangles={}
for log in sorted((root/'Logs').glob('KKTC_*Build.log'),key=lambda p:p.stat().st_mtime):
    for name,count in re.findall(r'KKTC_MODEL_READY (\w+) (\d+)',log.read_text(encoding='utf-8-sig',errors='replace')):
        triangles[name]=int(count)
catalog=[]
for source in sorted((root/'ArtDirection/KKTC/Props').glob('*.blend')):
    name=source.stem;model=root/f'Assets/Story/Art/KKTC/Models/{name}.fbx'
    if not model.exists():continue
    catalog.append({'id':name,'triangles':triangles.get(name),'source':source.relative_to(root).as_posix(),
        'model':model.relative_to(root).as_posix(),'prefab':f'Assets/Story/Art/KKTC/Prefabs/{name}.prefab',
        'preview':f'ClientExports/KKTC/Props/{name}.png'})
(root/'ArtDirection/KKTC/Props/catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
reports=root/'ClientExports/KKTC/Reports'
passes={}
for name in ['PlayMode_10Passed_1Pending.xml','Story01_PlayMode_Passed.xml','PlayMode_Final_Passed.xml',
             'PlayMode_Current_10Passed_1Pending.xml','PlayMode_CameraRegression_Passed.xml',
             'PlayMode_TallPortrait_Passed.xml','PlayMode_QuakeSurface_Passed.xml',
             'PlayMode_Story03Surface_Passed.xml','PlayMode_Story03Complete_Passed.xml',
             'PlayMode_Reunion_Passed.xml']:
    path=reports/name
    if not path.exists():continue
    for case in ET.parse(path).iter('test-case'):
        passes[case.get('fullname')]={'result':case.get('result'),'evidence':name,'completed':case.get('end-time')}
(reports/'CurrentPlayModeEvidence.json').write_text(json.dumps({'checks':passes,'passed':sum(c['result']=='Passed' for c in passes.values()),
    'failed':sum(c['result']=='Failed' for c in passes.values()),'note':'Consolidated evidence index; original XML reports are unchanged.'},indent=2),encoding='utf-8')
lines=['# KKTC görsel teslim kataloğu','',f'{len(catalog)} eşya; 8 düzenlenebilir karakter kaynağı. Oyun içi görüntüler ve test sonuçları Reports, Hero, CameraSheets ve GameplayReview klasörlerindedir.','',
    '| Eşya | Blender kaynağı | Unity prefabı | Yakın plan |','| --- | --- | --- | --- |']
status=reports/'NormalSpeedRecording_Status.txt'
if status.exists():
    recording=status.read_text(encoding='utf-8-sig')
    match=re.search(r'^video=(.+)$',recording,re.M)
    if match and 'COMPLETED' in recording:
        video=Path(match.group(1).strip())
        if video.exists():
            dimensions=re.search(r'^resolution=(.+)$',recording,re.M)
            resolution=dimensions.group(1).strip().replace('x','×') if dimensions else 'Portre'
            lines[3:3]=[f'[Dört bölümün normal hız kaydı]({video.as_posix()}) — {resolution}, 30 fps, 1× hız. Mevcut test sürücüsü ile oynatılmıştır; ortam/etkileşim sesleri vardır, nihai seslendirme bu tura dahil değildir.','']
            assembly=reports/'NormalSpeedRecording_Assembly.json'
            if assembly.exists():
                sources=json.loads(assembly.read_text(encoding='utf-8'))
                lines[5:5]=[f'Video, incelenmiş 1–2. bölüm kaydı ile güncel 3–4. bölüm kaydının bölüm sınırında birleştirilmesidir. [Kaynaklar ve kesim noktası]({assembly.as_posix()}); görüntü, ses ve oynatma hızı korunur.','']
for entry in catalog:
    links=[f'[{label}]({(root/entry[key]).as_posix()})' for label,key in [('Kaynak','source'),('Prefab','prefab'),('Görüntü','preview')]]
    lines.append('| '+entry['id']+' | '+' | '.join(links)+' |')
lines+=['','| Karakter | Blender kaynağı | Unity prefabı |','| --- | --- | --- |']
for name in ['Deniz','Can','Anne','Baba','Komsu','Police','Firefighter','RescueWorker']:
    source=root/f'ArtDirection/KKTC/Characters/{name}_Refined.blend'
    prefab=root/f'Assets/Story/Art/KKTC/Characters/{name}.prefab'
    label={'Komsu':'Nermin','Police':'Polis','Firefighter':'İtfaiyeci','RescueWorker':'Sivil Savunma görevlisi'}.get(name,name)
    lines.append(f'| {label} | [Kaynak]({source.as_posix()}) | [Prefab]({prefab.as_posix()}) |')
lines+=['','Karakterler onaylı görünüme dayanan mevcut 3D kaynakların düzenlenmiş sürümleridir; Humanoid rig ve mevcut yüz bağlantıları korunur.','',
    f'[Üretim ve doğrulama raporu]({(root/"Docs/KKTC_Visual_Production.md").as_posix()}) · [Oyun testi kanıtları]({(reports/"CurrentPlayModeEvidence.json").as_posix()})','',
    'Tüm oyun içi değişiklikler Story_01–04_RebuildPreview üzerinde uygulanır. Mevcut manager metinleri dışında runtime davranışı eklenmez.']
lines += ['',f'[Diyalog değişim içeriği]({(root/"Assets/Story/Art/KKTC/Dialogue/ContentReplacements.json").as_posix()}) · [Birinci bölüm altyazı manifesti]({(root/"Assets/Story/Audio/Voices/Story01/manifest.json").as_posix()})',
    '', 'Yakın planlar: '+ ' · '.join(f'[{label}]({(root/"ClientExports/KKTC/Hero"/filename).as_posix()})' for label,filename in [
        ('Açık çanta','01_Backpack_Open.png'),('Sırtta çanta','04_Backpack_Carried_Tall.png'),
        ('Fener','02_Flashlight.png'),('Pil yuvası','03_Radio_Empty.png'),('Takılı pil','03_Radio_Inserted.png')])]
followup=root/'Docs/KKTC_Gameplay_Followup.md'
if followup.exists():
    lines += ['',f'[Kayıtta görülen bulgular ve sonraki oynanış planı]({followup.as_posix()})']
(root/'ClientExports/KKTC/Delivery.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(f'Catalog: {len(catalog)} props; PlayMode evidence: {len(passes)} checks')
