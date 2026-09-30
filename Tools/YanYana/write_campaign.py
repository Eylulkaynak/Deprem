"""Author the campaign. JSON is baked into scene graphs by the Editor builder."""
import json
import pathlib

ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/YanYana/Content'
OUT.mkdir(parents=True,exist_ok=True)
beats=[]

def action(label,model='FamilyCard',gesture='tap',next='',flags='',target='Table',effect='',safe=True):
    return dict(label=label,model=model,gesture=gesture,next=next,flags=flags,target=target,effect=effect,safe=safe)

def beat(id,chapter,station,title,line,*actions,role='Ada',camera='close',condition='',yes='',no='',effect=''):
    beats.append(dict(id=id,chapter=chapter,station=station,title=title,line=line,role=role,camera=camera,
        actions=list(actions),condition=condition,yes=yes,no=no,effect=effect))

def a(label,model='FamilyCard',gesture='tap',**kwargs):return action(label,model,gesture,**kwargs)

# 0. A lived-in opening and an optional practiced family plan.
beat('welcome',0,'living','Efe’nin yanına git','Efe: Uçurtmamızın kuyruğu nerede?',a('Efe’ye yaklaş','ComfortFox','approach'),camera='wide')
beat('kite_ribbon',0,'living','Kurdeleyi uçurtmaya tak','Ada: Buldum! Birlikte uçuralım.',a('Kurdeleyi tak','SafetyStrap','drag',target='ComfortFox'))
beat('family_photo',0,'living','Ailene katıl','Derya: Önce mahalle planımıza bakalım.',a('Aileye yaklaş','FamilyCard','approach'),camera='wide')
beat('plan_choice',0,'plan','Buluşma yerini tanı','Emre: Ayrılırsak bu işaretin yanında buluşalım.',
     a('Planı dene','FamilyCard',next='plan_place'),a('Çantaya bak','BackpackClosed',next='bag_open',flags='FamilyPlan=0'))
beat('plan_place',0,'plan','Aile işaretini plana yerleştir','Efe: Büyük turkuaz ağacı gördüm!',a('İşareti yerleştir','FamilyCard','drag',target='Plant'))
beat('plan_rehearse',0,'plan','Efe’ye buluşma yerini göster','Ada: Sen göster, ben takip edeyim.',a('Birlikte göster','Plant','hold',flags='FamilyPlan=1'))

# 1. Packing is functional inspection and spatial manipulation, with real omissions.
beat('bag_open',1,'bag','Çantayı aç','Derya: Hazırladığımız şeyleri birlikte deneyelim.',a('Fermuarı çek','BackpackClosed','drag',target='BackpackOpen'))
beat('flashlight_choice',1,'bag','Feneri incele','Efe: Işığı yanıyor mu?',
     a('Pili kontrol et','Flashlight',next='flashlight_battery'),a('Çantaya koy','Flashlight','drag',next='radio_choice',flags='FlashlightReady=0',target='BackpackOpen'))
beat('flashlight_battery',1,'bag','Pili fenerin yuvasına yerleştir','Ada: Pil yerinde. Şimdi deneyelim.',a('Pili yerleştir','Battery','drag',target='Flashlight'))
beat('flashlight_test',1,'bag','Feneri çalıştır','Efe: Işığı gördüm!',a('Düğmeye bas','Flashlight','hold',flags='FlashlightReady=1',effect='light_on'))
beat('flashlight_pack',1,'bag','Feneri dış cebe koy','Ada: Gerektiğinde kolayca ulaşabiliriz.',a('Cebe yerleştir','Flashlight','drag',target='BackpackOpen'))
beat('radio_choice',1,'radio','Radyoyu dene','Emre: Bilgiyi nereden alacağımızı da düşünelim.',
     a('Pilleri tak','Battery','drag',target='Radio',next='radio_tune'),a('Sonra bakarız','Radio','drag',target='BackpackOpen',flags='RadioReady=0',next='water_find'))
