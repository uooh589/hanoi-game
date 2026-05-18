using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    /// <summary>
    /// Core Tower of Hanoi puzzle logic.
    /// Manages 3 pegs as stacks of disk sizes (1 = smallest).
    /// </summary>
    [System.Serializable]
    public class HanoiPuzzle
    {
        public int diskCount;
        public int targetPeg; // 0/1/2 — randomly chosen destination
        public List<int> peg0 = new List<int>();
        public List<int> peg1 = new List<int>();
        public List<int> peg2 = new List<int>();
        public int stepsUsed;

        private List<int>[] _pegs;

        public HanoiPuzzle(int disks)
        {
            diskCount = disks;
            stepsUsed = 0;
            targetPeg = Random.Range(0, 3);
            InitPegs();
            GenerateRandomState();
        }

        private void InitPegs()
        {
            peg0 = new List<int>();
            peg1 = new List<int>();
            peg2 = new List<int>();
            _pegs = new List<int>[] { peg0, peg1, peg2 };
        }

        /// <summary>
        /// Rebuild peg array reference (call after deserialization).
        /// </summary>
        public void Rebuild()
        {
            _pegs = new List<int>[] { peg0, peg1, peg2 };
        }

        public List<int> GetPeg(int index)
        {
            if (_pegs == null) Rebuild();
            return _pegs[index];
        }

        /// <summary>
        /// Top disk on peg, or int.MaxValue if empty (sentinel for comparisons).
        /// </summary>
        public int TopDisk(int pegIndex)
        {
            var p = GetPeg(pegIndex);
            return p.Count > 0 ? p[p.Count - 1] : int.MaxValue;
        }

        /// <summary>
        /// Check if moving top disk from pegA to pegB is legal.
        /// </summary>
        public bool IsValidMove(int fromPeg, int toPeg)
        {
            if (fromPeg == toPeg) return false;
            var src = GetPeg(fromPeg);
            if (src.Count == 0) return false;
            int disk = src[src.Count - 1];
            var dst = GetPeg(toPeg);
            if (dst.Count > 0 && dst[dst.Count - 1] < disk) return false;
            return true;
        }

        /// <summary>
        /// Execute a move. Returns the disk size moved, or -1 if illegal.
        /// </summary>
        public int MoveDisk(int fromPeg, int toPeg)
        {
            if (!IsValidMove(fromPeg, toPeg)) return -1;
            var src = GetPeg(fromPeg);
            int disk = src[src.Count - 1];
            src.RemoveAt(src.Count - 1);
            GetPeg(toPeg).Add(disk);
            stepsUsed++;
            return disk;
        }

        /// <summary>
        /// Puzzle complete when all disks are on the target peg.
        /// </summary>
        public bool IsComplete()
        {
            return GetPeg(targetPeg).Count == diskCount;
        }

        /// <summary>Set puzzle to 1 move from completion: smallest disk on source, rest on target.</summary>
        public void SetOneMoveFromComplete()
        {
            peg0.Clear(); peg1.Clear(); peg2.Clear();
            int srcPeg = 0;
            while (srcPeg == targetPeg) srcPeg = Random.Range(0, 3);
            // All disks except smallest on target peg
            for (int i = diskCount; i > 1; i--)
                GetPeg(targetPeg).Add(i);
            // Smallest disk on source peg — only needs to move it to target
            GetPeg(srcPeg).Add(1);
        }

        public static int GetOptimalSteps(int disks)
        {
            return (1 << disks) - 1;
        }

        public int OptimalSteps => GetOptimalSteps(diskCount);

        /// <summary>
        /// Generate a random legal state, ensuring not already complete on target peg.
        /// Picks a new random target peg each time (except for task card which preserves state).
        /// </summary>
        public void GenerateRandomState(bool newTarget = true)
        {
            if (newTarget) targetPeg = Random.Range(0, 3);

            for (int i = 0; i < 3; i++) GetPeg(i).Clear();

            for (int disk = diskCount; disk >= 1; disk--)
            {
                var validPegs = new List<int>();
                for (int p = 0; p < 3; p++)
                    if (GetPeg(p).Count == 0 || TopDisk(p) > disk) validPegs.Add(p);
                GetPeg(validPegs[Random.Range(0, validPegs.Count)]).Add(disk);
            }

            // If already complete on target peg, move smallest disk off
            if (GetPeg(targetPeg).Count == diskCount)
            {
                int top = GetPeg(targetPeg)[^1];
                GetPeg(targetPeg).RemoveAt(GetPeg(targetPeg).Count - 1);
                int alt = (targetPeg + 1 + Random.Range(0, 1)) % 3;
                GetPeg(alt).Add(top);
            }

            stepsUsed = 0;
        }

        /// <summary>
        /// Reset to standard starting position (all disks on peg 0).
        /// </summary>
        public void ResetToStandard()
        {
            for (int i = 0; i < 3; i++) GetPeg(i).Clear();
            for (int disk = diskCount; disk >= 1; disk--)
                GetPeg(0).Add(disk);
            stepsUsed = 0;
        }

        /// <summary>
        /// Deep copy the peg state from a CardData's saved state.
        /// </summary>
        public void LoadFromCardData(CardData card)
        {
            diskCount = card.towerLevel;
            InitPegs();
            peg0 = new List<int>(card.peg0State);
            peg1 = new List<int>(card.peg1State);
            peg2 = new List<int>(card.peg2State);
            stepsUsed = card.taskStepsUsed;
            Rebuild();
        }

        /// <summary>
        /// Save current state into a CardData.
        /// </summary>
        public void SaveToCardData(CardData card)
        {
            card.peg0State = new List<int>(peg0);
            card.peg1State = new List<int>(peg1);
            card.peg2State = new List<int>(peg2);
            card.taskStepsUsed = stepsUsed;
        }
    }
}
