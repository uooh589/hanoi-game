using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HanoiGame
{
    /// <summary>
    /// Serializable container for all persistent game state.
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        // Deck
        public List<CardSaveEntry> deckEntries = new List<CardSaveEntry>();

        // Permanent stats
        public float stepMultiplier = 1.0f;
        public int permanentATKBonus;
        public int permanentThorns;
        public int maxHPBonus;
        public int currentStage = 1;

        // Task card persistent progress
        public bool hasTaskCard;
        public int taskPeg0Count;
        public int[] taskPeg0Items;
        public int taskPeg1Count;
        public int[] taskPeg1Items;
        public int taskPeg2Count;
        public int[] taskPeg2Items;
        public int taskStepsUsed;
        public int taskStepsAccumulated; // total accumulated task steps
    }

    [System.Serializable]
    public class CardSaveEntry
    {
        public int towerLevel;
        public string effectTypeName;
        public int effectValue1;
        public int effectValue2;
        public float effectValueF;
        public bool isTaskCard;
        public string effectDescription;
        public string id;
    }

    /// <summary>
    /// Handles JSON save/load to Application.persistentDataPath.
    /// </summary>
    public static class SaveManager
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "hanoi_save.json");

        public static bool SaveExists()
        {
            return File.Exists(SavePath);
        }

        /// <summary>
        /// Serialize current game state to JSON and write to disk.
        /// </summary>
        public static void Save(GameManager gm)
        {
            var data = new SaveData();

            // Save deck
            foreach (var card in gm.Deck.cards)
            {
                var entry = new CardSaveEntry
                {
                    towerLevel = card.towerLevel,
                    effectTypeName = card.effectType.ToString(),
                    effectValue1 = card.effectValue1,
                    effectValue2 = card.effectValue2,
                    effectValueF = card.effectValueF,
                    isTaskCard = card.isTaskCard,
                    effectDescription = card.effectDescription,
                    id = card.id,
                };
                data.deckEntries.Add(entry);
            }

            // Permanent stats
            data.stepMultiplier = gm.stepMultiplier;
            data.permanentATKBonus = gm.permanentATKBonus;
            data.permanentThorns = gm.permanentThorns;
            data.maxHPBonus = gm.maxHPBonus;
            data.currentStage = gm.currentStage;

            // Task card state
            var taskCard = gm.Deck.cards.Find(c => c.isTaskCard);
            if (taskCard != null)
            {
                data.hasTaskCard = true;
                data.taskPeg0Items = taskCard.peg0State?.ToArray() ?? new int[0];
                data.taskPeg1Items = taskCard.peg1State?.ToArray() ?? new int[0];
                data.taskPeg2Items = taskCard.peg2State?.ToArray() ?? new int[0];
                data.taskStepsUsed = taskCard.taskStepsUsed;
            }

            data.taskStepsAccumulated = gm.taskSteps;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] Saved to {SavePath}");
        }

        /// <summary>
        /// Load game state from JSON. Returns null if no save file.
        /// </summary>
        public static SaveData Load()
        {
            if (!SaveExists()) return null;

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);

            // Validate
            if (data == null || data.deckEntries == null || data.deckEntries.Count == 0)
                return null;

            Debug.Log($"[SaveManager] Loaded from {SavePath}");
            return data;
        }

        /// <summary>
        /// Delete save file.
        /// </summary>
        public static void DeleteSave()
        {
            if (SaveExists())
                File.Delete(SavePath);
        }

        /// <summary>
        /// Apply loaded save data to GameManager.
        /// </summary>
        public static void ApplySaveData(GameManager gm, SaveData data)
        {
            gm.stepMultiplier = data.stepMultiplier;
            gm.permanentATKBonus = data.permanentATKBonus;
            gm.permanentThorns = data.permanentThorns;
            gm.maxHPBonus = data.maxHPBonus;
            gm.currentStage = data.currentStage;
            gm.taskSteps = data.taskStepsAccumulated;

            gm.Deck.cards.Clear();
            foreach (var entry in data.deckEntries)
            {
                var card = new CardData
                {
                    id = entry.id,
                    towerLevel = entry.towerLevel,
                    effectType = (EffectType)Enum.Parse(typeof(EffectType), entry.effectTypeName),
                    effectValue1 = entry.effectValue1,
                    effectValue2 = entry.effectValue2,
                    effectValueF = entry.effectValueF,
                    isTaskCard = entry.isTaskCard,
                    effectDescription = entry.effectDescription,
                };

                // Restore task card peg state
                if (card.isTaskCard && data.hasTaskCard)
                {
                    card.peg0State = new List<int>(data.taskPeg0Items ?? new int[0]);
                    card.peg1State = new List<int>(data.taskPeg1Items ?? new int[0]);
                    card.peg2State = new List<int>(data.taskPeg2Items ?? new int[0]);
                    card.taskStepsUsed = data.taskStepsUsed;
                }
                gm.Deck.AddCard(card);
            }

            // Ensure task card exists
            if (!gm.Deck.cards.Exists(c => c.isTaskCard))
            {
                var taskCard = EffectPool.Pool[8][0];
                var cd = new CardData
                {
                    towerLevel = 8,
                    effectType = EffectType.TaskStepMultiplier,
                    effectValueF = 0.1f,
                    isTaskCard = true,
                    effectDescription = taskCard.Format(8),
                };
                gm.Deck.AddCard(cd);
            }
        }
    }
}