beat('radio_tune',1,'radio','Yayını bul','Radyo: Mahalle hazırlık günü başladı.',a('Düğmeyi çevir','Radio','drag',target='Radio',effect='radio_tune'))
beat('radio_test',1,'radio','Yayını birlikte dinle','Efe: Sesini şimdi duyabiliyorum.',a('Yayını doğrula','Radio','hold',flags='RadioReady=1'))
beat('radio_pack',1,'radio','Radyoyu çantaya yerleştir','Ada: Radyo da hazır.',a('Radyoyu yerleştir','Radio','drag',target='BackpackOpen'))
beat('water_find',1,'kitchen','Suyu seç','Derya: Etiketine ve kapağına bakalım.',a('Suyu incele','Water',next='water_check'),a('Hızlıca yerleştir','Water','drag',target='BackpackOpen',flags='WaterReady=0',next='food_pick'))
beat('water_check',1,'kitchen','Tarih etiketini çevir','Ada: Etiketi şimdi okuyabiliyoruz.',a('Etiketi göster','Water','drag',target='Water'))
beat('water_seal',1,'kitchen','Kapağı kontrol et','Efe: Bu şişenin kapağı kapalı.',a('Kapağı incele','Water','hold',flags='WaterReady=1'))
beat('water_pack',1,'bag','Suyu dik yerleştir','Ada: Yan cebine sığdı.',a('Suyu yerleştir','Water','drag',target='BackpackOpen'))
beat('food_pick',1,'kitchen','Kapalı yiyecek paketini seç','Derya: Bozulmamış, kapalı paketi alalım.',a('Kapalı paket','Food','drag',target='BackpackOpen',flags='FoodReady=1'),a('Açık paket','Parcel',safe=False,effect='unsafe_packet'))
beat('aid_choice',1,'aid','İlk yardım setine bak','Ada: Paketin koruması sağlam mı?',a('Mührü kontrol et','Bandage','hold',next='aid_pack',flags='AidReady=1'),a('Kontrol etmeden koy','FirstAid','drag',target='BackpackOpen',next='blanket_pack',flags='AidReady=0'))
beat('aid_pack',1,'aid','Seti çantaya yerleştir','Derya: Gerektiğinde bir yetişkin kullanacak.',a('Seti yerleştir','FirstAid','drag',target='BackpackOpen'))
beat('blanket_pack',1,'bag','Battaniyeyi katla','Efe: Böyle daha az yer kaplıyor.',a('Battaniyeyi katla','Blanket','drag',target='BackpackOpen',flags='BlanketReady=1'))
beat('card_pack',1,'plan','Aile kartını güvenli cebe koy','Emre: Gerekirse bu kartı görevliye gösterebilirsiniz.',a('Kartı yerleştir','FamilyCard','drag',target='Documents',flags='ContactCard=1'))
beat('whistle_find',1,'bag','Düdüğü Efe’ye ver','Efe: Aile işaretimizi hatırlıyorum.',a('Düdüğü ver','Whistle','drag',target='ComfortFox',flags='WhistleReady=1'))
beat('comfort_choice',1,'bag','Efe’nin küçük eşyasını seç','Efe: Tilkim ya da aile fotoğrafımız gelebilir.',a('Küçük tilki','ComfortFox','drag',target='BackpackOpen',flags='Comfort=1'),a('Aile fotoğrafı','FamilyCard','drag',target='BackpackOpen',flags='Comfort=2'))
beat('bag_balance',1,'bag','Çantayı dengeli yerleştir','Ada: Ağır parçayı aşağıya alalım.',a('Suyu aşağı al','Water','drag',target='BackpackOpen'))
beat('bag_close',1,'bag','Çantanın fermuarını kapat','Efe: Dış cebi açıkta kalmasın.',a('Fermuarı kapat','BackpackOpen','drag',target='BackpackClosed'))
beat('bag_lift',1,'bag','Çantayı kaldırıp dene','Ada: Taşıyabiliyorum. Askısını da ayarlayayım.',a('Çantayı kaldır','BackpackClosed','hold'))
beat('bag_fit',1,'bag','Askıyı kendine göre ayarla','Derya: Rahat taşıyabilmen önemli.',a('Askıyı çek','SafetyStrap','drag',target='BackpackClosed',flags='BagReady=1'))
beat('bag_stage',1,'door','Çantayı çıkış yanına bırak','Ada: Buradan kolayca alabiliriz.',a('Rafa yerleştir','BackpackClosed','drag',target='Shelf'),camera='wide')

