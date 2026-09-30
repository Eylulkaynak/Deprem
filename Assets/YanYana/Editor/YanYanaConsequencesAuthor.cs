using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Deprem.Story;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static void CreateFireActivities()
        {
            var idil = cast["Idil"]; var bones = idil.GetComponentsInChildren<Transform>();
            var nozzle = Model("HoseNozzle", idil.transform, new Vector3(.23f, 1.08f, .56f), 1.3f);
            var tip = Group("NozzleTip", nozzle.transform, new Vector3(0, .02f, .32f));
            var rightGrip = Group("Sağ el kavrama", nozzle.transform, new Vector3(.09f, -.05f, -.09f));
            var leftGrip = Group("Sol el kavrama", nozzle.transform, new Vector3(-.1f, .02f, .12f));
            var waterMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); waterMaterial.SetColor("_BaseColor", Hex("#A1DBE1"));
            string mp = Root + "Art/Materials/WaterJet.mat"; if (AssetDatabase.LoadAssetAtPath<Material>(mp)) AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(waterMaterial, mp);
            for (int groupIndex = 1; groupIndex <= 3; groupIndex++)
            {
                string key = "fire_" + groupIndex; var root = Group("İdil · müdahale grubu " + groupIndex, world.transform); root.SetActive(false);
                var stream = Group("Su akışı", root.transform).AddComponent<LineRenderer>(); stream.positionCount = 5; stream.startWidth = .065f; stream.endWidth = .16f; stream.numCapVertices = 5; stream.sharedMaterial = waterMaterial; stream.useWorldSpace = true;
                var hose = Group("Besleme hortumu", root.transform).AddComponent<LineRenderer>(); hose.positionCount = 14; hose.startWidth = .055f; hose.endWidth = .055f; hose.numCapVertices = 4; hose.sharedMaterial = mats["YY_tealDark"]; hose.useWorldSpace = true;
                var impact = Group("Su zerrecikleri", root.transform).AddComponent<ParticleSystem>();
                var settings = impact.main; settings.startLifetime = .45f; settings.startSpeed = 1.5f; settings.startSize = .07f; settings.startColor = Hex("#C3ECE8"); settings.maxParticles = 90; settings.playOnAwake = false;
                var emission = impact.emission; emission.rateOverTime = 70; var shape = impact.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = .12f;
                impact.GetComponent<ParticleSystemRenderer>().sharedMaterial = waterMaterial;
                var fires = new FireTargetBinding[3];
                for (int i = 0; i < 3; i++)
                {
                    var p = stations["fire"] + new Vector3(-1.5f + i * 1.5f + (groupIndex - 2) * .25f, .04f, 1.8f + (groupIndex - 1) * .95f);
                    var target = Group("Odak " + i, root.transform, p); Model("Parcel", target.transform, Vector3.zero, 1.7f, i * 28);
                    var visual = Group("Stilize alev ve duman", target.transform, Vector3.up * .3f);
                    Shape("Alev dışı", PrimitiveType.Capsule, visual.transform, new Vector3(0, .4f, 0), new Vector3(.72f, .7f, .72f), mats["YY_coral"]);
                    Shape("Alev içi", PrimitiveType.Capsule, visual.transform, new Vector3(0, .2f, -.11f), new Vector3(.45f, .5f, .45f), mats["YY_mustard"]);
                    Shape("Alev ucu", PrimitiveType.Sphere, visual.transform, new Vector3(.1f, 1.04f, .08f), new Vector3(.22f, .45f, .22f), mats["YY_mustard"]);
                    for (int d = 0; d < 3; d++) Shape("Yumuşak duman", PrimitiveType.Sphere, visual.transform, new Vector3(.14f * d, 1.5f + d * .55f, .05f), Vector3.one * (.52f + d * .2f), mats["YY_silver"]);
                    var hit = target.AddComponent<BoxCollider>(); hit.center = new Vector3(0, .8f, 0); hit.size = new Vector3(1.1f, 1.7f, 1.1f);
                    var light = visual.AddComponent<Light>(); light.type = LightType.Point; light.color = Hex("#FFB554"); light.intensity = 1; light.range = 3;
                    fires[i] = new FireTargetBinding { hitCollider = hit, visualRoot = visual.transform, glow = light };
                }
                var manager = root.AddComponent<FirefighterExtinguishManager>();
                Serialized(manager, "worldCamera", camera); Serialized(manager, "nozzleTip", tip.transform); Serialized(manager, "waterStream", stream); Serialized(manager, "waterImpact", impact);
                Serialized(manager, "firefighterRoot", idil.transform); Serialized(manager, "nozzleRig", nozzle.transform); Serialized(manager, "rightNozzleGrip", rightGrip.transform); Serialized(manager, "leftNozzleGrip", leftGrip.transform); Serialized(manager, "supplyHose", hose); Serialized(manager, "progressManager", progress);
                foreach (var pair in new[] { ("spineBone","Spine"),("chestBone","Chest"),("rightUpperArmBone","RightUpperArm"),("rightLowerArmBone","RightLowerArm"),("rightHandBone","RightHand"),("leftUpperArmBone","LeftUpperArm"),("leftLowerArmBone","LeftLowerArm"),("leftHandBone","LeftHand") })
                    Serialized(manager, pair.Item1, bones.First(x => x.name == pair.Item2));
                var so = new SerializedObject(manager); var array = so.FindProperty("fires"); array.arraySize = fires.Length;
                for (int i = 0; i < fires.Length; i++)
                {
                    var f = array.GetArrayElementAtIndex(i); f.FindPropertyRelative("hitCollider").objectReferenceValue = fires[i].hitCollider; f.FindPropertyRelative("visualRoot").objectReferenceValue = fires[i].visualRoot; f.FindPropertyRelative("glow").objectReferenceValue = fires[i].glow;
                }
                so.ApplyModifiedPropertiesWithoutUndo(); Number(manager, "gameDuration", 36000); Number(manager, "extinguishSeconds", 3.2f); Number(manager, "sprayRadius", .55f); Number(manager, "aimPlaneHeight", .8f);
                fireManagers[key] = manager;
                if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
            }
        }

        static ControlOutput ApplyBeatEffect(YanYanaGraphAuthor g, ControlOutput path, YYBeat beat)
        {
            foreach (var fire in fireManagers) path = g.Active(path, fire.Value.gameObject, fire.Key == beat.effect);
            if (beat.effect == "quake_start" || beat.effect == "aftershock") path = g.SetVar(path, "Quake", true, flow);
            if (beat.id == "sibling_choice" || beat.id == "stairs_check") path = g.SetVar(path, "Quake", false, flow);
            if (beat.id == "crouch" || beat.id == "cover_head" || beat.id == "hold_table" || beat.id == "hold_after" || beat.id == "aftershock")
            {
                foreach (string name in new[] { "Ada", "Efe" }) path = g.Do(path, typeof(Animator), "SetInteger", cast[name].GetComponentInChildren<Animator>(), new[] { typeof(string), typeof(int) }, "Pose", 1);
            }
            else foreach (string name in new[] { "Ada", "Efe" }) path = g.Do(path, typeof(Animator), "SetInteger", cast[name].GetComponentInChildren<Animator>(), new[] { typeof(string), typeof(int) }, "Pose", 0);
            if (beat.chapter >= 3 && beat.chapter <= 3) path = g.Set(path, typeof(Light), "intensity", sun, .32f);
            else path = g.Set(path, typeof(Light), "intensity", sun, 1.35f);
            if (beat.effect.StartsWith("ending_"))
            {
                var at = stations[beat.station];
                path = g.Set(path, typeof(Transform), "position", cast["Derya"].transform, at + new Vector3(-.5f, 0, 3));
                path = g.Set(path, typeof(Transform), "position", cast["Emre"].transform, at + new Vector3(.7f, 0, 3.25f));
                if (beat.effect == "ending_neighbor") path = g.Set(path, typeof(Transform), "position", cast["Yusuf"].transform, at + new Vector3(-1.5f, 0, .8f));
                if (beat.effect == "ending_signal" || beat.effect == "ending_detour") path = g.Do(path, typeof(StoryPlayerMovement), "Warp", movers["Bora"], new[] { typeof(Vector3) }, at + new Vector3(2, 0, .6f));
            }
            return path;
        }

        static ControlOutput ApplyActionEffect(YanYanaGraphAuthor g, ControlOutput path, YYBeat beat, YYAction action)
        {
            if (action.effect == "light_on") path = g.Set(path, typeof(Light), "intensity", torch, 2.2f);
            if (action.effect == "wear_bag") path = g.Active(path, bagWorn, true);
            if (action.effect == "adult_clear") path = g.Active(path, exitBlock, false);
            if (action.gesture == "spray") path = g.Send(path, flow, "ActivityFinished", beat.id, "completed");
            if (beat.id == "fire_assistance") path = g.Send(path, flow, "ActivityFinished", "firefighter", "team");
            if (action.effect == "quake_start") path = g.SetVar(path, "Quake", true, flow);
            return path;
        }

        static void ConfigureWorldState()
        {
            main.Initial("Quake", false);
            var role = main.Event("EnterRole", true, 1);
            var changed = main.Branch(role.trigger, Is(main, main.Var("Role"), role.argumentPorts[0]));
            var path = main.SetVar(changed.ifFalse, "Busy", true);
            path = main.SetVar(path, "RoleTransition", true);
            path = main.SetVar(path, "Role", role.argumentPorts[0]);
            path = Text(main, path, roleBannerText, Concat(main, "Şimdi ", role.argumentPorts[0]));
            path = main.Active(path, rolePanel, true); path = main.Wait(path, 1.2f); path = main.Active(path, rolePanel, false); path = Released(main, path);
            path = main.SetVar(path, "RoleTransition", false);
            var join = main.Add(new Unity.VisualScripting.Sequence { outputCount = 1 }); main.Link(path, join.enter); main.Link(changed.ifTrue, join.enter); path = join.multiOutputs[0];
            foreach (var item in movers)
            {
                path = main.Do(path, typeof(StoryPlayerMovement), "Stop", item.Value, NoArgs);
                path = main.Do(path, typeof(StoryPlayerMovement), "SetStoryInputLocked", item.Value, OneBool, Is(main, Is(main, role.argumentPorts[0], item.Key), false));
            }
            var select = main.Add(new SwitchOnString { options = movers.Keys.ToList() }); main.Bind(select.selector, role.argumentPorts[0]); main.Link(path, select.enter);
            foreach (var b in select.branches)
            {
                var p = main.SetVar(b.Value, "ActiveMover", movers[b.Key]);
                p = main.Do(p, typeof(StorySiblingFollower), "SetFollowing", follower, OneBool, b.Key == "Ada");
            }
            var apply = main.Event("ApplyWorld"); path = apply.trigger;
            path = main.Active(path, shelfFixed, Is(main, main.Var("ShelfSecured"), 1));
            path = main.Active(path, wardrobeFixed, Is(main, main.Var("WardrobeSecured"), 1));
            path = main.Active(path, shelfDamage, And(main, main.Binary<GreaterOrEqual>(main.Var("Chapter"), 3), Is(main, main.Var("ShelfSecured"), 0)));
            path = main.Active(path, wardrobeDamage, And(main, main.Binary<GreaterOrEqual>(main.Var("Chapter"), 3), Is(main, main.Var("WardrobeSecured"), 0)));
            path = main.Active(path, exitBlock, Is(main, main.Var("ExitCleared"), 0));
            path = main.Active(path, bagWorn, main.Binary<GreaterOrEqual>(main.Var("Chapter"), 4));
            path = main.Active(path, comfortWorn, Is(main, main.Var("Comfort"), 1));
            path = main.Active(path, mainBarricade, Or(main, Is(main, main.Var("FacadeReported"), 1), Is(main, main.Var("FireAssisted"), 1)));
            var neighbor = main.Branch(path, And(main, Is(main, main.Var("NeighborTogether"), 1), main.Binary<GreaterOrEqual>(main.Var("Chapter"), 5)));
            main.Set(neighbor.ifTrue, typeof(Transform), "position", cast["Yusuf"].transform, new Vector3(-5, 0, 87));
            var finish = main.Event("FinishAdventure"); path = main.Active(finish.trigger, endingPanel, true); path = main.SetVar(path, "Busy", true);
            var ending = main.Add(new SwitchOnInteger { options = new System.Collections.Generic.List<int> { 1, 2, 3, 4 } }); main.Bind(ending.selector, main.Var("Ending")); main.Link(path, ending.enter);
            for (int i = 0; i < 4; i++) Text(main, ending.branches[i].Value, endingText, campaign.endings[i]);
            var cards = main.Event("FinishAdventure");
            var f = main.Branch(cards.trigger, Is(main, main.Var("FlashlightReady"), 1));
            Text(main, f.ifTrue, causeCards[0], "Feneri denedin →\nKaranlıkta kullandın."); Text(main, f.ifFalse, causeCards[0], "Fener hazır değildi →\nAcil aydınlatmayı buldun.");
            var second = main.Event("FinishAdventure"); var s = main.Branch(second.trigger, Is(main, main.Var("FamilyPlan"), 1));
            Text(main, s.ifTrue, causeCards[1], "Buluşma yerini öğrendin →\nAile işaretini tanıdın."); Text(main, s.ifFalse, causeCards[1], "Planı tamamlamadın →\nYardımla ailene ulaştın.");
            var third = main.Event("FinishAdventure"); var n = main.Branch(third.trigger, Is(main, main.Var("NeighborTogether"), 1));
            Text(main, n.ifTrue, causeCards[2], "Komşunu dinledin →\nBirlikte yol aldınız."); Text(main, n.ifFalse, causeCards[2], "Komşunun yerini bildirdin →\nGörevli desteği ulaştı.");
            main.Dirty();
        }

        static void ConfigureAmbientMotion()
        {
            var update = main.Add(new Unity.VisualScripting.Update()); var seq = main.Add(new Unity.VisualScripting.Sequence { outputCount = 3 }); main.Link(update.trigger, seq.enter);
            var rect = main.Get(typeof(Screen), "safeArea"); var width = main.Get(typeof(Screen), "width"); var height = main.Get(typeof(Screen), "height");
            var min = V2(main, main.Binary<ScalarDivide>(main.Get(typeof(Rect), "xMin", rect), width), main.Binary<ScalarDivide>(main.Get(typeof(Rect), "yMin", rect), height));
            var max = V2(main, main.Binary<ScalarDivide>(main.Get(typeof(Rect), "xMax", rect), width), main.Binary<ScalarDivide>(main.Get(typeof(Rect), "yMax", rect), height));
            var p = main.Set(seq.multiOutputs[0], typeof(RectTransform), "anchorMin", safeRect, min); main.Set(p, typeof(RectTransform), "anchorMax", safeRect, max);
            var running = main.Branch(seq.multiOutputs[1], Is(main, main.Var("Paused"), false));
            p = main.SetVar(running.ifTrue, "PlaySeconds", Sum(main, main.Var("PlaySeconds"), main.Get(typeof(Time), "deltaTime")));
            var available = main.Branch(p, Is(main, main.Var("Busy"), false));
            p = main.SetVar(available.ifTrue, "IdleSeconds", Sum(main, main.Var("IdleSeconds"), main.Get(typeof(Time), "deltaTime")));
            var hints = main.Add(new Unity.VisualScripting.Sequence { outputCount = 2 }); main.Link(p, hints.enter);
            var longIdle = main.Branch(hints.multiOutputs[0], main.Binary<Greater>(main.Var("IdleSeconds"), 45f)); main.Active(longIdle.ifTrue, hintPanel, true);
            var mediumIdle = main.Branch(hints.multiOutputs[1], main.Binary<Greater>(main.Var("IdleSeconds"), 25f)); main.Send(mediumIdle.ifTrue, flow, "ShowHint");
            var help = main.Event("ShowHint"); var sw = main.Add(new SwitchOnString { options = new System.Collections.Generic.List<string> { "tap", "drag", "hold", "approach", "swipe", "spray" } }); main.Bind(sw.selector, main.Var("ActiveGesture")); main.Link(help.trigger, sw.enter);
            foreach (var branch in sw.branches) Text(main, branch.Value, gestureText, GestureCaption(branch.Key));
            // Quake motion is authored on the environment, with a calm camera by default.
            var quake = main.Branch(seq.multiOutputs[2], And(main, main.Var("Quake"), Is(main, main.Var("Paused"), false)));
            var sine = main.Call(typeof(Mathf), "Sin", null, OneFloat, main.Binary<ScalarMultiply>(main.Get(typeof(Time), "time"), 21f)).result;
            var angle = main.Binary<ScalarMultiply>(sine, .2f);
            main.Set(quake.ifTrue, typeof(Transform), "localEulerAngles", world.transform, V3(main, 0f, 0f, angle));
            main.Set(quake.ifFalse, typeof(Transform), "localEulerAngles", world.transform, Vector3.zero);
            // Empty-ground input uses the existing navigation component.
            foreach (var collider in world.GetComponentsInChildren<BoxCollider>())
            {
                if (!collider.name.Contains("zemin") && !collider.name.Contains("zemini") && !collider.name.Contains("açık yol")) continue;
                var g = new YanYanaGraphAuthor(collider.gameObject, "Zemine dokunarak yürüme"); var click = g.Add(new OnPointerClick()); g.Bind(click.target, collider.gameObject);
                var allowed = g.Branch(click.trigger, And(g, Available(g), Is(g, g.Var("ActiveGesture", flow), "approach")));
                var hit = g.Get(typeof(PointerEventData), "pointerCurrentRaycast", click.data); var point = g.Get(typeof(RaycastResult), "worldPosition", hit);
                g.Do(allowed.ifTrue, typeof(StoryPlayerMovement), "TrySetDestination", g.Var("ActiveMover", flow), new[] { typeof(Vector3) }, point); g.Dirty();
            }
            main.Dirty();
        }
    }
}
