using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.VisualScripting;
using Deprem.Story;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static void FitModel(GameObject model, float longest)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            model.transform.localScale *= longest / Mathf.Max(.01f, max);
            bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            model.transform.position += model.transform.parent.position - bounds.center;
        }

        static void CreateAction(YYBeat beat, YYAction action, int index)
        {
            var container = beatWorlds[beat.id]; var origin = stations[beat.station];
            bool walk = action.gesture == "approach";
            float x = beat.actions.Length == 1 ? (walk ? -.25f : -.86f) : index == 0 ? -1.13f : 1.26f;
            int order = Array.FindIndex(campaign.beats, b => b.id == beat.id);
            var at = origin + new Vector3(x, walk ? .11f : 1.16f, walk ? 1.2f + (order % 3) * .4f : .25f);
            if (beat.chapter == 7 && walk) at += new Vector3((order % 3 - 1) * 1.4f, 0, order % 3 * .7f);
            var source = Group(index + " · " + action.gesture + " · " + action.label, container.transform, at);
            targets[beat.id].Add(source);
            var model = Model(action.model, source.transform, Vector3.zero);
            FitModel(model, walk ? .5f : 1.04f);
            if (walk) model.SetActive(false);
            var marker = Shape("Dokunma halkası", PrimitiveType.Cylinder, source.transform, new Vector3(0, walk ? 0 : -.5f, 0), new Vector3(1.12f, .025f, 1.12f), mats["YY_teal"]);
            var inner = Shape("Hedef merkezi", PrimitiveType.Cylinder, source.transform, new Vector3(0, walk ? .028f : -.471f, 0), new Vector3(.85f, .008f, .85f), mats["Paper"]);
            var collider = source.AddComponent<BoxCollider>(); collider.size = walk ? new Vector3(1.3f, .5f, 1.3f) : new Vector3(1.15f, 1.25f, 1.15f);
            collider.center = walk ? new Vector3(0, .15f, 0) : Vector3.zero;
            var label = WorldText(walk ? "●\n" + action.label : action.label, source.transform, new Vector3(0, walk ? .6f : .76f, 0), .21f, Ink);
            label.rectTransform.sizeDelta = new Vector2(2.6f, .8f); label.transform.rotation = View(beat).transform.rotation;
            var ringObject = Group("Hareketin ilerleme halkası", source.transform, new Vector3(0, .05f, -.57f));
            var canvas = ringObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rt = ringObject.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(128, 128); rt.localScale = Vector3.one * .0095f;
            ringObject.transform.rotation = View(beat).transform.rotation;
            var ring = Panel("Basılı tutma", ringObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Hex("#EAB64E"));
            ring.sprite = circle; ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360; ring.fillAmount = 0; ring.raycastTarget = false;
            var hole = Panel("Halka içi", ringObject.transform, Vector2.zero, Vector2.one, new Vector2(9, 9), new Vector2(-9, -9), new Color(1, 1, 1, 0)); hole.raycastTarget = false;
            GameObject target = null;
            var targetAt = origin + new Vector3(.97f, 1.16f, 1.4f);
            if (action.gesture == "drag")
            {
                target = Group("Bırakma hedefi · " + action.target, container.transform, targetAt);
                var tm = Model(action.target, target.transform, Vector3.zero); FitModel(tm, 1.23f);
                Shape("Hedef işareti", PrimitiveType.Cylinder, target.transform, new Vector3(0, -.5f, 0), new Vector3(1.38f, .02f, 1.38f), mats["YY_mustard"]);
                WorldText("BURAYA", target.transform, new Vector3(0, -.53f, -.78f), .17f, Ink).transform.rotation = View(beat).transform.rotation;
            }
            var g = new YanYanaGraphAuthor(source, "Doğrudan " + action.gesture + " · " + action.label);
            g.Initial("Held", false); g.Initial("Hold", 0f); g.Initial("Walking", false); g.Initial("Done", false); g.Initial("StartPointer", Vector2.zero);
            g.Initial("Origin", at);
            var enabled = g.Add(new Unity.VisualScripting.OnEnable()); var p = g.SetVar(enabled.trigger, "Held", false);
            p = g.SetVar(p, "Hold", 0f); p = g.SetVar(p, "Walking", false); p = g.SetVar(p, "Done", false);
            p = g.Set(p, typeof(Transform), "position", source.transform, at); g.Set(p, typeof(Image), "fillAmount", ring, 0f);
            var down = g.Add(new OnPointerDown()); g.Bind(down.target, source);
            var pressed = g.Branch(down.trigger, Available(g)); p = g.SetVar(pressed.ifTrue, "Held", true);
            p = g.SetVar(p, "StartPointer", g.Get(typeof(PointerEventData), "position", down.data));
            p = g.SetVar(p, "Hold", 0f); p = g.SetVar(p, "IdleSeconds", 0f, flow);
            g.Do(p, typeof(StoryPlayerMovement), "Stop", movers[beat.role], NoArgs);
            var up = g.Add(new OnPointerUp()); g.Bind(up.target, source); p = g.SetVar(up.trigger, "Held", false); g.SetVar(p, "Hold", 0f);
            var update = g.Add(new Unity.VisualScripting.Update());
            var paused = g.Branch(update.trigger, g.Var("Paused", flow)); p = g.SetVar(paused.ifTrue, "Held", false); p = g.SetVar(p, "Hold", 0f); g.Set(p, typeof(Image), "fillAmount", ring, 0f);
            var available = g.Branch(paused.ifFalse, Available(g));
            if (action.gesture == "tap")
            {
                var click = g.Add(new OnPointerClick()); g.Bind(click.target, source); var allowed = g.Branch(click.trigger, Available(g)); g.Send(allowed.ifTrue, source, "Accept");
            }
            else if (walk)
            {
                var click = g.Add(new OnPointerClick()); g.Bind(click.target, source); var allowed = g.Branch(click.trigger, Available(g));
                p = g.Do(allowed.ifTrue, typeof(StoryPlayerMovement), "TrySetDestination", movers[beat.role], new[] { typeof(Vector3) }, new Vector3(at.x, 0, at.z));
                g.SetVar(p, "Walking", true);
                var walking = g.Branch(available.ifTrue, g.Var("Walking"));
                var distance = g.Call(typeof(Vector3), "Distance", null, new[] { typeof(Vector3), typeof(Vector3) }, g.Get(typeof(Transform), "position", cast[beat.role].transform), new Vector3(at.x, 0, at.z)).result;
                var arrived = g.Branch(walking.ifTrue, g.Binary<Less>(distance, .4f)); g.Send(arrived.ifTrue, source, "Accept");
            }
            else if (action.gesture == "hold")
            {
                var held = g.Branch(available.ifTrue, g.Var("Held"));
                float duration = beat.id == "hold_table" || beat.id == "hold_after" ? 3.4f : 1.25f;
                p = g.SetVar(held.ifTrue, "Hold", Sum(g, g.Var("Hold"), g.Get(typeof(Time), "deltaTime")));
                p = g.Set(p, typeof(Image), "fillAmount", ring, g.Binary<ScalarDivide>(g.Var("Hold"), duration));
                var complete = g.Branch(p, g.Binary<GreaterOrEqual>(g.Var("Hold"), duration)); g.Send(complete.ifTrue, source, "Accept");
            }
            else if (action.gesture == "drag")
            {
                var drag = g.Add(new OnDrag()); g.Bind(drag.target, source); var allowed = g.Branch(drag.trigger, And(g, Available(g), g.Var("Held")));
                var pointer = g.Get(typeof(PointerEventData), "position", drag.data);
                var depth = g.Get(typeof(Vector3), "z", g.Call(typeof(Camera), "WorldToScreenPoint", camera, new[] { typeof(Vector3) }, at).result);
                var screen = V3(g, g.Get(typeof(Vector2), "x", pointer), g.Get(typeof(Vector2), "y", pointer), depth);
                var worldPoint = g.Call(typeof(Camera), "ScreenToWorldPoint", camera, new[] { typeof(Vector3) }, screen).result;
                g.Set(allowed.ifTrue, typeof(Transform), "position", source.transform, worldPoint);
                var end = g.Add(new OnEndDrag()); g.Bind(end.target, source); var allowedEnd = g.Branch(end.trigger, Available(g));
                var d = g.Call(typeof(Vector3), "Distance", null, new[] { typeof(Vector3), typeof(Vector3) }, g.Get(typeof(Transform), "position", source.transform), targetAt).result;
                var hit = g.Branch(allowedEnd.ifTrue, g.Binary<Less>(d, .95f)); g.Send(hit.ifTrue, source, "Accept");
                p = g.Set(hit.ifFalse, typeof(Transform), "position", source.transform, at); g.SetVar(p, "IdleSeconds", 0f, flow);
            }
            else if (action.gesture == "swipe")
            {
                var drag = g.Add(new OnDrag()); g.Bind(drag.target, source); // Register drag tracking on the collider.
                var end = g.Add(new OnEndDrag()); g.Bind(end.target, source); var allowed = g.Branch(end.trigger, Available(g));
                var current = g.Get(typeof(PointerEventData), "position", end.data);
                var dy = g.Binary<ScalarSubtract>(g.Get(typeof(Vector2), "y", g.Var("StartPointer")), g.Get(typeof(Vector2), "y", current));
                var distance = g.Binary<ScalarMultiply>(g.Get(typeof(Screen), "height"), .065f);
                var swiped = g.Branch(allowed.ifTrue, g.Binary<Greater>(dy, distance)); g.Send(swiped.ifTrue, source, "Accept");
            }
            else if (action.gesture == "spray")
            {
                model.SetActive(false); marker.SetActive(false); inner.SetActive(false); label.gameObject.SetActive(false); collider.enabled = false;
                var manager = fireManagers[beat.effect];
                var extinguished = g.Branch(available.ifTrue, g.Get(typeof(FirefighterExtinguishManager), "IsSuccessful", manager));
                g.Send(extinguished.ifTrue, source, "Accept");
            }
            var accept = g.Event("Accept", true); var once = g.Branch(accept.trigger, And(g, Available(g), Is(g, g.Var("Done"), false)));
            p = g.SetVar(once.ifTrue, "Done", true); p = g.SetVar(p, "Busy", true, flow); p = g.SetVar(p, "Held", false);
            if (!action.safe)
            {
                string sentence = action.effect == "unsafe_elevator" ? "Merdiven yolunu birlikte kullanalım." : action.effect == "unsafe_packet" ? "Kapalı ve bozulmamış paketi seçelim." : action.effect == "unsafe_glass" ? "Camların uzağındaki açık yolu kullanalım." : "Yakında çök, başını koru, tutun.";
                p = Text(g, p, lineText, sentence); p = g.Set(p, typeof(Renderer), "enabled", marker.GetComponent<Renderer>(), false);
                p = g.Wait(p, 3f); p = Text(g, p, lineText, beat.line); p = g.Set(p, typeof(Renderer), "enabled", marker.GetComponent<Renderer>(), true);
                p = Released(g, p); p = g.SetVar(p, "Done", false); g.SetVar(p, "Busy", false, flow);
            }
            else
            {
                if (!string.IsNullOrEmpty(action.flags)) foreach (string flag in action.flags.Split(','))
                {
                    var parts = flag.Split('='); p = g.Send(p, flow, "DecisionTaken", parts[0], int.Parse(parts[1]));
                }
                if (sounds.TryGetValue("complete", out var sound)) p = g.Do(p, typeof(AudioSource), "PlayOneShot", feedback, new[] { typeof(AudioClip) }, sound);
                p = ApplyActionEffect(g, p, beat, action);
                if (action.gesture == "drag") p = g.Set(p, typeof(Transform), "position", source.transform, targetAt);
                p = g.Wait(p, .22f); p = Released(g, p);
                g.Send(p, flow, "Go", action.next);
            }
            g.Dirty();
        }
    }
}