# 2. Independent preparation flags are recalled by authored world state.
beat('safety_look',2,'living','Odaya Efe’yle bak','Efe: Çıkışa yürürken neler karşımıza çıkıyor?',a('Yolu birlikte incele','ComfortFox','approach'),camera='wide')
beat('exit_choice',2,'door','Geçiş yolunu kontrol et','Ada: Ayakkabılar ve kutu yolu daraltıyor.',a('Yolu aç','ShoePair',next='exit_shoes'),a('Rafa bakalım','Shelf',flags='ExitCleared=0',next='shelf_choice'))
beat('exit_shoes',2,'door','Ayakkabıları yerine koy','Efe: Şimdi burası daha geniş.',a('Ayakkabıları taşı','ShoePair','drag',target='Shelf'))
beat('exit_box',2,'door','Hafif kutuyu kenara al','Ada: Kapının önünü açık bırakalım.',a('Kutuyu taşı','ToyBox','drag',target='Shelf'))
beat('exit_test',2,'door','Açılan yolu dene','Efe: Birlikte rahatça geçebiliyoruz.',a('Eşiğe yürü','FamilyCard','approach',flags='ExitCleared=1'),camera='wide')
beat('shelf_choice',2,'shelf','Rafın çevresine bak','Derya: Sabitlemeyi ben yapabilirim.',a('Yetişkine haber ver','Bracket',next='shelf_books'),a('Dolaba bak','Wardrobe',flags='ShelfSecured=0',next='wardrobe_choice'))
beat('shelf_books',2,'shelf','Hafif kitapları aşağıya yerleştir','Ada: Ağır işleri anneme bırakıyorum.',a('Hafif paketi taşı','Documents','drag',target='Shelf'))
beat('shelf_bracket',2,'shelf','Sabitleme parçasını Derya’ya ver','Derya: Teşekkür ederim. Ben sabitliyorum.',a('Parçayı ver','Bracket','drag',target='FamilyCard',flags='ShelfSecured=1',effect='adult_secure'))
beat('reading_corner',2,'shelf','Efe’nin minderini güvenli yere taşı','Efe: Kitabımı burada okuyabilirim.',a('Minderi taşı','Cushion','drag',target='Table'))
beat('wardrobe_choice',2,'wardrobe','Dolap için destek iste','Emre: Bu dolaba birlikte müdahale etmeyelim.',a('Emre’ye göster','SafetyStrap',next='wardrobe_strap'),a('Komşuya git','WalkingCane',flags='WardrobeSecured=0',next='neighbor_intro'))
beat('wardrobe_strap',2,'wardrobe','Kayışı yetişkine uzat','Emre: Sabitlemeyi ben tamamlayacağım.',a('Kayışı ver','SafetyStrap','drag',target='FamilyCard',flags='WardrobeSecured=1',effect='adult_secure'))
beat('wardrobe_space',2,'wardrobe','Dolabın önünü boş bırak','Ada: Efe’nin oyuncakları başka yerde durabilir.',a('Oyuncağı taşı','ComfortFox','drag',target='ToyBox'))
beat('neighbor_intro',2,'door','Yusuf Amca’yı karşıla','Yusuf: Merdivende gerektiğinde birbirimize haber verelim.',a('Kapıya yaklaş','WalkingCane','approach'),camera='wide')
beat('neighbor_cane',2,'door','Bastonu Yusuf’a uzat','Efe: Bastonun burada kalmış.',a('Bastonu uzat','WalkingCane','drag',target='FamilyCard'))
beat('home_ready',2,'living','Hazırlığını Efe’ye göster','Ada: Hazırladıklarımızı artık nasıl kullanacağımızı biliyoruz.',a('Birlikte bak','FamilyCard','approach'),camera='wide')

