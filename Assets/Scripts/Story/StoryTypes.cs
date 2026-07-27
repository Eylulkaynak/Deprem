using System;
using System.Collections.Generic;

namespace Deprem.Story
{
    public enum StoryFlag
    {
        None = 0,
        BagFlashlight = 1,
        BagFirstAid = 2,
        BagWater = 3,
        BagRadio = 4,
        BagWhistle = 5,
        WardrobeSecured = 6,
        ShelfSecured = 7,
        ExitCleared = 8,
        BagBatteries = 9,
        BagFood = 10,
        BagDocuments = 11,
        BagNotebook = 12,
        BagSoap = 13,
        BagWetWipes = 14,
        BagBlanket = 15,
        BagClothing = 16,
        BagReady = 17,
        NeighborAssisted = 18,
        ElevatorAvoided = 19,
        StairRouteCompleted = 20,
        AssemblyHeadcountComplete = 21,
        BagComfortItem = 22
    }

    public enum StoryCheckpoint
    {
        None = 0,
        PreparationStart = 1,
        HomeSafetyStart = 2,
        QuakeStart = 3,
        SiblingCalmed = 4,
        UnderCover = 5,
        PostQuake = 6,
        CorridorReached = 7,
        EvacuationStart = 8,
        AssemblyAreaReached = 9,
        BagInspected = 10,
        CommunicationPacked = 11,
        FoodPacked = 12,
        HealthPacked = 13,
        WarmthPacked = 14,
        BagFitted = 15,
        PreparationComplete = 16,
        HomeExitCleared = 17,
        HomeShelfPrepared = 18,
        HomeWardrobeSecured = 19,
        HomeSafetyComplete = 20,
        StairwellEntered = 21,
        AftershockHeld = 22,
        NeighborHelped = 23,
        BuildingExited = 24,
        StreetRouteCleared = 25,
        AssemblyHeadcountComplete = 26
    }

    public enum StoryAct
    {
        Preparation = 1,
        HomeSafety = 2,
        Quake = 3,
        Evacuation = 4
    }

    public enum StoryInteractionKind
    {
        Inspect,
        Collect,
        HelpSibling,
        TakeCover,
        Exit,
        UnsafeChoice
    }

    public enum StoryInteractionGesture
    {
        Tap = 0,
        RepeatedTap,
        SwipeDown,
        SwipeHorizontal,
        Approach,
        WorldHold,
        DragToBag,
        DragToTarget
    }

    public enum StoryCameraZoneId
    {
        None = 0,
        RoomOverview = 1,
        QuakeClose = 2,
        UnderTable = 3,
        PostQuake = 4,
        Corridor = 5,
        InspectTable = 6,
        InspectWindow = 7,
        InspectWardrobe = 8,
        InspectExit = 9,
        InspectBrokenGlass = 10,
        PreparationOverview = 11,
        PreparationBag = 12,
        PreparationParent = 13,
        PreparationSignal = 14,
        PreparationFood = 15,
        PreparationHealth = 16,
        PreparationWarmth = 17,
        PreparationWrongChoice = 18,
        PreparationBagFit = 19,
        PreparationExitShelf = 20,
        HomeOverview = 21,
        HomeWardrobe = 22,
        HomeShelf = 23,
        HomeExit = 24,
        HomeParent = 25,
        HomeFinalTest = 26,
        EvacuationCorridor = 27,
        EvacuationStairsTop = 28,
        EvacuationLanding = 29,
        EvacuationNeighbor = 30,
        EvacuationBuildingFront = 31,
        EvacuationStreet = 32,
        EvacuationAssembly = 33,
        EvacuationHazard = 34,
        EvacuationElevator = 35,
        EvacuationStairDoor = 36,
        EvacuationBuildingDoor = 37,
        EvacuationLowerLanding = 38,
        EvacuationStreetInspect = 39,
        EvacuationAssemblyRadio = 40,
        PreparationSiblingHandoff = 41,
        PreparationFlashlight = 42
    }

    public enum StorySlicePhase
    {
        None = 0,
        CalmOpening,
        Quake,
        UnderCover,
        PostQuake,
        Corridor,
        Completed
    }

    [Serializable]
    public sealed class StorySessionState
    {
        public int schemaVersion = 1;
        public StoryAct activeAct = StoryAct.Quake;
        public StoryCheckpoint checkpoint = StoryCheckpoint.None;
        public string activeScene = "Story_03_Quake";
        public List<StoryFlag> flags = new List<StoryFlag>();
        public int mistakeCount;
        public List<StoryAct> completedActs = new List<StoryAct>();

        public bool HasFlag(StoryFlag flag)
        {
            return flag != StoryFlag.None && flags != null && flags.Contains(flag);
        }

        public void SetFlag(StoryFlag flag, bool value)
        {
            if (flag == StoryFlag.None)
                return;

            flags ??= new List<StoryFlag>();
            if (value)
            {
                if (!flags.Contains(flag))
                    flags.Add(flag);
            }
            else
            {
                flags.Remove(flag);
            }
        }

        public void Normalize()
        {
            schemaVersion = Math.Max(1, schemaVersion);
            flags ??= new List<StoryFlag>();
            completedActs ??= new List<StoryAct>();
            activeScene ??= string.Empty;
        }
    }
}
