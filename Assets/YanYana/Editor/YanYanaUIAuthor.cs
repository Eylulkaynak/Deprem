using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static RectTransform safeRect;
        static readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        static readonly List<TMP_Text> causeCards = new List<TMP_Text>();
        static Color Ink => Hex("#244D50");
        static Color Paper => Hex("#FFF5DF");

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax; return rt;
        }
        static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var rt = Rect(name, parent, min, max, offsetMin, offsetMax); var image = rt.gameObject.AddComponent<Image>();
            image.sprite = panelSprite; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false; return image;
        }
        static TMP_Text Label(string text, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, int size, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var rt = Rect(text, parent, min, max, offsetMin, offsetMax); var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font;
            t.text = SafeText(text); t.fontSize = size; t.color = color ?? Ink; t.alignment = alignment; t.raycastTarget = false;
            t.enableWordWrapping = true; t.overflowMode = TextOverflowModes.Overflow; return t;
        }
        static Button Button(string id, string text, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color? color = null)
        {
            var p = Panel(id, parent, min, max, offsetMin, offsetMax, color ?? Hex("#327E80")); p.raycastTarget = true;
            var button = p.gameObject.AddComponent<Button>(); var colors = button.colors; colors.highlightedColor = new Color(.94f, 1, 1); colors.pressedColor = new Color(.8f, .9f, .85f); button.colors = colors;
            Label(text, p.transform, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8), 24, Paper, TextAlignmentOptions.Center);
            buttons[id] = button; return button;
        }

        static void CreateUI()
        {
            buttons.Clear(); causeCards.Clear();
            var go = Group("Dikey ekran · 540 × 960", uiRoot.transform); var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(540, 960); scaler.matchWidthOrHeight = 0;
            go.AddComponent<GraphicRaycaster>();
            safeRect = Rect("Güvenli ekran alanı", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var top = Panel("Tek amaç", safeRect, new Vector2(0, 1), Vector2.one, new Vector2(18, -142), new Vector2(-18, -18), Paper);
            var portrait = Rect("Aktif karakter portresi", top.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, -44), new Vector2(88, 46));
            portraitImage = portrait.gameObject.AddComponent<Image>(); portraitImage.sprite = portraits["Ada"]; portraitImage.preserveAspect = true; portraitImage.raycastTarget = false;
            chapterText = Label("BİZİM MAHALLE", top.transform, new Vector2(0, .67f), Vector2.one, new Vector2(103, 0), new Vector2(-68, -4), 17, Hex("#69807B"));
            goalText = Label("Efe’nin yanına git", top.transform, new Vector2(0, .13f), new Vector2(1, .76f), new Vector2(103, 0), new Vector2(-85, 0), 26);
            roleText = Label("Ada", top.transform, Vector2.zero, new Vector2(0, .2f), new Vector2(14, 0), new Vector2(92, 3), 17, Ink, TextAlignmentOptions.Center);
            Button("Pause", "Dur", top.transform, new Vector2(1, 1), Vector2.one, new Vector2(-78, -79), new Vector2(-4, -5), Hex("#5C8580"));
            var dialog = Panel("Kısa konuşma", safeRect, Vector2.zero, new Vector2(1, 0), new Vector2(18, 22), new Vector2(-18, 146), Paper);
            lineText = Label("Efe: Uçurtmamızın kuyruğu nerede?", dialog.transform, Vector2.zero, Vector2.one, new Vector2(22, 15), new Vector2(-22, -15), 26);
            var instruction = Panel("Hareket ipucu", safeRect, Vector2.zero, new Vector2(1, 0), new Vector2(24, 154), new Vector2(-24, 204), new Color(.14f, .3f, .31f, .94f));
            gestureText = Label("Zemine dokun · birlikte yürü", instruction.transform, Vector2.zero, Vector2.one, new Vector2(10, 3), new Vector2(-10, -3), 21, Paper, TextAlignmentOptions.Center);
            hintPanel = Panel("İsteğe bağlı destek", safeRect, new Vector2(.08f, 0), new Vector2(.92f, 0), new Vector2(0, 306), new Vector2(0, 382), Hex("#E6AE5A")).gameObject;
            hintText = Label("Birlikte deneyebiliriz.", hintPanel.transform, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-128, -4), 21);
            var hintButton=Button("Hint", "Göster", hintPanel.transform, new Vector2(1, 0), Vector2.one, new Vector2(-124, 4), new Vector2(-4, -4));hintButton.GetComponentInChildren<TMP_Text>().enableWordWrapping=false;hintPanel.SetActive(false);
            rolePanel = Panel("Karakter geçişi", safeRect, new Vector2(.08f, .44f), new Vector2(.92f, .65f), Vector2.zero, Vector2.zero, Paper).gameObject;
            roleBannerText = Label("Şimdi İdil", rolePanel.transform, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20), 38, Ink, TextAlignmentOptions.Center); rolePanel.SetActive(false);
            titlePanel = Panel("Yan Yana · başlangıç", safeRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.96f, .93f, .84f, .95f)).gameObject;
            Label("DEPREM", titlePanel.transform, new Vector2(.06f, .57f), new Vector2(.94f, .91f), Vector2.zero, Vector2.zero, 84, Ink, TextAlignmentOptions.Center);
            Label("Birlikte hazırlanalım.\nBirlikte yol bulalım.", titlePanel.transform, new Vector2(.1f, .46f), new Vector2(.9f, .57f), Vector2.zero, Vector2.zero, 28, Ink, TextAlignmentOptions.Center);
            var duo = Rect("Ada ve Efe", titlePanel.transform, new Vector2(.14f, .3f), new Vector2(.86f, .47f), Vector2.zero, Vector2.zero);
            for (int i = 0; i < 2; i++)
            {
                var rt = Rect(Names[i], duo, new Vector2(i * .5f, 0), new Vector2((i + 1) * .5f, 1), Vector2.zero, Vector2.zero);
                var image = rt.gameObject.AddComponent<Image>(); image.sprite = portraits[Names[i]]; image.preserveAspect = true; image.raycastTarget = false;
            }
            Button("Continue", "Macera devam etsin", titlePanel.transform, new Vector2(.1f, .19f), new Vector2(.9f, .28f), Vector2.zero, Vector2.zero);
            Button("New", "Yeni yolculuk", titlePanel.transform, new Vector2(.1f, .08f), new Vector2(.9f, .17f), Vector2.zero, Vector2.zero, Hex("#BA6A53"));
            Label("Dokun · keşfet · dene", titlePanel.transform, new Vector2(.1f, .01f), new Vector2(.9f, .07f), Vector2.zero, Vector2.zero, 20, Ink, TextAlignmentOptions.Center);
            pausePanel = Panel("Duraklatma", safeRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Paper).gameObject;
            Label("Bir nefes alalım", pausePanel.transform, new Vector2(.1f, .8f), new Vector2(.9f, .92f), Vector2.zero, Vector2.zero, 38, Ink, TextAlignmentOptions.Center);
            Button("Resume", "Devam et", pausePanel.transform, new Vector2(.1f, .67f), new Vector2(.9f, .77f), Vector2.zero, Vector2.zero);
            Button("ReducedMotion", "Kamera sarsıntısı: sakin", pausePanel.transform, new Vector2(.1f, .54f), new Vector2(.9f, .64f), Vector2.zero, Vector2.zero);
            Button("Sound", "Ses: açık", pausePanel.transform, new Vector2(.1f, .41f), new Vector2(.9f, .51f), Vector2.zero, Vector2.zero);
            Button("Captions", "Altyazı: açık", pausePanel.transform, new Vector2(.1f, .28f), new Vector2(.9f, .38f), Vector2.zero, Vector2.zero);
            Button("Vibration", "Titreşim: kapalı", pausePanel.transform, new Vector2(.1f, .15f), new Vector2(.9f, .25f), Vector2.zero, Vector2.zero);
            Label("Tamamladığın hareketler kaydediliyor.", pausePanel.transform, new Vector2(.1f, .03f), new Vector2(.9f, .13f), Vector2.zero, Vector2.zero, 20, Ink, TextAlignmentOptions.Center); pausePanel.SetActive(false);
            endingPanel = Panel("Senin yolun · neden ve sonuç", safeRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Paper).gameObject;
            Label("Senin yolun", endingPanel.transform, new Vector2(.08f, .86f), new Vector2(.92f, .97f), Vector2.zero, Vector2.zero, 43, Ink, TextAlignmentOptions.Center);
            endingText = Label("Söz Verdiğimiz Yerde", endingPanel.transform, new Vector2(.08f, .75f), new Vector2(.92f, .86f), Vector2.zero, Vector2.zero, 28, Ink, TextAlignmentOptions.Center);
            for (int i = 0; i < 3; i++)
            {
                float y = .56f - i * .175f;
                var card = Panel("Neden–sonuç " + i, endingPanel.transform, new Vector2(.07f, y), new Vector2(.93f, y + .15f), Vector2.zero, Vector2.zero, Hex(i % 2 == 0 ? "#E1E9D7" : "#F0D4B7"));
                var icon=Rect("Karar resmi",card.transform,Vector2.zero,new Vector2(.18f,1),new Vector2(8,12),new Vector2(-2,-12)).gameObject.AddComponent<Image>();icon.sprite=portraits[new[]{"Ada","Yusuf","Efe"}[i]];icon.preserveAspect=true;icon.raycastTarget=false;
                causeCards.Add(Label("Hazırlığın yolunu değiştirdi.", card.transform, new Vector2(.19f, 0), Vector2.one, new Vector2(4, 12), new Vector2(-14, -12), 24));
            }
            Button("ReplayPlan", "Aile planını yeniden dene", endingPanel.transform, new Vector2(.07f, .09f), new Vector2(.93f, .18f), Vector2.zero, Vector2.zero);
            Button("ReplayBag", "Fener kararına dön", endingPanel.transform, new Vector2(.07f, .01f), new Vector2(.93f, .08f), Vector2.zero, Vector2.zero, Hex("#BA6A53")); endingPanel.SetActive(false);
        }

        static void Wire(string id, YanYanaGraphAuthor graph, string eventName)
        {
            UnityEventTools.AddStringPersistentListener(buttons[id].onClick, graph.Machine.TriggerUnityEvent, eventName);
        }
        static ControlOutput ButtonEvent(YanYanaGraphAuthor g, string button, string name)
        {
            Wire(button, g, name); var e = g.Add(new BoltUnityEvent()); g.Bind(e.name, name); return e.trigger;
        }
    }
}