# 3. Quake is a separate next-day event. Protection is nearby, never cross-room running.
beat('next_day',3,'living','Efe’nin yanına otur','Ertesi gün. Efe: Tilkime bir ev yapalım mı?',a('Masaya yaklaş','ComfortFox','approach'),camera='wide',effect='next_day')
beat('toy_build',3,'table','Küçük çatıyı yerleştir','Ada: Bu parça buraya uyuyor.',a('Parçayı yerleştir','Parcel','drag',target='ToyBox',effect='quake_start'))
beat('crouch',3,'cover','Olduğun yerde çök','Ada: Efe, ben buradayım. Birlikte korunuyoruz.',a('Çök','Cushion','swipe'),a('Kapıya koş','FamilyCard',safe=False,effect='unsafe_quake'),camera='cover')
beat('cover_head',3,'cover','Başını ve enseni koru','Efe: Yanındayım.',a('Başını koru','ComfortFox','swipe'),a('Pencereye git','Water',safe=False,effect='unsafe_quake'),camera='cover')
beat('hold_table',3,'cover','Masa ayağına tutun','Ada: Sarsıntı bitene kadar buradayız.',a('Tutun','Table','hold',effect='quake_hold'),camera='cover')
beat('hold_after',3,'cover','Konumunu koru','Efe: Masa hareket ediyor. Tutunmayı sürdürüyorum.',a('Tutunmayı sürdür','Table','hold'),camera='cover',effect='quake_end')
beat('sibling_choice',3,'table','Efe’yi kontrol et','Efe: Biraz korktum.',a('Efe’yi dinle','ComfortFox','hold',flags='SiblingSupported=1'),a('Yolu birlikte incele','FamilyCard',flags='SiblingSupported=0'))
beat('light_route',3,'living','','',condition='FlashlightReady',yes='use_flashlight',no='find_emergency_light')
beat('use_flashlight',3,'bag','Feneri dış cepten çıkar','Ada: Önceden denediğimiz fener işe yarıyor.',a('Feneri çıkar','Flashlight','drag',target='BackpackOpen',next='room_after',effect='light_on'))
beat('find_emergency_light',3,'door','Acil aydınlatmayı bul','Efe: Kapının yanında bir ışık görüyorum.',a('Işığa yaklaş','Flashlight','approach',next='emergency_light_on'),camera='wide')
beat('emergency_light_on',3,'door','Acil ışığı kendine çevir','Ada: Bu ışıkla önümüzü görebiliyoruz.',a('Işığı yönelt','Flashlight','drag',target='FamilyCard',next='room_after',effect='light_on'))
beat('room_after',3,'living','Odadaki değişikliği incele','Ada: Eşyaların çevresine dikkat edelim.',a('Geçişe bak','Shelf','approach'),camera='wide',effect='apply_damage')
beat('exit_route',3,'door','','',condition='ExitCleared',yes='shoes_after',no='exit_support')
beat('exit_support',3,'door','Güvenli taraftan yardım iste','Derya: Ağır engeli ben kontrol edeceğim.',a('Yetişkine haber ver','FamilyCard','hold',next='shoes_after',effect='adult_clear'))
beat('shoes_after',3,'door','Ayakkabılarını giy','Ada: Zemine dikkat ederek hazırlanıyoruz.',a('Ayakkabıları giy','ShoePair','drag',target='Cushion'))
beat('bag_after',3,'door','Erişilebilir çantanı al','Efe: Çanta çıkışın yanında.',a('Çantayı al','BackpackClosed','hold',effect='wear_bag'))
beat('home_leave',3,'door','Kapıyı açıp çevreye bak','Ada: Sarsıntı durdu. Çevremizi kontrol ediyoruz.',a('Eşiğe yaklaş','FamilyCard','approach'),camera='wide')

