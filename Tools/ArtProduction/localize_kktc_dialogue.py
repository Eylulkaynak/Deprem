"""Content-only replacement of existing Turkish dialogue. No new runtime behaviour."""
from pathlib import Path
import json,re

root=Path(__file__).resolve().parents[2]
manifest_path=root/'Assets/Story/Audio/Voices/Story01/manifest.json'
manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
new_lines={
'01_opening': 'Can: Arabam da gelsin mi?\nAnne: Önce gerekenler oğlum. Arabana sonra yer bakarız.\nDeniz: Bak, ilk kart mahalle parkı. Panoda yerini bulalım.',
'02_plan_park': 'Deniz: Park binalardan uzak. Buluşma yerimiz burası.\nAnne: Sırada Melek teyzenin kartı var. Ankara’da, adanın dışında yaşıyor.',
'03_plan_contact': 'Deniz: Melek teyzeye hepimiz haber vereceğiz. Ada dışındaki ortak kişimiz o.\nAnne: Şimdi Can’ın kartına bakalım; ona hangi işi ayırmıştık?',
'04_plan_whistle': 'Deniz: Sarsıntı sürerken çantaya koşmuyoruz.\nCan: Senin yanında kalıyorum. Sonra düdüğüm bende.\nAnne: Tamamdır. Planımızı da biliyoruz artık.',
'05_bag_opened': 'Anne: Önce feneri, radyoyu, pilleri bulalım. Elektrik giderse elimizin altında olsunlar.',
'06_signal_review': 'Anne: Piller ayrı poşette, düdük dış cepte. Elektrik kesilse de haber alabiliriz.',
'07_radio_tuned': 'Radyo: KKTC Sivil Savunma duyurularını takip edin. Doğrulanmamış haberleri paylaşmayın.\nCan: Düdüğü bana ayırmıştık!',
'08_food_intro': 'Can: Büyük şişede daha çok su var.\nDeniz: Var ama taşıyabilecek miyiz? Kapağına ve tarihine de bakalım.',
'09_water_checked': 'Deniz: Tarihi uygun, kapağı da sağlam.\nCan: Tamam, taşıyabildiğimiz şişeyi alalım.',
'10_food_review': 'Anne: Su sızdırmaz bölümde, yiyecek yanında. Çantayı da taşıyabileceğimiz kadar doldurduk.',
'11_health_intro': 'Can: Bu kutudakileri biz mi kullanacağız?\nAnne: Siz kapalı malzemeyi bulup yetişkine verin. Paketin kenarlarına bakalım önce.',
'12_bandage_checked': 'Deniz: Mührü açılmamış.\nAnne: Tamamdır. İlaç seçmiyoruz; kapalı malzemeyi sorumlu yetişkine veriyoruz.',
'13_health_review': 'Anne: İlk yardım malzemesi yetişkin gözetiminde kullanılır. Kimlik kopyalarıyla aile notunu da su geçirmez dosyaya koyalım.',
'14_warmth_review': 'Anne: İnce battaniye de tamam. Mevsime uygun kıyafet koyduk mu? Hadi, ağırlığını deneyelim.',
'15_console_reveal': 'Can: Konsolu da koydum ben.\nAnne: Bir kaldıralım bakalım, taşıyabilecek miyiz?',
'16_console_heavy': 'Deniz: Oof, ağır olmuş!\nCan: Konsol evde mi kalacak yani?\nAnne: Küçük araban gelir. Çantayı rahat taşıyabilmemiz lazım.',
'17_console_removed': 'Can: Konsol eve göz kulak olsun. Arabam bana yeter.\nDeniz: Hah, şimdi daha hafif.',
'18_comfort_choice': 'Anne: Küçük eşyan için yer açıldı.\nCan: Arabamı seçiyorum. Ötekiler evde kalsın.',
'19_comfort_chosen': 'Can: Arabam dış cepte dursun mu?\nDeniz: Dursun. Yanındayım ben de.',
'20_balanced_lift': 'Can: Şimdi rahat kalktı!\nAnne: Ağırlık dengeli. İki askıyı da omzuna al Deniz.',
'21_straps': 'Anne: İki askı omuzda, ellerin serbest. Ağır gelirse birlikte azaltırız.',
'22_finish': 'Anne: Çanta burada hazır dursun. Sarsıntı bitince birbirimizi kontrol eder, güvenle çıkarken alırız.',
'23_blackout_can_question': 'Can: Elektrikler giderse ne yapacağız?',
'24_blackout_deniz': 'Deniz: Buradayım Can. Fener de elimizde, seni görüyorum.',
'25_blackout_anne': 'Anne: Sakin olun çocuklar. Çıkışa koşmadan yolu kontrol edelim.',
'26_blackout_can_whistle': 'Can: Düdüğüm cebimde. Ayrılırsak üç kısa sesle yerimi belli ederim.'
}
saved_mapping=root/'Assets/Story/Art/KKTC/Dialogue/ContentReplacements.json'
mapping={e['before']:e['after'] for e in json.loads(saved_mapping.read_text(encoding='utf-8'))['entries']} if saved_mapping.exists() else {}
for entry in manifest['entries']:
    revised=new_lines[entry['id']]
    mapping[entry['subtitle']]=revised
    entry['subtitle']=revised
    entry['segments']=[{'speaker':line.split(':',1)[0].lower().replace('ı','i'),'text':line.split(':',1)[1].strip()} for line in revised.split('\n')]
