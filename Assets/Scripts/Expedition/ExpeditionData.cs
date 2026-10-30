using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnbridledSanctum.Expedition
{
    public enum RoomType
    {
        Start,
        Empty,
        Combat,
        Event,
        Treasure,
        Exit
    }

    [Serializable]
    public class DungeonRoom
    {
        public Vector2Int gridPos;
        public RoomType type;
        public bool isDiscovered;
        public bool isVisited;
        public bool isResolved;

        public string title;
        public string description;

        public DungeonRoom(Vector2Int pos, RoomType t)
        {
            gridPos = pos;
            type = t;
            isDiscovered = false;
            isVisited = false;
            // Start and Empty rooms are always immediately resolved
            isResolved = (t == RoomType.Start || t == RoomType.Empty);
        }
    }

    public class DungeonFloor
    {
        public int floorNumber = 1;
        public Vector2Int startPos = Vector2Int.zero;
        public Vector2Int exitPos = Vector2Int.zero;
        public Vector2Int currentPos = Vector2Int.zero;

        public Dictionary<Vector2Int, DungeonRoom> rooms = new Dictionary<Vector2Int, DungeonRoom>();

        public DungeonRoom CurrentRoom
        {
            get
            {
                rooms.TryGetValue(currentPos, out DungeonRoom r);
                return r;
            }
        }

        public bool HasRoom(Vector2Int pos) => rooms.ContainsKey(pos);

        public bool CanMoveTo(Vector2Int targetPos)
        {
            if (!rooms.ContainsKey(targetPos)) return false;
            // Can only move to orthogonally adjacent rooms
            int dist = Math.Abs(targetPos.x - currentPos.x) + Math.Abs(targetPos.y - currentPos.y);
            return dist == 1;
        }
    }

    public static class DungeonFloorGenerator
    {
        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 0)    // Right
        };

        private static readonly string[] CombatTitles = new string[]
        {
            "Dark Ambush",
            "Cavern Slime",
            "Goblin Scout",
            "Vampire Bats"
        };

        private static readonly string[] CombatDescriptions = new string[]
        {
            "A feral cavern creature leaps out from a fissure with bared fangs!",
            "An acidic translucent slime drops from the cavern ceiling, blocking your passage!",
            "A wild goblin scout springs from the shadows, brandishing a rusty dagger!",
            "A swarm of hostile cave bats swoops down fiercely from the vaulted ceiling!"
        };

        private static readonly string[] EventTitles = new string[]
        {
            "Underground Spring",
            "Ancient Stele",
            "Abandoned Camp",
            "Glowing Fungi"
        };

        private static readonly string[] EventDescriptions = new string[]
        {
            "A crystal-clear natural spring bubbles in a stone recess, glowing faintly with azure light.",
            "You find an ancient stone obelisk inscribed with forgotten runes of an ancient era.",
            "The remnants of a long-abandoned campfire. The ashes are cold, but an aura of quiet safety lingers.",
            "A patch of soft bioluminescent mushrooms illuminates the corridor with an ethereal violet glow."
        };

        private static readonly string[] TreasureTitles = new string[]
        {
            "Smuggler Cache",
            "Forgotten Knapsack",
            "Ironbound Chest",
            "Crystal Vein"
        };

        private static readonly string[] TreasureDescriptions = new string[]
        {
            "You discovered a hidden wooden crate bound with weathered iron straps!",
            "A traveler's leather pack rests against the rock wall. Something metallic jingles inside!",
            "Concealed within a deep wall fissure lies an antique locked coffer with an intact lock!",
            "A rich cluster of luminous mana crystals gleams invitingly along the stone wall."
        };

        private static readonly string[] EmptyDescriptions = new string[]
        {
            "A quiet damp cavern. Only slow dripping water breaks the subterranean stillness.",
            "An empty passageway with smooth basalt walls. Nothing moves here.",
            "A small dry grotto strewn with fine gravel. The way is completely clear.",
            "A wide corridor with a gentle subterranean breeze. No hazards detected."
        };

        public static DungeonFloor Generate(int floorNumber)
        {
            DungeonFloor floor = new DungeonFloor();
            floor.floorNumber = floorNumber;

            int targetRoomCount = Mathf.Clamp(10 + floorNumber * 2, 10, 20);

            // Start room
            Vector2Int start = Vector2Int.zero;
            floor.startPos = start;
            floor.currentPos = start;

            DungeonRoom startRoom = new DungeonRoom(start, RoomType.Start);
            startRoom.title = "Dungeon Entrance";
            startRoom.description = "You have entered this sector of the catacombs. Safe passage lies behind you; unexplored depths lie ahead.";
            startRoom.isDiscovered = true;
            startRoom.isVisited = true;
            startRoom.isResolved = true; // Never trigger popups on start room!
            floor.rooms.Add(start, startRoom);

            List<Vector2Int> roomPositions = new List<Vector2Int> { start };

            // Organic Random Walk / Branching room expansion
            int attempts = 0;
            while (roomPositions.Count < targetRoomCount && attempts < 1000)
            {
                attempts++;
                Vector2Int basePos = roomPositions[UnityEngine.Random.Range(0, roomPositions.Count)];
                Vector2Int dir = Directions[UnityEngine.Random.Range(0, Directions.Length)];
                Vector2Int newPos = basePos + dir;

                if (!floor.rooms.ContainsKey(newPos))
                {
                    DungeonRoom newRoom = new DungeonRoom(newPos, RoomType.Empty);
                    floor.rooms.Add(newPos, newRoom);
                    roomPositions.Add(newPos);
                }
            }

            // Find farthest room from start using BFS to place the Exit
            Vector2Int exitPos = FindFarthestRoom(floor.rooms, start);
            floor.exitPos = exitPos;

            floor.rooms[exitPos].type = RoomType.Exit;
            floor.rooms[exitPos].title = "Passage Deeper";
            floor.rooms[exitPos].description = $"At the far end of the hall, a massive spiral stone staircase winds downward to Floor {floorNumber + 1}.";
            floor.rooms[exitPos].isResolved = false;

            // Assign remaining room types
            foreach (var kvp in floor.rooms)
            {
                if (kvp.Key == start || kvp.Key == exitPos) continue;

                float roll = UnityEngine.Random.value;
                if (roll < 0.30f)
                {
                    kvp.Value.type = RoomType.Empty;
                    kvp.Value.title = "Empty Corridor";
                    kvp.Value.description = EmptyDescriptions[UnityEngine.Random.Range(0, EmptyDescriptions.Length)];
                    kvp.Value.isResolved = true;
                }
                else if (roll < 0.60f)
                {
                    kvp.Value.type = RoomType.Combat;
                    int idx = UnityEngine.Random.Range(0, CombatTitles.Length);
                    kvp.Value.title = CombatTitles[idx];
                    kvp.Value.description = CombatDescriptions[idx];
                    kvp.Value.isResolved = false;
                }
                else if (roll < 0.80f)
                {
                    kvp.Value.type = RoomType.Event;
                    int idx = UnityEngine.Random.Range(0, EventTitles.Length);
                    kvp.Value.title = EventTitles[idx];
                    kvp.Value.description = EventDescriptions[idx];
                    kvp.Value.isResolved = false;
                }
                else
                {
                    kvp.Value.type = RoomType.Treasure;
                    int idx = UnityEngine.Random.Range(0, TreasureTitles.Length);
                    kvp.Value.title = TreasureTitles[idx];
                    kvp.Value.description = TreasureDescriptions[idx];
                    kvp.Value.isResolved = false;
                }
            }

            // Discover adjacent rooms of start
            DiscoverAdjacent(floor, start);

            return floor;
        }

        public static void DiscoverAdjacent(DungeonFloor floor, Vector2Int pos)
        {
            foreach (var d in Directions)
            {
                Vector2Int neighbor = pos + d;
                if (floor.rooms.TryGetValue(neighbor, out DungeonRoom r))
                {
                    r.isDiscovered = true;
                }
            }
        }

        private static Vector2Int FindFarthestRoom(Dictionary<Vector2Int, DungeonRoom> rooms, Vector2Int start)
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();

            queue.Enqueue(start);
            distances[start] = 0;

            Vector2Int farthest = start;
            int maxDist = 0;

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();
                int curDist = distances[curr];

                if (curDist > maxDist)
                {
                    maxDist = curDist;
                    farthest = curr;
                }

                foreach (var d in Directions)
                {
                    Vector2Int n = curr + d;
                    if (rooms.ContainsKey(n) && !distances.ContainsKey(n))
                    {
                        distances[n] = curDist + 1;
                        queue.Enqueue(n);
                    }
                }
            }

            return farthest;
        }
    }
}