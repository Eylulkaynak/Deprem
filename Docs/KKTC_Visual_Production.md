# KKTC visual production — 8 September 2026

Status: implemented and verified; the current Rebuild visual pass is ready for the user's visual review. Final voiceover and gameplay redesign remain in their agreed later pass.

## Locked direction

- Only Story_01–04_RebuildPreview and their shared art/builders.
- Polished stylized 3D matching ApprovedFamily_APose_Lineup.
- Contemporary KKTC apartment; natural, lightly local dialogue.
- Subtitles first. Final voiceover is deferred; stale voice bindings must be removed.
- No new runtime behaviour or save schema. Editor, Blender, existing managers and authored animation only.

## Recovery and baseline

- The project was closed. Started Unity 6000.0.58f2 for Deprem; the unrelated CraftBeerSimulator editor was left intact.
- Pre-existing edits backed up under `.codex_tmp/kktc_visual_20260908/baseline` before production changes.
- Baseline four-scene item inventories and overview renders: `ClientExports/KKTC/Before`.
- Unity log: `Logs/KKTC_VisualProduction.log`.

## Implemented

- Reproducible Blender modelling tool: `Tools/ArtProduction/build_kktc_props.py`.
- 31 authored prop FBXs, editable Blender sources, review images and common URP palette.
- Open/closed backpack from one construction, lining, straps, hardware and explicit grip/packing anchors.
- Flashlight with separate power button and lens anchor; radio with open, dimensioned two-AA battery well and hinged cover.
- Existing scene items migrated through editor-only `StoryKktcArtLibrary`; original interaction roots/IDs retained.
- Migration inventories under `ClientExports/KKTC/Reports`.
- Eight refined Humanoid models (Deniz, Can, Anne, Baba, Nermin/Komsu, Police, Firefighter, RescueWorker), packed-texture Blender sources, Unity mesh assets/materials/prefabs. These refine the existing reference-based meshes; they are not claimed as new character sculpts.
- One backpack construction in open/closed states, equal carried/floor scale, curved padded straps. Packed results use the loose item's physical scale.
- Flashlight switch Animator driven by the existing interaction UnityEvents; light anchored at the lens. Existing sound events retained.
- Radio socket/contact/cover geometry aligned with the existing battery insertion and removal interaction.
- Shared home dressing: Lefkara-inspired cloth, basket, ceramics, coffee corner, books, family photo, balcony plants, neighbor facades and closed camera-visible wall/ceiling openings.
- Warm key lighting with local material copies; static prop palette atlases and combined fence geometry preserve the existing Scene04 renderer budget.
- KKTC apartment/street/help-point labels and Civil Defence clothing identifiers.
- Dialogue content replacements across the four existing directors and serialized scene content. All four scenes have zero old dialogue voice bindings; subtitle-driven face bindings remain.
- Rebuild builder hooks apply the ready art library before final scene validation.
- Authored camera environment scan: 168 interior viewpoints/short transition samples at 9:16 and 9:19.5, zero uncovered pixels in the last completed scan. Under-table and corridor shots were separately moved inside the completed room shell after image review.
- Actual walking-camera capture exposed Deoccluder orientation drift above the ceiling and a close camera inside the family group. Native Cinemachine Pan Tilt and Confiner 3D now retain the authored angle and interior bounds. An authored camera Animator preserves the first ten seconds of the opening family shot, then prepares the rear view used for carrying. Scene02's conversation view includes the door-side plan; its instruction now names that location correctly.
- The 1080×2340 run also exposed clipped wheel/crouch targets in Story03. The table and quake lenses are now 54 degrees and the inside-wall cover shot is 60 degrees. The closing corridor camera now starts behind the children's actual standing position. The final reunion is a 50-degree wide view covering the plaza approach as well as the parents; the shelter fascia is above their faces. These changes apply through the existing builder art hook.
- An authored RectTransform dock moves the existing yellow context panel below the battery drag path while keeping the original panel Animator. No UI runtime code was added.
- Story03's opening car, wheel and drawing now sit on the visible tabletop. Its old bounds calculation included the inactive FamilyBoardGame and raised all three about 25 cm. The builder measures SafeTable_Visual, aligns the existing top collider, and creates the original drag/drop bindings from those positions. The existing placement test now also limits the vertical air gap to 2 cm.
- A fresh Story03 generation exposed a separate Timeline authoring defect: default root-pose removal without track offsets moved the secured wardrobe and shelf to the room origin. The builder now writes each prop track's authored position/rotation and saves its playable settings. The existing PlayMode cover check also rejects secured furniture moving more than 25 cm from its starting position.
- All four builders successfully regenerated their Rebuild scenes with the art pass. Fresh generation exposed and fixed parent-before-child bag anchor placement, null TMP label content, and a stale Story03 validator lens limit that disagreed with its existing 56-degree overview.
- Focused EditMode run: 122 passed, 0 failed (`Focused_EditMode_Passed.xml`).
- Scope comparison against the initial workspace snapshot: exactly four runtime directors changed, string literals only; no new or changed runtime behaviour (`ScopeAudit.json`).

## Delivery verification

Final camera and backpack contact changes are implemented. The review video is `Recordings/RebuildFull/Story_Rebuild_KKTC_Reviewed_20260908.mp4`: 15:37.47, 1080×2340, 30 fps, normal speed, with ambient and interaction audio. It combines the inspected first two chapters from the 15:42 recording with the complete, corrected last two chapters from the 16:24 recording. The only edit is at the chapter boundary; source recordings, exact cut time and hashes are in `ClientExports/KKTC/Reports/NormalSpeedRecording_Assembly.json`. The brief scene-load blackout at this boundary is retained. These are automated touch/navigation runs with fresh chapter loads; save/flag continuity is tested separately.

