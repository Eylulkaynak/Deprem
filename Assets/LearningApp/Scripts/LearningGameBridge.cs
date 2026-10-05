using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Deprem.Learning
{
    // Owns only navigation. Existing story and Visual Scripting saves remain authoritative.
    public sealed class LearningGameBridge : MonoBehaviour
    {
        public const string SceneName = "Deprem_App";
        public static LearningGameBridge Instance { get; private set; }
        public static bool ReturnedFromGame { get; set; }
        private UIDocument document;
        private PanelSettings overlaySettings;
        private Button returnButton;
        private VisualElement confirmation;
        private Label status;
        private VisualElement returnPicture;
        private Image loadingPicture;
        private bool Child => !LearningProgress.Current.Data.Active.adult;
        private bool loading, dialogOpen;
        private float previousTimeScale;
        private bool previousAudioPause;
        public bool IsLoading => loading;
        public static string RouteScene(string route)
        {
            switch (route)
            {
                case "adventure": return "YanYana_Adventure";
                case "story": return "Story_Rebuild_MainMenu";
                case "minigames": return "Minigame_Hub";
                default: return null;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; ReturnedFromGame = false; }
        public static void Ensure(PanelSettings settings, Font font)
        {
            if (Instance != null) return;
            var host = new GameObject("Learning Game Navigation");
            var bridge = host.AddComponent<LearningGameBridge>();
            bridge.overlaySettings = Instantiate(settings); bridge.overlaySettings.name = "Learning Game Overlay";
            // UIDocument sorting only orders documents inside one panel. The panel itself
            // must render above the legacy UGUI canvases of the 3D scenes.
            bridge.overlaySettings.sortingOrder = 20000;
            bridge.document = host.AddComponent<UIDocument>(); bridge.document.panelSettings = bridge.overlaySettings; bridge.document.sortingOrder = 10000;
            bridge.BuildOverlay(font); bridge.Refresh();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); SceneManager.sceneLoaded += SceneLoaded;
        }
        private void BuildOverlay(Font font)
        {
            var root = document.rootVisualElement; root.pickingMode = PickingMode.Ignore;
            root.style.flexGrow = 1; if (font != null) root.style.unityFont = font;
            root.styleSheets.Add(Resources.Load<StyleSheet>("LearningApp/LearningStyles"));
            returnButton = new Button(RequestReturn) { text = "‹  Eğitime dön", name = "ReturnToLearning" };
            returnButton.AddToClassList("compact");
            returnButton.style.position = Position.Absolute; returnButton.style.right = 14; returnButton.style.bottom = 14;
            returnButton.style.width = 164; root.Add(returnButton);
            returnPicture = NavigationPicture(true); returnButton.Add(returnPicture);
            returnButton.style.flexDirection = FlexDirection.Row; returnButton.style.alignItems = Align.Center; returnButton.style.justifyContent = Justify.Center;
            returnPicture.style.width = 32; returnPicture.style.height = 32;
            confirmation = new VisualElement(); confirmation.AddToClassList("modal-layer"); confirmation.style.display = DisplayStyle.None; root.Add(confirmation);
            status = new Label("Oyun açılıyor…") { pickingMode = PickingMode.Ignore };
            status.style.position = Position.Absolute; status.style.top = Length.Percent(44); status.style.left = Length.Percent(15); status.style.width = Length.Percent(70);
            status.style.backgroundColor = Color.white; status.style.color = new Color(0.08f, 0.25f, 0.36f); status.style.fontSize = 24;
            status.style.paddingTop = status.style.paddingBottom = 20; status.style.unityTextAlign = TextAnchor.MiddleCenter;
            status.style.display = DisplayStyle.None; root.Add(status);
            loadingPicture = Picture("icon-menu-play"); status.Add(loadingPicture);
        }
        private static Image Picture(string key)
        {
            var picture = new Image { image = Resources.Load<Texture2D>("LearningApp/Art/" + key), pickingMode = PickingMode.Ignore };
            picture.style.width = 56; picture.style.height = 56; picture.style.alignSelf = Align.Center;
            return picture;
        }
        private static VisualElement NavigationPicture(bool home)
        {
            var picture = new VisualElement { pickingMode = PickingMode.Ignore };
            picture.style.width = 56; picture.style.height = 56; picture.style.alignSelf = Align.Center;
            picture.generateVisualContent += context =>
            {
                var p = context.painter2D; p.fillColor = new Color32(255,248,227,255);
                float scale=picture.contentRect.width/56f;
                Vector2 V(float x,float y)=>new Vector2(x,y)*scale;
                p.BeginPath();
                if(home) { p.MoveTo(V(5,26)); p.LineTo(V(28,6)); p.LineTo(V(51,26)); p.LineTo(V(45,26)); p.LineTo(V(45,49)); p.LineTo(V(33,49)); p.LineTo(V(33,34)); p.LineTo(V(23,34)); p.LineTo(V(23,49)); p.LineTo(V(11,49)); p.LineTo(V(11,26)); }
                else { p.MoveTo(V(15,7)); p.LineTo(V(49,28)); p.LineTo(V(15,49)); }
                p.ClosePath(); p.Fill();
            };
            return picture;
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode) => Refresh();
        private void Refresh()
        {
            if (returnButton == null) return;
            bool inApp = SceneManager.GetActiveScene().name == SceneName;
            returnButton.text = Child ? "" : "‹  Eğitime dön";
            returnPicture.style.display = Child ? DisplayStyle.Flex : DisplayStyle.None;
            returnButton.style.width = Child ? 76 : 164;
            returnButton.style.display = inApp ? DisplayStyle.None : DisplayStyle.Flex;
        }
        private void Update()
        {
            if (returnButton != null && Screen.height > 0)
                returnButton.style.bottom = Length.Percent(100f * Screen.safeArea.yMin / Screen.height + 1.5f);
            if (!loading && SceneManager.GetActiveScene().name != SceneName && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { if (dialogOpen) CloseDialog(); else RequestReturn(); }
        }
        public void Launch(string route, Action<string> error = null)
        {
            string scene = RouteScene(route);
            if (loading) return;
            if (scene == null || !Application.CanStreamedLevelBeLoaded(scene)) { error?.Invoke("Bu oyun sahnesi derlemeye eklenmemiş: " + (scene ?? route)); return; }
            if (!LearningProgress.Current.Save()) { error?.Invoke("İlerleme kaydedilemedi. Cihazdaki boş alanı kontrol et."); return; }
            StartCoroutine(Load(scene, error));
        }
        public void ReturnToLearning()
        {
            if (loading) return;
            Deprem.Minigames.MinigameScenarioJourney.Cancel();
            CloseDialog(); ReturnedFromGame = true;
            StartCoroutine(Load(SceneName, null));
        }
        private IEnumerator Load(string scene, Action<string> error)
        {
            loading = true; status.text = Child ? "" : scene == SceneName ? "Eğitime dönülüyor…" : "Oyun açılıyor…"; status.style.display = DisplayStyle.Flex;
            loadingPicture.style.display = Child ? DisplayStyle.Flex : DisplayStyle.None;
            returnButton.SetEnabled(false); Time.timeScale = 1; AudioListener.pause = false;
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(scene); }
            catch (Exception e) { error?.Invoke(e.Message); Debug.LogException(e); }
            if (operation != null) while (!operation.isDone) yield return null;
            loading = false; status.style.display = DisplayStyle.None; returnButton.SetEnabled(true); Refresh();
        }
        private void RequestReturn()
        {
            if (loading || dialogOpen) return;
            previousTimeScale = Time.timeScale; previousAudioPause = AudioListener.pause;
            Time.timeScale = 0; AudioListener.pause = true; dialogOpen = true;
            confirmation.Clear(); confirmation.style.display = DisplayStyle.Flex;
            var panel = new VisualElement(); panel.AddToClassList("modal"); confirmation.Add(panel);
            if (Child)
            {
                var heading = new Label("Ara verelim mi?"); heading.AddToClassList("title"); panel.Add(heading);
                Button Choice(string name,string label,bool home,Action action)
                {
                    var choice = new Button(action) { name = name }; choice.style.flexDirection = FlexDirection.Row;
                    choice.style.alignItems = Align.Center; choice.style.justifyContent = Justify.Center;
                    var icon=NavigationPicture(home);icon.style.width=32;icon.style.height=32;icon.style.marginRight=12;choice.Add(icon);
                    var caption=new Label(label) { pickingMode=PickingMode.Ignore };caption.style.fontSize=22;choice.Add(caption);return choice;
                }
                panel.Add(Choice("ConfirmReturnToLearning","Ana menü",true,ReturnToLearning));
                panel.Add(Choice("Resume3DGame","Devam et",false,CloseDialog));
                return;
            }
            var title = new Label("Eğitime dönelim mi?"); title.AddToClassList("title"); panel.Add(title);
            var message = new Label("Eğitim ilerlemen korunur. 3D oyundaki devam noktası oyunun kendi kayıt sistemine bağlıdır."); message.AddToClassList("paragraph"); panel.Add(message);
            panel.Add(new Button(ReturnToLearning) { text = "Eğitime dön" });
            var stay = new Button(CloseDialog) { text = "Oyuna devam et" }; stay.AddToClassList("secondary"); panel.Add(stay);
        }
        private void CloseDialog()
        {
            if (dialogOpen) { Time.timeScale = previousTimeScale; AudioListener.pause = previousAudioPause; }
            dialogOpen = false; if (confirmation != null) confirmation.style.display = DisplayStyle.None;
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (Instance == this) { CloseDialog(); Instance = null; }
            if (overlaySettings != null) Destroy(overlaySettings);
        }
    }
}
