using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    public enum MapNodeType { Battle, Elite, Event, Chest, Rest, Shop, Boss }

    public class MapNode
    {
        public int floor, index;
        public MapNodeType type;
        public Vector2 pos;
        public bool visited, reachable;

        public string Label => type switch
        {
            MapNodeType.Battle => "魔",
            MapNodeType.Elite  => "精",
            MapNodeType.Event  => "?",
            MapNodeType.Chest  => "箱",
            MapNodeType.Rest   => "神",
            MapNodeType.Shop   => "商",
            MapNodeType.Boss   => "B",
            _ => ""
        };

        public Color NodeColor => type switch
        {
            MapNodeType.Battle => new Color(0.85f, 0.3f, 0.25f),
            MapNodeType.Elite  => new Color(1f, 0.5f, 0.15f),
            MapNodeType.Event  => new Color(0.25f, 0.5f, 0.95f),
            MapNodeType.Chest  => new Color(1f, 0.85f, 0.2f),
            MapNodeType.Rest   => new Color(0.25f, 0.8f, 0.35f),
            MapNodeType.Shop   => new Color(0.9f, 0.6f, 0.3f),
            MapNodeType.Boss   => new Color(0.7f, 0.15f, 0.7f),
            _ => Color.gray
        };

        public string Symbol => type switch
        {
            MapNodeType.Battle => "⚔",
            MapNodeType.Elite  => "☠",
            MapNodeType.Event  => "?",
            MapNodeType.Chest  => "♢",
            MapNodeType.Rest   => "♨",
            MapNodeType.Shop   => "♢",
            MapNodeType.Boss   => "☀",
            _ => ""
        };
    }

    public class MapData
    {
        public List<List<MapNode>> floors = new();
        public MapNode startNode, bossNode;
        public MapNode currentNode;
        public int totalFloors;

        public static MapData Generate(int stage)
        {
            var map = new MapData();
            int n = 9 + Mathf.Min(stage + 2, 6); // 11-15 floors, Slay the Spire-like
            map.totalFloors = n;

            // Floor 0: start
            var f0 = new List<MapNode>();
            var start = new MapNode { floor = 0, index = 0, type = MapNodeType.Rest, visited = true, reachable = true };
            f0.Add(start);
            map.floors.Add(f0);
            map.startNode = start;
            map.currentNode = start;

            // Middle floors: each with 2-4 nodes, all reachable
            for (int f = 1; f < n - 1; f++)
            {
                int cnt = Random.Range(2, 5);
                var floor = new List<MapNode>();
                for (int i = 0; i < cnt; i++)
                {
                    floor.Add(new MapNode {
                        floor = f, index = i,
                        type = RollType(f, n),
                        reachable = true
                    });
                }
                map.floors.Add(floor);
            }

            // Boss
            var fBoss = new List<MapNode>();
            fBoss.Add(new MapNode { floor = n - 1, index = 0, type = MapNodeType.Boss, reachable = true });
            map.floors.Add(fBoss);
            map.bossNode = fBoss[0];

            Layout(map);
            return map;
        }

        static MapNodeType RollType(int floor, int max)
        {
            if (floor == 1) return Random.value < 0.55f ? MapNodeType.Battle : MapNodeType.Event;
            if (floor == max - 2) return Random.value < 0.4f ? MapNodeType.Elite : MapNodeType.Rest;
            // Shop only on ~5% of nodes
            if (Random.value < 0.05f) return MapNodeType.Shop;
            float r = Random.value;
            return r < 0.28f ? MapNodeType.Battle
                 : r < 0.50f ? MapNodeType.Event
                 : r < 0.64f ? MapNodeType.Elite
                 : r < 0.82f ? MapNodeType.Chest
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
                    floor[i].pos = new Vector2(Mathf.Clamp01(t + Random.Range(-0.03f, 0.03f)), y);
                }
            }
        }

        /// <summary>Player can click any node on the next floor.</summary>
        public bool CanMoveTo(MapNode target)
        {
            if (currentNode == null) return target == startNode;
            if (target == currentNode) return false;
            if (!target.reachable) return false;
            return target.floor == currentNode.floor + 1;
        }
    }
}