# 4. Evacuation with meaningful alternative assistance and report decisions.
beat('corridor',4,'corridor','Koridoru birlikte incele','Efe: Merdiven işareti bu tarafta.',a('İşarete yaklaş','FamilyCard','approach'),camera='wide')
beat('elevator',4,'corridor','Merdiven yolunu seç','Ada: Merdiven işaretini takip edelim.',a('Merdivene yürü','FamilyCard','approach'),a('Asansöre dokun','Radio',safe=False,effect='unsafe_elevator'),camera='wide')
beat('landing',4,'stairs','Sahanlığa dikkatle ilerle','Efe: Birlikte gidelim.',a('Sahanlığa yürü','ComfortFox','approach'),camera='wide')
beat('aftershock',4,'stairs','Dur ve başını koru','Ada: Yeniden sallanıyor. Olduğumuz yerde korunuyoruz.',a('Korun','Cushion','hold'),a('Koşmaya devam et','ShoePair',safe=False,effect='unsafe_quake'),effect='aftershock')
beat('stairs_check',4,'stairs','Sarsıntı sonrası yolu kontrol et','Ada: Önümüzdeki boş geçişi görüyorum.',a('Geçişi incele','FamilyCard','hold'),camera='wide')
beat('neighbor_choice',4,'neighbor','Yusuf Amca’ya nasıl yardım edelim?','Yusuf: Önce neye ihtiyacım olduğunu konuşalım.',a('Sor ve yanında kal','WalkingCane',next='neighbor_ask',flags='NeighborTogether=1'),a('Görevliye haber ver','Radio',next='neighbor_report',flags='NeighborTogether=0'),camera='wide')
beat('neighbor_ask',4,'neighbor','Yusuf Amca’yı dinle','Yusuf: Bastonuma ulaşmam ve yolun açılması yeterli.',a('Birlikte konuş','FamilyCard','hold'))
beat('neighbor_light_box',4,'neighbor','Hafif kutuyu kenara taşı','Ada: Ağır eşyalara dokunmadan yolu açıyorum.',a('Hafif kutuyu taşı','Parcel','drag',target='ToyBox'))
beat('neighbor_return_cane',4,'neighbor','Bastonu Yusuf’a ver','Yusuf: Teşekkür ederim. Birlikte ilerleyebiliriz.',a('Bastonu ver','WalkingCane','drag',target='FamilyCard',next='building_exit'))
beat('neighbor_report',4,'neighbor','Yusuf’un yerini görevliye göster','Bora: Ekibimiz ona destek olacak.',a('Konumu göster','FamilyCard','drag',target='Radio'))
beat('neighbor_wait_ack',4,'neighbor','Yardımın ulaştığını doğrula','Ada: Görevli Yusuf Amca’nın yanında.',a('Görevliye bak','Radio','hold',next='building_exit'),camera='wide')
beat('building_exit',4,'front','Bina önünden açık alana ilerle','Efe: Açık kaldırım bu tarafta.',a('Açık alana yürü','FamilyCard','approach'),camera='wide')
beat('street_hazard',4,'street','Sokaktaki geçişleri incele','Ada: O cephede gevşek bir parça görüyorum.',a('Görevliye göster','Radio',flags='FacadeReported=1'),a('Açık geçişi izle','FamilyCard','approach',flags='FacadeReported=0'),camera='wide')
beat('glass_boundary',4,'street','Camlı sınırdan uzak geç','Efe: Buradaki boş yolu kullanabiliriz.',a('Açık yolu takip et','Cone','approach'),a('Camların yanına git','Water',safe=False,effect='unsafe_glass'),camera='wide')
beat('emergency_lane',4,'street','Ekiplerin geçişini açık bırak','Ada: Araç yolunun dışından ilerliyoruz.',a('Yaya tarafına geç','Cone','drag',target='FamilyCard'),camera='wide')
beat('meet_idil',4,'fire_entry','Güvenli taraftaki İdil’e yaklaş','İdil: Buradan gördüğünüzü anlatabilirsiniz.',a('İdil’e yaklaş','HoseNozzle','approach'),camera='wide')

