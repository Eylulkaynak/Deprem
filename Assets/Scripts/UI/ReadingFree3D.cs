using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Deprem.Learning;
using Deprem.Story;
using Deprem.Minigames;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deprem.Accessibility
{
    /// <summary>Child presentation for the authored 3D scenes. Never rewrites gameplay text,
    /// callbacks, saves, or Visual Scripting variables (speech still consumes those strings).</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class ReadingFree3D : MonoBehaviour
    {
        public static bool Enabled => !LearningProgress.Current.Data.Active.adult && Supports(SceneManager.GetActiveScene().name);
        public static bool Supports(string scene) => scene == "YanYana_Adventure" || scene == "Story_Rebuild_MainMenu" ||
            // The runner authors pictorial controls and numeric prices in its own HUD.
            // Replacing those labels would hide upgrade costs and Apo's guidance.
            Regex.IsMatch(scene, @"^Story_0[1-4]_RebuildPreview$") ||
            new[] { "Minigame_Hub", "Minigame_FirefighterExtinguish", "Minigame_Evacuation_25D", "Minigame_AftershockCover",
                "Minigame_RoomSafety", "Minigame_EmergencyBagRush", "Minigame_EmergencyCorridor", "Minigame_RubbleSignal" }.Contains(scene);
        private sealed class Binding
        {
            public TMP_Text text, detail; public string role, path, previous; public ReadingFreeIcon[] icons;
            public TextMeshProUGUI caption;
            public StoryActionButton action; public bool world; public float maximum;
        }
        private readonly List<Binding> bindings = new List<Binding>();
        private StoryTouchManager touch;
        private StoryInteractable[] interactions;
        private StoryInteractable guideTarget;
        private MinigameSessionManager session;
        private RectTransform guideRoot;
        private ReadingFreeIcon hand, destination;
        private float scanAt;
        public int ConvertedLabelCount => bindings.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; }
        private static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (!Supports(scene.name) || LearningProgress.Current.Data.Active.adult) return;
            var host = new GameObject("Reading-free 3D presentation"); SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<ReadingFree3D>();
            if (scene.name == "YanYana_Adventure") host.AddComponent<ReadingFreeYanYanaGuide>();
        }
        private void Start()
        {
            touch = FindFirstObjectByType<StoryTouchManager>(); session = FindFirstObjectByType<MinigameSessionManager>();
            interactions = FindObjectsByType<StoryInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Scan();
            if (touch != null) CreateGuide();
        }
        private void Scan()
        {
            foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.gameObject.scene != gameObject.scene || text.name == "Visual caption" || bindings.Any(b => b.text == text)) continue;
                string path = text.name;
                for (Transform t = text.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                // Scenic names and battery polarity may remain; no decision depends on reading them.
                if (text.name.StartsWith("KKTC_") || text.text == "+" || text.text == "−" || text.text == "-") continue;
                var button = text.GetComponentInParent<Button>(true);
                var binding = new Binding { text = text, role = button != null ? "button:" + button.name : text.name,
                    path = path, world = text is TextMeshPro, action = text.GetComponentInParent<StoryActionButton>(true) };
                ConfigureLayout(binding);
                Transform parent = text.transform;
                if (binding.world)
                {
                    var canvas = new GameObject("Picture sign", typeof(RectTransform), typeof(Canvas));
                    canvas.transform.SetParent(parent, false); canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                    float height = Mathf.Clamp(text.fontSize * .28f, .07f, 1.4f);
                    var rect = (RectTransform)canvas.transform; rect.sizeDelta = new Vector2(300,100);rect.localScale=Vector3.one*(height/100);
                    rect.localPosition = new Vector3(0, 0, -.008f); parent = rect;
                }
                binding.icons = new ReadingFreeIcon[3];
                for (int i = 0; i < binding.icons.Length; i++) binding.icons[i] = NewIcon(parent, "Picture " + i);
                var label=new GameObject("Visual caption",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(parent,false);
                binding.caption=label.GetComponent<TextMeshProUGUI>();binding.caption.font=text.font;
                binding.caption.fontSharedMaterial=text.fontSharedMaterial;binding.caption.color=text.color;
                binding.caption.fontStyle=text.fontStyle;binding.caption.enableAutoSizing=true;
                binding.caption.fontSizeMax=binding.world?27:Mathf.Clamp(text.fontSize,12,38);
                binding.caption.fontSizeMin=binding.caption.fontSizeMax*.8f;
                binding.caption.alignment=TextAlignmentOptions.MidlineLeft;binding.caption.textWrappingMode=TextWrappingModes.Normal;
                binding.caption.overflowMode=TextOverflowModes.Ellipsis;binding.caption.raycastTarget=false;
                text.enabled = false; bindings.Add(binding); Refresh(binding);
            }
            scanAt = Time.unscaledTime + 2;
        }
        public static ReadingFreeIcon NewIcon(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(ReadingFreeIcon));
            go.transform.SetParent(parent, false);
            var icon = go.GetComponent<ReadingFreeIcon>(); icon.raycastTarget = false; return icon;
        }
        private void LateUpdate()
        {
            if (Time.unscaledTime >= scanAt) Scan();
            UpdateGuide();
            foreach (var binding in bindings) if (binding.text != null)
            {
                binding.text.enabled = false;
                if (binding.previous != Content(binding) || binding.action != null) Refresh(binding);
            }
        }
        private void Refresh(Binding b)
        {
            b.previous = Content(b);
            string role = b.role.ToLowerInvariant(), message = Normalize(b.previous), path = b.path.ToLowerInvariant();
            string[] pictures; float progress = 1; int repeats = 1;
            if (b.action != null) pictures = new[] { Gesture(b.action.Gesture) };
            else if (role.StartsWith("button:"))
                pictures = role.Contains("caption") || role.Contains("reduced") || role.Contains("vibration")
                    ? new[] { ButtonKind(role,message), message.Contains("kapali") ? "cross" : "check" }
                    : new[] { ButtonKind(role, message) };
            else if (Secondary(role, path)) pictures = Array.Empty<string>();
            else if (role == "leftarrow") pictures = new[] { "back" };
            else if (role == "rightarrow") pictures = new[] { "next" };
            else if (path.Contains("hubheader") && role == "title") pictures = new[] { "icon-menu-play" };
            else if (role == "gametitle") pictures = new[] { path.Contains("firefighter") ? "hose" : path.Contains("firetruck") ? "truck" : path.Contains("room-safety") ? "home" : path.Contains("rubble-signal") ? "item-tap-pipe" : Pictures(message,path)[0] };
            else if (role.Contains("stagecounter") || role.Contains("badgevalue") || role == "badgetext")
            {
                var values = Regex.Matches(message, @"\d+"); int done = values.Count > 0 ? int.Parse(values[0].Value) : 1;
                repeats = values.Count > 1 ? Mathf.Clamp(int.Parse(values[1].Value), 1, 15) : 4;
                progress = (float)done / repeats; pictures = new[] { "dot" };
            }
            else if (role.Contains("timer") || role == "countdown")
            {
                var number = Regex.Matches(message, @"\d+"); float value = number.Count > 0 ? float.Parse(number[0].Value, CultureInfo.InvariantCulture) : 0;
                if(message.Contains(":") && number.Count > 1) value=value*60+float.Parse(number[1].Value,CultureInfo.InvariantCulture);
                b.maximum = session != null && session.VisualStage != null ? Mathf.Max(1,session.VisualStage.stageTimeLimitSeconds) : Mathf.Max(b.maximum, value, 1);
                progress = value / b.maximum;
                pictures = session != null && session.VisualStage != null && session.VisualStage.stageTimeLimitSeconds <= 0 ? Array.Empty<string>() : new[] { "timer" };
            }
            else if(role=="remainingfire")
            {
                var number=Regex.Match(message,@"\d+"); repeats=number.Success?Mathf.Clamp(int.Parse(number.Value),1,8):1;pictures=new[]{"flame"};
            }
            else if(role=="resulttitle"||role=="completiontitle"||role=="successtitle")
                pictures=new[]{message.Contains("doldu")||message.Contains("tekrar")?"replay":"star"};
            else if (role.Contains("stars"))
            {
                var number = Regex.Match(message, @"\d+"); progress = number.Success ? Mathf.Clamp01(float.Parse(number.Value)/3) : 1;
                repeats = 3; pictures = new[] { "star" };
            }
            else if (role == "hinttext" && touch != null)
                pictures = guideTarget != null ? new[] { Gesture(guideTarget.InteractionGesture) } : Array.Empty<string>();
            else if (role.Contains("gesture") || role == "nextaction" || role == "hinttext" || path.Contains("hareket ipucu"))
                pictures = session != null && session.VisualStage != null ? new[] { Gesture(session.VisualStage.gesture) } : new[] { Gesture(message) };
            else if (path.Contains("sectorareceiver")) pictures = new[] { "circle" };
            else if (path.Contains("sectorbreceiver")) pictures = new[] { "square" };
            else if (path.Contains("sectorcreceiver")) pictures = new[] { "triangle" };
            else if (path.Contains("humantriple")) { pictures = new[] { "dot" }; repeats = 3; }
            else if (path.Contains("metalcreak")) pictures = new[] { "long-wave" };
            else if (path.Contains("waterwaveform")) pictures = new[] { "waves" };
            else if (role.Contains("routebadgenumber")) pictures = new[] { role.Contains("preparation") ? "item-emergency-bag" : role.Contains("safety") ? "home" : role.Contains("quake") ? "item-drop-cover-hold" : "item-meet-family" };
            else if (message.Length == 0) pictures = Array.Empty<string>();
            else if (role.Contains("feedback")) pictures = new[] { message.Contains("yeniden") || message.Contains("degil") || message.Contains("tekrar") ? "replay" : "check" };
            else if (role == "subtitletext" || path.Contains("kısa konuşma"))
                pictures = Pictures(message, path).Where(k=>k!="eye").Take(2).Concat(new[]{"hand"}).ToArray();
            else pictures = Pictures(message, path);

            string caption=Caption(b,role,message,pictures);
            if((role=="objectivetitle"||role=="objective") && pictures.Length>1) pictures=pictures.Where(p=>p!="check").Take(2).ToArray();
            bool titleOnly=role=="gametitle"||role=="minigametitle"||role=="pausetitle"||role=="chapterselectiontitle"||
                role=="storyhook"||role.Contains("routebadgecaption")||
                (path.Contains("tek amaç")&&(role=="ada"||Normalize(role)=="bizim mahalle"))||
                (path.Contains("hubheader")&&role=="title");
            if(titleOnly)pictures=Array.Empty<string>();
            if(caption.Length>0 && pictures.Length>1 && role.StartsWith("button:") && !role.Contains("caption") && !role.Contains("reduced") && !role.Contains("vibration"))pictures=pictures.Take(1).ToArray();
            if(pictures.Length==1&&pictures[0]=="eye"&&caption.Length>0)pictures=Array.Empty<string>();
            bool words=caption.Length>0;
            b.caption.gameObject.SetActive(words);b.caption.text=caption;
            var labelRect=b.caption.rectTransform;
            // A single supporting picture beside a short caption preserves the authored hierarchy.
            float pictureWidth=words?Mathf.Min(.35f,((RectTransform)b.caption.transform.parent).rect.height/Mathf.Max(1,((RectTransform)b.caption.transform.parent).rect.width)*1.1f):1;
            if(pictures.Length==0)pictureWidth=0;
            float pictureStart=0,labelEnd=1;
            if(words && role.StartsWith("button:"))
            {
                Rect parentRect=((RectTransform)b.caption.transform.parent).rect;
                pictureWidth=Mathf.Min(.32f,parentRect.height*.7f*pictures.Length/Mathf.Max(1,parentRect.width));
                float group=Mathf.Min(.98f,pictureWidth+(caption.Length*b.caption.fontSizeMax*.55f+12)/Mathf.Max(1,parentRect.width));
                pictureStart=(1-group)*.5f;labelEnd=pictureStart+group;
            }
            labelRect.anchorMin=new Vector2(pictureStart+pictureWidth,0);labelRect.anchorMax=new Vector2(labelEnd,1);
            labelRect.offsetMin=new Vector2(pictures.Length>0?8:0,0);labelRect.offsetMax=Vector2.zero;
            b.caption.alignment=pictures.Length==0?b.text.alignment:TextAlignmentOptions.MidlineLeft;

            for (int i = 0; i < b.icons.Length; i++)
            {
                var icon = b.icons[i]; bool visible = i < pictures.Length; icon.gameObject.SetActive(visible);
                if (!visible) continue;
                var rect = icon.rectTransform;
                rect.anchorMin = new Vector2(pictureStart+(float)i / pictures.Length*pictureWidth, words?.12f:0); rect.anchorMax = new Vector2(pictureStart+(float)(i+1)/pictures.Length*pictureWidth, words?.88f:1);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                icon.Style(words ? b.text.color : new Color32(32,62,59,255), !words);
                icon.Set(pictures[i], progress, repeats);
            }
        }
        private static string Caption(Binding b,string role,string message,string[] pictures)
        {
            string source=Regex.Replace(b.text.text??"",@"<[^>]*>","").Trim();
            source=Regex.Replace(source,@"\s*[·•]\s*\d+\s*/\s*\d+.*$","");
            var culture=new CultureInfo("tr-TR");
            if(source.Length>1 && source==source.ToUpper(culture))source=source.Substring(0,1)+source.Substring(1).ToLower(culture);
            if(role.StartsWith("button:"))
            {
                if(role.Contains("scenariojourney"))return "Deprem senaryosu";
                if(message.Contains("sonraki asama"))return "Devam";
                if(message.Contains("senaryoyu bitir"))return "Tamam";
                if(role.Contains("sound"))return "Ses";
                if(role.Contains("caption"))return "Yazılar";
                if(role.Contains("reduced"))return "Sakin hareket";
                if(role.Contains("vibration"))return "Titreşim";
                if(role.Contains("rotate"))return "Çevir";
                if(role.Contains("replaydecision"))return ObjectName(pictures.FirstOrDefault());
                if(role.Contains("preparationact")||role.Contains("replaybag"))return "Çanta";
                if(role.Contains("homesafetyact"))return "Güvenli ev";
                if(role.Contains("quakeact"))return "Korun";
                if(role.Contains("evacuationact"))return "Güvenli çıkış";
                if(role.Contains("replayplan"))return "Aile planı";
                if(role.Contains("minigame"))return "Oyunlar";
                if(role.Contains("retry")||role.Contains("restart")||role.Contains("replay"))return "Tekrar";
                if(role.Contains("new")||role.Contains("startnew"))return "Yeni oyun";
                if(role.Contains("resume")||role.Contains("continue")||role.Contains("next"))return "Devam";
                if(role.Contains("close")||role.Contains("back"))return "Geri";
                if(role.Contains("hub")||role.Contains("return"))return "Oyunlar";
                if(role.Contains("pause"))return "";
                if(role.Contains("hint"))return "Göster";
                if(role.Contains("done"))return "Tamam";
                if(role.Contains("team"))return "Birlikte";
                if(role.Contains("walk"))return "Yürü";
                if(role.Contains("fire"))return "Yardım et";
                return "Oyna";
            }
            if(role=="pausetitle")return "Kısa bir mola";
            if(role=="chapterselectiontitle")return "Bölüm seç";
            if(role=="storyhook")return "Birlikte güvendeyiz";
            if(role=="gametitle"||role=="minigametitle")
            {
                string identity=Normalize(b.path+" "+SceneManager.GetActiveScene().name);
                return identity.Contains("firefighter")?"Yangını söndür":identity.Contains("firetruck")?"İtfaiye yolda":identity.Contains("aftershock")?"Çök, kapan, tutun":identity.Contains("room")?"Güvenli oda":identity.Contains("bag")?"Çantanı hazırla":identity.Contains("corridor")?"Yolu aç":identity.Contains("rubble")?"Sinyali bul":"Güvenli çıkış";
            }
            if(b.path.Contains("HubHeader")&&role=="title")return "Oyun zamanı";
            if(role.Contains("routebadgecaption"))return role.Contains("preparation")?"Çanta":role.Contains("safety")?"Ev":role.Contains("quake")?"Korun":"Çıkış";
            if(b.path.Contains("Tek amaç")&&(role=="ada"||Normalize(role)=="bizim mahalle"))return source.Length<24?source:"";
            if(role=="resulttitle"||role=="completiontitle"||role=="successtitle")return message.Contains("doldu")||message.Contains("tekrar")?"Bir daha dene":"Harika!";
            if(role.Contains("feedback"))return pictures.Contains("replay")?"Bir daha dene":"Harika!";
            if(role=="gestureverb"||role=="hinttext"||b.path.Contains("Hareket ipucu"))
                return pictures.Length==0?"":pictures[0]=="hold"?"Basılı tut":pictures[0]=="down"?"Aşağı kaydır":pictures[0]=="diagonal"?"Çapraz kaydır":pictures[0]=="horizontal"?"Sürükle":"Dokun";
            if(Secondary(role,b.path.ToLowerInvariant())||role=="nextaction"||role.Contains("timer")||role.Contains("counter")||role.Contains("badgevalue")||role=="badgetext"||role.Contains("stars"))return "";
            if(source.Length<=32&&source.Split(new[]{' ','\n'},StringSplitOptions.RemoveEmptyEntries).Length<=4)return source;
            if(role.Contains("subtitle")||b.path.Contains("Kısa konuşma"))return "Birlikte bakalım";
            string first=pictures.FirstOrDefault();
            return first=="item-drop-cover-hold"?"Çök, kapan, tutun":first=="item-emergency-bag"?"Çantanı hazırla":first=="route"?"Güvenli yolu bul":first=="item-meet-family"?"Ailenle buluş":first=="item-flashlight"?"Feneri dene":first=="hose"?"Yangını söndür":first=="home"?"Güvenli ev":ObjectName(first);
        }
        private static string ObjectName(string key)
        {
            switch(key)
            {
                case "item-emergency-bag":return "Çanta";case "item-flashlight":return "Fener";case "item-battery":return "Pil";
                case "item-water":return "Su";case "item-canfood":return "Yiyecek";case "item-radio":return "Radyo";
                case "item-map":return "Harita";case "item-first-aid":return "İlk yardım";case "item-whistle":return "Düdük";
                case "item-vase":return "Vazo";case "item-book":return "Kitap";case "item-meet-family":return "Aile";
                case "team":return "Birlikte";case "route":return "Güvenli yol";case "home":return "Ev";default:return "";
            }
        }
        private string Content(Binding b) => b.role == "HintText" && touch != null
            ? guideTarget != null ? guideTarget.InteractionGesture.ToString() : ""
            : b.text.text + (b.detail != null ? " " + b.detail.text : "");
        private static void ConfigureLayout(Binding b)
        {
            if (b.world) return;
            string role=b.role.ToLowerInvariant();
            void Fit(float left,float bottom,float right,float top)
            {
                var rect=b.text.rectTransform;rect.anchorMin=new Vector2(left,bottom);rect.anchorMax=new Vector2(right,top);
                rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            if(role=="objectivetitle" && b.path.Contains("ObjectiveStrip"))
            {
                b.detail=b.text.transform.parent.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="ObjectiveDetail"||t.name=="MissionText");
                Fit(.27f,.10f,.97f,.90f);
            }
            if(role=="objective" && b.path.Contains("MissionHeader")) Fit(.035f,.14f,.75f,.64f);
            if(role=="gestureverb") Fit(.24f,.16f,.95f,.88f);
            if(role=="speakername")
            {
                var plate=b.text.transform.parent.GetComponent<Image>();if(plate!=null)plate.enabled=false;
            }
            if(role=="nextaction")
            {
                var rect=b.text.transform.parent as RectTransform;
                if(rect!=null)rect.sizeDelta=new Vector2(76,76);
                Fit(.06f,.06f,.94f,.94f);
            }
        }
        private static bool Secondary(string role, string path) =>
            role.Contains("eyebrow") || role.Contains("caption") || role == "speakername" || role.Contains("kicker") ||
            role == "score" || role=="resultdetail" || role=="completionstats" || role.Contains("coins") || role.Contains("cointotal") || role.Contains("totalcoin") || role.Contains("coincount") ||
            role.Contains("besttime") || role == "distance" || role == "gesturedetail" || role == "gestureprogress" ||
            role == "resultlabel" || role == "pausetitle" || role == "pausehint" || role == "chapterselectionhint" ||
            role == "actbadgecaption" || role == "minigametitle" || role == "objectivedetail" ||
            (path.Contains("tek amaç") && (role=="ada" || Normalize(role)=="bizim mahalle")) ||
            (role == "missiontext" && path.Contains("objectivestrip")) || path.Contains("karakter geçişi") || path.Contains("i̇steğe bağlı destek");
        public static string Normalize(string value) => (value ?? "").ToLower(new CultureInfo("tr-TR"))
            .Replace('ı','i').Replace('ş','s').Replace('ğ','g').Replace('ü','u').Replace('ö','o').Replace('ç','c');
        private static string ButtonKind(string role, string message)
        {
            if(role.Contains("scenariojourney"))return "item-drop-cover-hold";
            if(message.Contains("sonraki asama"))return "next";
            if(message.Contains("senaryoyu bitir"))return "check";
            if (role.Contains("sound")) return message.Contains("kapali") ? "muted" : "sound";
            if (role.Contains("caption")) return "captions";
            if (role.Contains("reduced") || role.Contains("vibration")) return "vibrate";
            if (role.Contains("rotate")) return "rotate";
            if (role.Contains("preparationact") || role.Contains("replaybag") || role.Contains("replaydecisionbag")) return "item-emergency-bag";
            if (role.Contains("homesafetyact")) return "home";
            if (role.Contains("quakeact")) return "item-drop-cover-hold";
            if (role.Contains("evacuationact") || role.Contains("replayplan")) return "item-meet-family";
            if (role.Contains("minigame")) return "icon-menu-play";
            if (role.Contains("replaydecision"))
            {
                if(role.Contains("flashlight"))return "item-flashlight";
                if(role.Contains("radio"))return "item-radio";
                if(role.Contains("supplywater"))return "item-water";
                if(role.Contains("supplyfood"))return "item-canfood";
                if(role.Contains("map"))return "item-map";
                if(role.Contains("aid"))return "item-first-aid";
                if(role.Contains("fire"))return "hose";
                if(role.Contains("home"))return "home";
                if(role.Contains("evacuation"))return "route";
            }
            if (role.Contains("retry") || role.Contains("restart") || role.Contains("replay")) return "replay";
            if (role.Contains("continue")) return "next";
            if (role.Contains("resume") || role.Contains("continue") || role.Contains("start") || role.Contains("play") || role.Contains("new")) return "play";
            if (role.Contains("close") || role.Contains("back")) return "back";
            if (role.Contains("hub") || role.Contains("return")) return "home";
            if (role.Contains("done")) return "check";
            if (role.Contains("pause")) return "pause";
            if (role.Contains("hint")) return "hand";
            if (role.Contains("team")) return "team";
            if (role.Contains("walk")) return "route";
            if (role.Contains("fire")) return "hose";
            if (role.Contains("next")) return "next";
            return Pictures(message, role)[0];
        }
        public static string Gesture(object gesture) => Gesture(gesture.ToString().ToLowerInvariant());
        private static string Gesture(string value)
        {
            value = Normalize(value);
            if (value.Contains("diagonal") || value.Contains("capraz")) return "diagonal";
            if (value.Contains("swipedown") || value.Contains("asagi")) return "down";
            if (value.Contains("swipehorizontal") || value.Contains("drag") || value.Contains("surukle") || value.Contains("kaydir") || value.Contains("yonlendir")) return "horizontal";
            if (value.Contains("hold") || value.Contains("basili") || value.Contains("tut")) return "hold";
            return "hand";
        }
        public static string[] Pictures(string text, string path = "")
        {
            string s = Normalize(text), p = Normalize(path); var result = new List<string>();
            void Add(bool when, string key) { if (when && !result.Contains(key) && result.Count < 3) result.Add(key); }
            // Stable matching symbols replace written route/sector names on both source and destination.
            if (s == "a" || s == "a rotasi" || s.Contains("bati giris") || s == "bati") return new[] { "circle" };
            if (s == "b" || s == "b rotasi" || s.Contains("dogu giris") || s == "dogu") return new[] { "square" };
            if (s == "c") return new[] { "triangle" };
            if (p.Contains("expiry") || Regex.IsMatch(s, @"^09 / 202[58]$")) return new[] { s.Contains("2025") ? "cross" : "check" };
            if (s.Contains("bugun:")) return Array.Empty<string>();
            Add(s.Contains("cok") || s.Contains("kapan") || s.Contains("masa alti") || s.Contains("korun"), "item-drop-cover-hold");
            Add(s.Contains("canta") || s.Contains("agir esya") || s.Contains("hazirlik"), "item-emergency-bag");
            Add(s.Contains("fener") || s.Contains("kesinti"), "item-flashlight");
            Add(s.Contains("pil"), "item-battery");
            Add(s.Contains("radyo") || s.Contains("yayin"), "item-radio");
            Add(Regex.IsMatch(s, @"\bsu\b") || s.Contains("siseyi"), "item-water");
            Add(s.Contains("gida") || s.Contains("yiyecek"), "item-canfood");
            Add(s.Contains("saglik") || s.Contains("ilkyardim") || s.Contains("ilk yardim"), "item-first-aid");
            Add(s.Contains("duduk") || s.Contains("sinyal") || p.Contains("whistle"), "item-whistle");
            Add(s.Contains("aile") || s.Contains("toplan") || s.Contains("bulus") || s.Contains("kardes") || s.Contains("efe") || s.Contains("can "), "item-meet-family");
            Add(s.Contains("iletisim") || s.Contains("melek") || s.Contains("haber ver") || s.Contains("uzaktan bildir") || p.Contains("contact"), "item-send-message");
            Add(s.Contains("vazo"), "item-vase"); Add(s.Contains("kitap"), "item-book");
            Add(s.Contains("cerceve"), "item-mirror"); Add(s.Contains("sepet"), "basket");
            Add(s.Contains("dolap") || s.Contains("sabitle"), "item-hold-cabinet");
            Add(s.Contains("yangin") || s.Contains("itfaiye") || s.Contains("hortum") || s.Contains("sondur"), "hose");
            Add(s.Contains("merdiven"), "item-run-stairs"); Add(s.Contains("asansor"), "item-use-elevator");
            Add(s.Contains("harita") || s.Contains("rota") || s.Contains("koridor") || s.Contains("tahliye") || s.Contains("cikis") || s.Contains("yuru") || s.Contains("acik gecis") || s.Contains("molozdan uzak"), "route");
            Add(s.Contains("ekip") || s.Contains("komsu") || s.Contains("birlikte"), "team");
            Add(s.Contains("tamam") || s.Contains("basar") || s.Contains("dogrulandi"), "check");
            Add(s.Contains("geri koy"), "back");
            if (result.Count == 0) Add(true, s.Contains("ev") ? "home" : s.Contains("dokun") ? "hand" : "eye");
            return result.ToArray();
        }
        private void CreateGuide()
        {
            var go = new GameObject("Visual target guide", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false); var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080,1920); scaler.matchWidthOrHeight = .5f;
            guideRoot = (RectTransform)go.transform; hand = NewIcon(guideRoot, "Demonstrated gesture"); destination = NewIcon(guideRoot, "Destination ring");
            hand.rectTransform.sizeDelta = new Vector2(90,90); destination.rectTransform.sizeDelta = new Vector2(84,84);
        }
        private void UpdateGuide()
        {
            if (hand == null) return;
            var camera = Camera.main; bool available = touch != null && touch.VisualInputAvailable && Time.timeScale > 0;
            StoryInteractable target = available ? touch.VisualInteraction : null;
            if (available && target == null && camera != null)
            {
                float best = float.PositiveInfinity;
                foreach (var item in interactions)
                {
                    if (item == null || !item.gameObject.activeInHierarchy || !item.IsAvailable || !item.WorldSelectable || item.InteractionKind == StoryInteractionKind.UnsafeChoice) continue;
                    if (item.RequiredFlag != StoryFlag.None && (StoryGameManager.Instance == null || !StoryGameManager.Instance.HasFlag(item.RequiredFlag))) continue;
                    var viewport = camera.WorldToViewportPoint(item.InteractionPoint.position);
                    if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < .12f || viewport.y > .9f) continue;
                    float score = (viewport-new Vector3(.5f,.5f,viewport.z)).sqrMagnitude;
                    if (score < best) { best=score; target=item; }
                }
            }
            bool show = available && target != null && camera != null;
            guideTarget = show ? target : null;
            hand.gameObject.SetActive(show); destination.gameObject.SetActive(show);
            if (!show) return;
            Vector3 start = camera.WorldToScreenPoint(target.InteractionPoint.position);
            if (start.z <= 0) { hand.gameObject.SetActive(false); destination.gameObject.SetActive(false); return; }
            Vector3 end = start; string gesture = Gesture(target.InteractionGesture);
            if (target.GestureTarget != null) end = camera.WorldToScreenPoint(target.GestureTarget.position);
            else if (gesture == "down") end += Vector3.down * Screen.height*.07f;
            else if (gesture == "horizontal") end += Vector3.right * Screen.width*.15f;
            else if (gesture == "diagonal") end += new Vector3(Screen.width*.1f,-Screen.height*.06f);
            float cycle = Mathf.Repeat(Time.time, 2)/2;
            var point = Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,Mathf.Clamp01(cycle*1.5f)));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(guideRoot,point,null,out var local);
            hand.rectTransform.anchoredPosition=local; hand.Set(gesture == "hold" ? "hold" : "hand");
            hand.rectTransform.localScale=Vector3.one * (gesture == "hand" ? 1-.12f*Mathf.Sin(cycle*Mathf.PI*2) : 1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(guideRoot,end,null,out local);
            destination.rectTransform.anchoredPosition=local; destination.Set("target");
        }
    }
}