All ten-second contact sheets from both source recordings were inspected, with additional full-resolution samples for battery insertion/removal, carrying, cover, corridor actions and reunion. The assembled video's boundary and stream metadata were checked. The final focused EditMode batch passed 122/122; the current 11-check PlayMode evidence index includes the complete Story03 route and actual family framing checks. The two obsolete pre-production PlayMode tests described below are not counted as passing.

The existing assembly interaction still permits activating the reunion without walking all the way to the parents. All four family members are now visible in the authored wide shot; the movement inconsistency is recorded in `Docs/KKTC_Gameplay_Followup.md` for the gameplay pass. New gameplay mechanics are outside this pass.

## Evidence and reproduction

- Hero object renders: `ClientExports/KKTC/Hero` (authored states, not gameplay proof).
- Camera sheets: `ClientExports/KKTC/CameraSheets`; full-size captures in `Temp/StoryCameraQA`.
- Models: `Assets/Story/Art/KKTC`; editable sources: `ArtDirection/KKTC`.
- Blender production scripts: `Tools/ArtProduction`.
- Unity authoring menus: `Tools/Deprem Story/KKTC`, numbered prepare/apply/capture/verification actions.
- Runtime recordings use the existing `Tools/Deprem Story/Video/Record Full Rebuild Walkthrough (Normal Speed + Audio)` workflow.

## Runtime verification log

The first 13-check PlayMode batch passed 8 and failed 5 (`PlayMode_InitialFindings.xml`). Two failures referred to interactions already absent from the pre-production Rebuild builder: `Blackout_OpenOuterPocket` and the old interactable on `BandageSealInspection`/`WaterExpiryLabel_Unchecked`. Those obsolete tests remain in the repository and are excluded from the current 11-check Rebuild batch; their failures are retained in the report.

The other three failures identified two real issues: the old battery staging clip moved the physical battery back outside the new radio shot, and the new inside-room under-table shot cut off the grip target. The existing battery animation endpoint was retargeted in the Editor using Unity's serialized `m_LocalPosition` curve bindings; the under-table camera was widened/moved back while remaining inside the front wall. The corrected under-table gestures and Story02/03/04 walks passed in `PlayMode_10Passed_1Pending.xml`; the corrected Story01 walk then passed in `Story01_PlayMode_Passed.xml`. All 11 current checks have passing evidence. Original reports are preserved instead of rewriting their failed history.

The first 13:44 normal-speed video exposed two additional camera defects that static coverage checks could not detect. Editor-only `StoryKktcTrackingQA` records the actual Cinemachine position and sampled frames during existing PlayMode tests. Runtime walkthrough assertions now also reject tracking cameras above the apartment or inside the player. All 11 checks passed with these assertions. Final validation follows the last strap adjustment, opening-camera clip and Scene02 composition refinement.

The subsequent 14:39, 1080×1920 recording reached the four configured checkpoint targets with authored dialogue pacing and visible 0.7-second drag gestures in the test driver. Inspection used timestamped ten-second contact sheets across the full video and full-resolution samples for battery insertion/removal and carrying. The 9:19.5 checks then found the additional framing issues above (`PlayMode_TallPortrait_InitialFindings.xml`, `EditMode_TallPortrait_InitialFindings.xml`). The mouth-envelope test also sampled only twelve frames, which could miss a subtitle pulse at high editor frame rates; it now samples 0.6 seconds while retaining the inactive-mouth and gaze assertions. The revised 1080×2340 EditMode run passed 122/122.

The 14:50, 1080×2340 capture was then reviewed across all eight ten-second contact sheets and at full resolution for insertion, removal, carrying, cover and the closing shots. This exposed a limitation in the pre-existing walkthrough itself: Story03's CorridorReached checkpoint is committed before its five corridor beats. Earlier successful walkthrough reports therefore prove arrival at that checkpoint, not completion of those last five actions. The test driver now also requires the existing sequence director's Completed phase. The corridor and reunion walkthroughs additionally project actual family positions into both portrait aspect ratios and check clearance from the subtitle/objective areas. Earlier evidence is retained with this qualification. The complete Story03 route and both children in the closing corridor pass in `PlayMode_Story03Complete_Passed.xml`; the four family members at the reunion pass in `PlayMode_Reunion_Passed.xml`.

Two EditMode assertions still encoded the former camera configuration (`EditMode_CameraContract_BeforeUpdate.xml`). The opening keeps its original 44-degree, three-quarter composition, while obstacle resolution now pulls forward within the camera bounds; Scene02 allows the authored 54-degree portrait conversation view. The revised focused suite passed 122/122.

The user's existing `story-session.json` was backed up before any PlayMode test to `.codex_tmp/kktc_visual_20260908/pre_playmode_story-session.json`. It was restored after all PlayMode and EditMode runs exited and verified byte-for-byte by SHA-256 equality (`ClientExports/KKTC/Reports/SaveRestoration.json`). Restoration happens after teardown because teardown can write the test state after the recording test's own restoration.

## References

- Approved family reference in `ArtDirection/CharacterReferences`.
- Northern Cyprus handicrafts: https://www.visitncy.com/discover/traditional-handicrafts/
- KKTC Civil Defence: https://sivilsavunma.gov.ct.tr/

Rendered previews alone do not establish interaction or gameplay correctness.
