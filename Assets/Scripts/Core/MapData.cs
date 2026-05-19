using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    public enum MapNodeType { Battle, Elite, Event, Chest, Rest, Shop, Boss, Remove }

    public class MapNode
    {
        public int floor, index;
        public MapNodeType type;
        public Vector2 pos;
        public bool visited, reachable;
        public List<int> connections = new(); // next floor indices this connects to

        public string Label => type switch
        {
            MapNodeType.Battle => "魔", MapNodeType.Elite => "精", MapNodeType.Event => "?",
            MapNodeType.Chest => "箱", MapNodeType.Rest => "神", MapNodeType.Shop => "商",
            MapNodeType.Boss => "B", MapNodeType.Remove => "删", _ => ""
        };

        public string Symbol => type switch
        {
            MapNodeType.Battle => "⚔", MapNodeType.Elite => "☠", MapNodeType.Event => "?",
            MapNodeType.Chest => "♢", MapNodeType.Rest => "♨", MapNodeType.Shop => "$",
            MapNodeType.Boss => "☀", MapNodeType.Remove => "✕", _ => ""
        };

        public Color NodeColor => type switch
        {
            MapNodeType.Battle => new Color(0.85f, 0.3f, 0.25f), MapNodeType.Elite => new Color(1f, 0.5f, 0.15f),
            MapNodeType.Event => new Color(0.25f, 0.5f, 0.95f), MapNodeType.Chest => new Color(1f, 0.85f, 0.2f),
            MapNodeType.Rest => new Color(0.25f, 0.8f, 0.35f), MapNodeType.Shop => new Color(0.9f, 0.6f, 0.3f),
            MapNodeType.Boss => new Color(0.7f, 0.15f, 0.7f), MapNodeType.Remove => new Color(0.6f, 0.3f, 0.3f),
            _ => Color.gray
        };
    }

    public class MapData
    {
        public List<List<MapNode>> floors = new();
        public MapNode startNode, bossNode, currentNode;
        public int totalFloors;

        public static MapData Generate(int stage)
        {
            var map = new MapData();
            int n = 20 + stage * 3; // 20-26 floors
            map.totalFloors = n;
            System.Random rng = new System.Random(stage * 100 + (int)(Time.time * 1000));

            // Floor 0: single start node
            var f0 = new List<MapNode>();
            var start = new MapNode { floor = 0, index = 0, type = MapNodeType.Rest, visited = true, reachable = true };
            start.connections.Add(0); // connects to first path node
            f0.Add(start);
            map.floors.Add(f0);
            map.startNode = start;
            map.currentNode = start;

            // Generate floors 1 to n-2 with branching
            int pathCount = 3; // number of parallel paths
            List<List<int>> pathNodes = new(); // per-path list of floor indices
            for (int p = 0; p < pathCount; p++) pathNodes.Add(new());

            // Floor 1: one entry per path
            var f1 = new List<MapNode>();
            for (int p = 0; p < pathCount; p++)
            {
                var node = new MapNode { floor = 1, index = p, type = RollType(1, n, rng), reachable = true };
                node.connections.Add(p); // each connects to its own path forward
                f1.Add(node);
                pathNodes[p].Add(p);
            }
            map.floors.Add(f1);

            // Floors 2 to n-2: branching with occasional merges
            int[] pathFloorCount = new int[pathCount]; // track which floor each path is at
            for (int p = 0; p < pathCount; p++) pathFloorCount[p] = 2;

            for (int f = 2; f < n - 1; f++)
            {
                var floor = new List<MapNode>();

                // Each active path generates a node
                for (int p = 0; p < pathCount; p++)
                {
                    if (pathFloorCount[p] > f) continue; // path not at this floor yet

                    var node = new MapNode { floor = f, index = floor.Count, type = RollType(f, n, rng), reachable = false };

                    // Connect to next floor on same path
                    node.connections.Add(floor.Count); // default: continue straight
                    floor.Add(node);
                    pathFloorCount[p] = f + 1;
                }

                // Occasionally merge paths (15% chance at floors 5-15)
                if (floor.Count >= 2 && f >= 5 && f <= n - 8 && rng.NextDouble() < 0.15f)
                {
                    // Merge last two nodes: both connect to a single next node
                    int mergedIdx = rng.Next(0, floor.Count - 1);
                    floor[mergedIdx].connections.Add(floor[mergedIdx + 1].connections[0]);
                }

                // Occasionally branch (20% chance): add an extra node
                if (floor.Count < 4 && f < n - 6 && rng.NextDouble() < 0.2f)
                {
                    var branch = new MapNode { floor = f, index = floor.Count, type = RollType(f, n, rng), reachable = false };
                    branch.connections.Add(floor.Count);
                    floor.Add(branch);
                }

                if (floor.Count == 0)
                {
                    // Ensure at least 1 node
                    floor.Add(new MapNode { floor = f, index = 0, type = RollType(f, n, rng), reachable = false });
                }

                map.floors.Add(floor);
            }

            // Fill in actual floor count
            n = map.floors.Count + 1;
            map.totalFloors = n;

            // Boss floor
            var fBoss = new List<MapNode>();
            fBoss.Add(new MapNode { floor = n - 1, index = 0, type = MapNodeType.Boss, reachable = false });
            map.floors.Add(fBoss);
            map.bossNode = fBoss[0];

            // Set all nodes on floor 1 as reachable from start
            foreach (var node in map.floors[1]) node.reachable = true;

            // Propagate reachability: any node connected FROM a reachable node on the previous floor is reachable
            for (int f = 2; f < map.floors.Count; f++)
            {
                var prevFloor = map.floors[f - 1];
                var currFloor = map.floors[f];
                for (int i = 0; i < currFloor.Count; i++)
                {
                    // Check if any node on previous floor connects to this one
                    for (int j = 0; j < prevFloor.Count; j++)
                    {
                        if (prevFloor[j].reachable && prevFloor[j].connections.Contains(i))
                        {
                            currFloor[i].reachable = true;
                            break;
                        }
                    }
                }
                // Also set nodes reachable if previous floor had reachable nodes (fallback)
                if (currFloor.Count == 1) currFloor[0].reachable = true;
            }

            Layout(map);
            return map;
        }

        static MapNodeType RollType(int floor, int max, System.Random rng)
        {
            if (floor == 1) return rng.NextDouble() < 0.4f ? MapNodeType.Battle : MapNodeType.Event;
            if (floor == max - 2) return MapNodeType.Rest; // pre-boss rest
            if (floor == max - 3 && rng.NextDouble() < 0.5f) return MapNodeType.Elite;
            if (rng.NextDouble() < 0.03f) return MapNodeType.Remove; // 3% card removal
            if (rng.NextDouble() < 0.06f) return MapNodeType.Shop;
            double r = rng.NextDouble();
            return r < 0.30f ? MapNodeType.Battle
                 : r < 0.50f ? MapNodeType.Event
                 : r < 0.62f ? MapNodeType.Elite
                 : r < 0.80f ? MapNodeType.Chest
                 : MapNodeType.Rest;
        }

        static void Layout(MapData map)
        {
            float yStep = 1f / (map.totalFloors - 1);
            for (int f = 0; f < map.totalFloors; f++)
            {
                var floor = map.floors[f];
                float y = 1f - f * yStep;
                for (int i = 0; i < floor.Count; i++)
                {
                    float t = (i + 1f) / (floor.Count + 1f);
                    floor[i].pos = new Vector2(Mathf.Clamp01(t + Random.Range(-0.02f, 0.02f)), y);
                }
            }
        }

        /// <summary>Player can move to any reachable node on the next floor.</summary>
        public bool CanMoveTo(MapNode target)
        {
            if (currentNode == null) return target == startNode;
            if (target == currentNode || !target.reachable) return false;
            if (target.floor != currentNode.floor + 1) return false;
            // Allow moving to adjacent nodes (same or ±1 index) on the next floor
            int curIdx = currentNode.index;
            return Mathf.Abs(target.index - curIdx) <= 1 || currentNode.connections.Contains(target.index);
        }
    }
}