# 5. The player visibly becomes the adult professional, then returns to Ada.
beat('fire_report',5,'fire_entry','Dumanın yerini göster','Ada: Duman şu ahşap bölümden geliyor.',a('Bölümü işaret et','FamilyCard','drag',target='Parcel'))
beat('idil_role',5,'fire','İdil’in görevine katıl','Şimdi İdil. Çocuklar güvenli alanda bekliyor.',a('Ekipmanını kontrol et','HoseNozzle','hold'),role='Idil',camera='wide',effect='role_idil')
beat('fire_hose',5,'fire','Hortum bağlantısını kontrol et','İdil: Ekibim güvenli müdahale alanını hazırladı.',a('Bağlantıyı kontrol et','HoseNozzle','drag',target='Radio'),role='Idil')
beat('fire_position',5,'fire','İlk müdahale noktasına geç','İdil: Önce bize yakın olan odağı kontrol ediyorum.',a('İlk noktaya ilerle','Cone','approach'),role='Idil',camera='wide')
beat('fire_group_1',5,'fire','Suyu ilk odağa yönlendir','İdil: Parmağını sürükle, suyu yönlendir.',a('Suyu yönlendir','HoseNozzle','spray'),a('Ekiple birlikte','Radio',next='fire_assistance',flags='FireAssisted=1'),role='Idil',effect='fire_1')
beat('fire_reposition',5,'fire','İkinci noktaya geç','İdil: İlk bölüm söndü. Konumumu değiştiriyorum.',a('İkinci noktaya geç','Cone','approach'),role='Idil',camera='wide')
beat('fire_group_2',5,'fire','Yandaki odağı kontrol et','İdil: Yeni açıdan daha iyi görebiliyorum.',a('Suyu yönlendir','HoseNozzle','spray'),a('Ekiple birlikte','Radio',next='fire_assistance',flags='FireAssisted=1'),role='Idil',effect='fire_2')
beat('fire_inspect',5,'fire','Kalan sıcak bölümü incele','İdil: Son kontrolde küçük bir odak gördüm.',a('Bölümü kontrol et','Flashlight','hold'),role='Idil')
beat('fire_group_3',5,'fire','Son odağı söndür','İdil: Ekibimle son kontrolü tamamlıyorum.',a('Suyu yönlendir','HoseNozzle','spray',flags='FireAssisted=0',next='fire_safe'),a('Ekiple birlikte','Radio',next='fire_assistance',flags='FireAssisted=1'),role='Idil',effect='fire_3')
beat('fire_assistance',5,'fire','Ekip arkadaşına alanı göster','İdil: Birlikte tamamlıyoruz. Bu yol kontrol için kapalı.',a('Kalan bölümü göster','Radio','drag',target='Parcel',next='fire_safe'),role='Idil',effect='fire_team')
beat('fire_safe',5,'fire','Müdahale sınırını işaretle','İdil: Geçiş kararını ekip kontrolünden sonra veriyoruz.',a('Sınırı yerleştir','Cone','drag',target='FamilyCard'),role='Idil',camera='wide')
beat('ada_return',5,'fire_entry','Efe’nin yanına dön','Ada: Ekibin gösterdiği yoldan gidebiliriz.',a('Efe’ye yaklaş','ComfortFox','approach'),camera='wide',effect='role_ada')