manifest['disclosure']='KKTC altyazı metinleri güncellendi. Önceki prototip sesler bu metinlerle kullanılmaz; yeni seslendirme bekleniyor.'
manifest['voiceStatus']='pending_new_recording'

mapping.update({
'Deniz: Açıldı! Elektrik kesilirse karanlıkta yolu bununla görebiliriz.\n':'Deniz: Yandı! Düğmesi de burada.\n',
'Anne: Evet. Mum yerine el feneri kullanır, yedek pilini de yanında tutarız.':'Anne: Tamamdır. Mum yerine fener, yanında da yedek pil.',
'Radyo: Acil durumlarda doğrulanmamış bilgileri paylaşmayın; resmî duyuruları takip edin.\n':'Radyo: KKTC Sivil Savunma duyurularını takip edin. Doğrulanmamış haberleri paylaşmayın.\n',
'Anne: Radyo çalışıyor. Şimdi kapatıp pili ayrı tutalım; böylece çantada boş yere tükenmez.':'Anne: Yayın geldi. Kapatıp pili ayrı koyalım; çantada boşuna tükenmesin.',
'Deniz: Yayını duydum; radyo çalışıyor.\n':'Deniz: Çalıştığını gördük.\n',
'Anne: Güzel. Aynı pili masaya geri aldık; şimdi radyoyu ve pili sırayla çantaya koyabiliriz.':'Anne: Pili de geri aldık. İkisini sırayla çantaya koyalım.',
'Can’ın arabası ayakkabıya çarpıp durdu.\nCan: Araba geçemedi.\nDeniz: Biz de karanlıkta aynı yerde takılırız.':'Can: Araba yine ayakkabıya takıldı.\nDeniz: Biz de karanlıkta takılırız burada. Yolu açalım.',
"Can'ın arabası ayakkabıya çarpıp durdu.\nCan: Araba geçemedi.\nDeniz: Biz de karanlıkta aynı yerde takılırız.":'Can: Araba yine ayakkabıya takıldı.\nDeniz: Biz de karanlıkta takılırız burada. Yolu açalım.',
'Nermin: Apartmanın yeni tahliye planını getirdim.\nCan: Asansör niye çizilmemiş?\nNermin: Sarsıntıdan sonra merdiven kullanılır; yolu açık tutan biri bana yeter.':'Nermin: Komşular, apartmanın planını getirdim.\nCan: Asansör nerede?\nNermin: Sarsıntıdan sonra merdivenden ineriz oğlum. Önüm açık olsun, yeter.',
"Deniz zarfı Nermin'in eline verdi. İçinden apartmanın yeni tahliye planı çıktı.\nNermin: Matkap sesini duyarsam artık nedenini bilirim.":'Nermin: Sağ ol Deniz. Planı panoya koyalım.\nAnne: Hazır gelmişken kahven de var komşum.',
'Plan aile panosunun yanına yerleşti. Asansör işareti yoktu; merdiven rotası ve Nermin’in dairesi aynı çizgide görünüyordu.':'Deniz: Bizim daire, Nermin teyzenin dairesi, merdiven… Hepsi burada.',
'Anne rafı aynı noktadan hafifçe sınadı. Önceki sallanma yoktu; ağır kitaplar da artık alt bölmedeydi.':'Anne: Hah, şimdi sağlam. Ağır kitaplar da aşağıda.',
'Anne aynı kontrollü testi tekrarladı. Kayış gerildi, dolap duvardan ayrılmadı.\nDeniz: Risk aynı yerdeydi; davranışı değişti.':'Anne: Kayış tuttu, dolap duvardan ayrılmıyor.\nDeniz: Önceki gibi sallanmıyor artık.',
'Deniz bağlantı parçasını uzattı. Anne rafı duvar dikmesine sabitledi; çocuklar matkap veya ağır rafla uğraşmadı.':'Deniz: Parça burada anne.\nAnne: Sağ ol. Sabitlemeyi ben yapıyorum; siz biraz geride durun.',
'Anne kayışı iki noktadan sabitledi ve dolabı yeniden kontrol etti. Ağır işi yetişkin yaptı; Deniz güvenli mesafede kaldı.':'Anne: İki bağlantı da tamam. Bir daha kontrol edeyim, siz orada durun.',
"Çanta giriş rafındaydı. Can'ın oyuncak arabası yine kapıya giderken bir şeye takıldı.\nAnne: Bu kez arabaya değil, yoluna bakın.":'Can: Yahu, arabam yine geçemedi!\nAnne: Kapının önüne bakalım bir. Çanta hazır ama yolumuz açık mı?',
'Nermin kapıdan çıkarken bastonunu açık kalan çizginin üstüne koydu.\nNermin: Arabaya yetişemem ama sizi aşağıda yakalarım.':'Nermin: Bak, bastonum da takılmadan geçiyor. Elinize sağlık çocuklar.',
'Can: Tavan sustu.  Deniz: Merdiven yolu açık; asansörün paneli karanlık. Önce kapıyı kontrol edelim.':'Can: Ses kesildi.\nDeniz: Birlikte kalalım. Merdiven kapısını kontrol edeceğim.',
'Nermin: Deniz, Can! Buradayım; acele etmeyin, basamakların iç tarafında kalın.':'Nermin: Çocuklar, buradayım! Ağır ağır gelin; basamakların iç yanında kalın.',
'Nermin: İyiyim. Bastonum kutunun arkasında kaldı, önümdeki hafif parçalar da geçişi daraltıyor. Ağır dolaba dokunmayın.':'Nermin: İyiyim oğlum. Bastonum kutunun arkasında. Hafif parçaları kenara alabiliriz; ağır dolaba dokunmayın.',
'Deniz Nermin teyzenin kolunu çekmedi; bastonunu kavramasını bekleyip yanında yürüdü. Can merdivenin iç tarafında yolu açık tuttu.':'Deniz: Bastonunuz elinizde mi Nermin teyze? Yanınızdan geliyorum.\nNermin: Tamamdır oğlum, acelemiz yok.',
'Aile artık aynı kadrajdaydı. Deniz, Can, Anne ve Baba alandan ayrılmadan resmî duyuruyu dinledi; Nermin teyze görevlinin yanında güvendeydi.':'Baba: Buradasınız ya, içim rahatladı.\nAnne: Birlikte kalalım. Sivil Savunma görevlisinin duyurusunu dinleyelim.\nCan: Nermin teyze de burada.',
'Can: Tam oturdu! Tekerlek dönüyor.':'Can: Hah, şimdi oldu! Tekerlek dönüyor.',
'Deniz radyonun sesini kısıyor. Mutfaktan tabak ve çatal sesleri geliyor; kimse birazdan olacakları bilmiyor.':'Anne: Çocuklar, sofrayı kuruyorum.\nDeniz: Tamam anne, radyonun sesini de kıstım.'
})

