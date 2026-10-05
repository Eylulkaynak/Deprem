using System;
using System.Linq;
using UnityEngine;

namespace Deprem.Learning
{
    [Serializable] public sealed class LearningCatalog
    {
        public int version;
        public LearningCourse[] courses;
        public LearningArt[] art;
        public LearningGuide[] guides;
        public LearningField[] planFields, checklist;
        public LearningItem[] items;
        public LearningLevel[] levels;
        public LearningBadge[] badges;
        public static LearningCatalog Load() => JsonUtility.FromJson<LearningCatalog>(Resources.Load<TextAsset>("LearningApp/catalog").text);
        public LearningCourse[] Courses(bool adult) => courses.Where(c => c.adult == adult).ToArray();
        public LearningLevel[] Levels(bool adult) => levels.Where(c => c.adult == adult).ToArray();
        public LearningItem Item(string id) => items.FirstOrDefault(i => i.id == id);
    }
    [Serializable] public sealed class LearningArt { public string key, resource; }
    [Serializable] public sealed class LearningBadge { public string id, title, description; }
    [Serializable] public sealed class LearningCourse
    {
        public string id, title, subtitle, icon;
        public bool adult;
        public LearningExercise[] exercises;
    }
    [Serializable] public sealed class LearningExercise
    {
        public string kind, title, prompt, text, tip, success, icon;
        public LearningChoice[] choices;
    }
    [Serializable] public sealed class LearningChoice { public string label, icon, feedback; public bool correct; }
    [Serializable] public sealed class LearningGuide { public string title, tag, summary, iconKey; public bool adult; public string[] points; }
    [Serializable] public sealed class LearningField { public string key, label, placeholder; }
    [Serializable] public sealed class LearningItem { public string id, label, textureKey, sortZone, purpose; }
    [Serializable] public sealed class LearningTarget { public string id, label, accepts, textureKey; public bool dangerous; }
    [Serializable] public sealed class LearningRound { public LearningTarget[] steps; }
    [Serializable] public sealed class LearningPair { public string itemId, targetLabel; }
    [Serializable] public sealed class LearningCatchSpawn { public string id; public float x, at; public bool good; }
    [Serializable] public sealed class LearningLevel
    {
        public string id, type, title, shortTitle, instruction, previewTextureKey, successMessage;
        public bool adult;
        public string[] items, correctItems, packItems, goodItems, badItems, memoryPads, sequence;
        public string[] correctPool, packPool, negativePool;
        public LearningPair[] pairPool;
        public LearningCatchSpawn[] catchSpawns;
        public int catchTarget, sequenceLength, correctCount, packCount, negativeCount, matchCount, questionCount;
        public LearningTarget[] targets, actions, steps;
        public LearningRound[] rounds;
        public LearningExercise[] questions;
        public string SaveId => (adult ? "adult:" : "child:") + id;
        public LearningLevel CreateRound()
        {
            var round = JsonUtility.FromJson<LearningLevel>(JsonUtility.ToJson(this));
            T[] Pick<T>(T[] pool, int count) => pool.OrderBy(_ => UnityEngine.Random.value).Take(Math.Max(0, count)).ToArray();
            if (type == "bag")
            {
                round.correctItems = Pick(correctPool, correctCount);
                round.items = round.correctItems.Concat(Pick(negativePool, negativeCount)).OrderBy(_ => UnityEngine.Random.value).ToArray();
            }
            if (type == "sort")
            {
                round.packItems = Pick(packPool, packCount);
                round.items = round.packItems.Concat(Pick(negativePool, negativeCount)).OrderBy(_ => UnityEngine.Random.value).ToArray();
            }
            if (type == "match")
            {
                var pairs = Pick(pairPool, matchCount); round.items = pairs.Select(p => p.itemId).ToArray();
                round.targets = pairs.Select(p => new LearningTarget { id = p.itemId, accepts = p.itemId, label = p.targetLabel }).ToArray();
            }
            if (type == "quiz")
            {
                round.questions = Pick(round.questions, questionCount > 0 ? questionCount : questions.Length);
                foreach (var question in round.questions) question.choices = Pick(question.choices, question.choices.Length);
            }
            return round;
        }
    }
}