# 6. Needs and information are physical tasks at the aid point.
beat('assembly_arrive',6,'assembly','Yardım noktasına ulaş','Bora: Hoş geldiniz. Birlikte yardımcı olalım.',a('Bora’ya yaklaş','FamilyCard','approach'),camera='wide')
beat('bora_role',6,'aidpoint','İhtiyaçları birlikte belirle','Şimdi Bora. Önce insanları dinliyoruz.',a('İhtiyaçları dinle','FamilyCard','hold'),role='Bora',effect='role_bora')
beat('water_route',6,'aidpoint','','',role='Bora',condition='WaterReady',yes='use_packed_water',no='supply_water')
beat('use_packed_water',6,'aidpoint','Hazırlanan suyu teslim et','Bora: Kontrol edilen şişe hazır.',a('Suyu teslim et','Water','drag',target='FamilyCard',next='blanket_help'),role='Bora')
beat('supply_water',6,'aidpoint','Eksik suyu yardım noktasından al','Bora: Buradaki kapalı şişeleri kullanabiliriz.',a('Suyu raftan al','Water','drag',target='BackpackOpen'),role='Bora')
beat('supply_water_deliver',6,'aidpoint','Suyu ihtiyaç noktasına götür','Bora: İhtiyacını sordum. Şişeyi ulaştırıyorum.',a('Suyu teslim et','Water','drag',target='FamilyCard',next='blanket_help'),role='Bora')
beat('blanket_help',6,'aidpoint','İstenen battaniyeyi ulaştır','Bora: Üşüyen komşumuza battaniye getirelim.',a('Battaniyeyi ver','Blanket','drag',target='Cushion'),role='Bora')
beat('aid_route',6,'aidpoint','','',role='Bora',condition='AidReady',yes='packed_aid',no='supply_aid')
beat('packed_aid',6,'aidpoint','Hazır seti sağlık görevlisine ver','Bora: Kapalı set sağlık görevlisinin masasına gidiyor.',a('Seti teslim et','FirstAid','drag',target='Table',next='family_lookup'),role='Bora')
beat('supply_aid',6,'aidpoint','Sağlık masasından destek al','Bora: Eksik malzemeyi görevliye bildiriyorum.',a('Görevliye göster','FamilyCard','drag',target='FirstAid',next='family_lookup'),role='Bora')
beat('family_lookup',6,'aidpoint','Aile kartını kontrol et','Bora: Adları ve aile işaretini birlikte doğrulayalım.',a('Aileyi eşleştir','FamilyCard','drag',target='Documents'),role='Bora')
beat('radio_route',6,'aidpoint','','',role='Bora',condition='RadioReady',yes='packed_radio',no='official_radio')
beat('packed_radio',6,'aidpoint','Hazırlanan radyodan bilgiyi dinle','Radyo: Görevlilerin yönlendirmelerini takip ediniz.',a('Yayını dinle','Radio','hold',next='verify_info'),role='Bora')
beat('official_radio',6,'aidpoint','Görevli yayın noktasına git','Bora: Resmî bilgiyi burada birlikte dinleyebiliriz.',a('Yayına yaklaş','Radio','approach'),role='Bora',camera='wide')
beat('official_broadcast',6,'aidpoint','Görevli bilgisini dinle','Bora: Duyduğumuz bilginin kaynağını kontrol ediyoruz.',a('Bilgiyi dinle','Radio','hold',next='verify_info'),role='Bora')
beat('verify_info',6,'aidpoint','Buluşma bilgisini doğrula','Bora: Emin olmadığımız bilgiyi yeniden sorabiliriz.',a('Kartla doğrula','FamilyCard','drag',target='Radio',flags='InfoVerified=1',next='group_route'),a('Görevliye sor','Radio',next='ask_information'),role='Bora')
beat('ask_information',6,'aidpoint','Aile işaretini görevliye göster','Bora: Şimdi bilgileri birlikte eşleştirdik.',a('İşareti göster','FamilyCard','drag',target='Documents',flags='InfoVerified=1'),role='Bora')
beat('group_route',6,'aidpoint','Aile grubuna yol göster','Bora: Herkesin gideceği yer belli.',a('İşareti yerleştir','FamilyCard','drag',target='Plant'),role='Bora',effect='resolve_route')
beat('ada_final',6,'assembly','Ada ve Efe’ye dön','Ada: Ailemize birlikte ulaşacağız.',a('Efe’nin yanına git','ComfortFox','approach'),camera='wide',effect='role_ada')
beat('ending_router',7,'assembly','','',condition='ENDING',yes='',no='')