mapping.update(json.loads((root/'Tools/ArtProduction/kktc_dialogue_additions.json').read_text(encoding='utf-8')))

for key in mapping:
    visited={key}
    while mapping[key] in mapping and mapping[key] not in visited:
        visited.add(mapping[key]);mapping[key]=mapping[mapping[key]]

# Exact C# string tokens only. Identifiers, control flow, timings and state flags are untouched.
token=re.compile(r'"(?:\\.|[^"\\])*"')
counts={}
files=[root/'Assets/Scripts/Story'/f for f in ['StoryPreparationDirector.cs','StoryHomeSafetyDirector.cs','StoryEvacuationDirector.cs','StorySequenceDirector.cs']]
files += [root/'Assets/Editor'/f for f in ['StoryPreparationRebuildPreviewBuilder.cs','StoryHomeSafetyRebuildPreviewBuilder.cs','StoryQuakeRebuildPreviewBuilder.cs','StoryEvacuationRebuildPreviewBuilder.cs']]
for file in files:
    source=file.read_text(encoding='utf-8-sig'); count=[0]
    def replace(match):
        try: value=json.loads(match.group())
        except ValueError: return match.group()
        if value not in mapping: return match.group()
        count[0]+=1
        return json.dumps(mapping[value],ensure_ascii=False)
    revised=token.sub(replace,source)
    if revised!=source: file.write_text(revised,encoding='utf-8')
    counts[str(file.relative_to(root))]=count[0]
manifest_path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
output=root/'Assets/Story/Art/KKTC/Dialogue'
output.mkdir(parents=True,exist_ok=True)
(output/'ContentReplacements.json').write_text(json.dumps({'entries':[{'before':k,'after':v} for k,v in mapping.items()]},ensure_ascii=False,indent=2),encoding='utf-8')
(root/'ClientExports/KKTC/Reports/DialogueMigration.json').write_text(json.dumps(counts,indent=2),encoding='utf-8')
print(json.dumps(counts,indent=2))