# 7. Four authored, playable epilogues rather than four renamed completion panels.
beat('ending_detour_1',7,'alternate','Yeni güzergâh işaretini bul','Bora: Kontrol edilen yeni alanda buluşuyoruz.',a('İşarete yürü','FamilyCard','approach'),camera='wide',effect='ending_detour')
beat('ending_detour_2',7,'alternate','Efe’yle aile grubuna katıl','Efe: Annemle babamı gördüm!',a('Birlikte yaklaş','ComfortFox','approach'),camera='wide')
beat('ending_detour_3',7,'alternate','Ailene ulaş','Ada: Yol değişti, birbirimizi yine bulduk.',a('Ailene yaklaş','FamilyCard','approach',next='finish',flags='Ending=1'),camera='wide')
beat('ending_plan_1',7,'assembly','Tanıdığın aile işaretini bul','Efe: Önceden birlikte baktığımız yer!',a('İşareti bul','Plant','approach'),camera='wide',effect='ending_plan')
beat('ending_plan_2',7,'assembly','Aile işaretinizi ver','Ada: Buradayız!',a('İşareti ver','Whistle','hold'))
beat('ending_plan_3',7,'assembly','Sözleştiğiniz yerde buluş','Derya: Birlikte hazırladığımız plan işe yaradı.',a('Ailene yaklaş','FamilyCard','approach',next='finish',flags='Ending=2'),camera='wide')
beat('ending_neighbor_1',7,'neighbor_final','Yusuf Amca’yı dinle','Yusuf: Ailenizi gördüm. Şu güvenli taraftalar.',a('Yusuf’a yaklaş','WalkingCane','approach'),camera='wide',effect='ending_neighbor')
beat('ending_neighbor_2',7,'neighbor_final','Komşunla birlikte ilerle','Efe: Birbirimize yardım ederek geldik.',a('Birlikte yürü','FamilyCard','approach'),camera='wide')
beat('ending_neighbor_3',7,'neighbor_final','Ailene ve komşuna katıl','Emre: Hepinizi bir arada görmek çok güzel.',a('Ailene yaklaş','ComfortFox','approach',next='finish',flags='Ending=3'),camera='wide')
beat('ending_signal_1',7,'help_final','Bora’ya aile kartını göster','Ada: Ailemizi bulmak için yardım istiyoruz.',a('Kartı göster','FamilyCard','drag',target='Radio'),effect='ending_signal')
beat('ending_signal_2',7,'help_final','Görevli anonsunu birlikte dinle','Bora: Aileniz bu buluşma noktasına geliyor.',a('Birlikte dinle','Radio','hold'))
beat('ending_signal_3',7,'help_final','Sesini duyan ailene ulaş','Derya: Yardım isteyerek birbirimizi bulduk.',a('Ailene yaklaş','FamilyCard','approach',next='finish',flags='Ending=4'),camera='wide')
beat('finish',7,'assembly','Senin yolun','Ada: Hazırladıklarımız ve yardımlarımız yolumuzu değiştirdi.',a('Yolunu gör','FamilyCard'),effect='complete')

for i,b in enumerate(beats):
    for choice in b['actions']:
        if not choice['next']:
            choice['next']=beats[i+1]['id'] if i+1<len(beats) else 'finish'

data={
 'title':'Yan Yana','subtitle':'Birlikte hazırlanalım. Birlikte yol bulalım.',
 'schemaVersion':1,
 'chapters':['Bizim mahalle','Hazır mıyız?','Güvenli ev','Sarsıntı','Birlikte dışarı','İdil’in görevi','Yardım noktası','Bizim sonumuz'],
 'chapterMinutes':[2,5,4,4,5,4,4,2],
 'flags':['FamilyPlan','FlashlightReady','RadioReady','WaterReady','FoodReady','AidReady','BlanketReady','ContactCard','WhistleReady','Comfort','BagReady','ShelfSecured','WardrobeSecured','ExitCleared','SiblingSupported','NeighborTogether','FacadeReported','FireAssisted','InfoVerified','Ending'],
 'beats':beats,
 'endings':['Yeni Yolda Birlikte','Söz Verdiğimiz Yerde','Komşu Eli','Sesimizi Duydular']
}
assert len(set(b['id'] for b in beats))==len(beats)
ids={b['id'] for b in beats}
for b in beats:
    assert len(b['title'].split())<=6,(b['id'],b['title'])
    assert len(b['line'].split())<=12,(b['id'],b['line'])
    for c in b['actions']:
        assert c['next'] in ids,(b['id'],c['next'])
(OUT/'Campaign.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'beats':len(beats),'actions':sum(len(b['actions']) for b in beats),'chapters':8,'endings':4}))
