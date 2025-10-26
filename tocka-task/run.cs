using System;
using System.Collections.Generic;
using System.Linq;

namespace AmphipodOrganization
{
    public class BurrowState
    {
        public string Corridor { get; }
        public string[] Rooms { get; }
        public int RoomDepth { get; }
        public string StateKey { get; }

        public BurrowState(string corridor, string[] rooms, int roomDepth)
        {
            Corridor = corridor;
            Rooms = rooms;
            RoomDepth = roomDepth;
            StateKey = corridor + string.Join("", Rooms);
        }
        
        public bool IsOrganized()
        {
            for (var i = 0; i < 4; i++)
            {
                var targetAmphipod = (char)('A' + i);
                if (Rooms[i] != new string(targetAmphipod, RoomDepth))
                    return false;
            }
            return true;
        }
    }
    
    public class MoveGenerator
    {
        private static readonly int[] RoomEntrances = { 2, 4, 6, 8 };
        private static readonly Dictionary<char, int> EnergyCosts = new()
        {
            ['A'] = 1, ['B'] = 10, ['C'] = 100, ['D'] = 1000
        };

        public IEnumerable<(BurrowState newState, int cost)> Generate(BurrowState state)
        {
            foreach (var move in GenerateMovesFromRooms(state))
                yield return move;
            foreach (var move in GenerateMovesFromCorridor(state))
                yield return move;
        }

        private IEnumerable<(BurrowState, int)> GenerateMovesFromRooms(BurrowState state)
        {
            for (var roomIndex = 0; roomIndex < 4; roomIndex++)
            {
                var depth = -1;
                var amphipod = '.';
                for (var d = 0; d < state.RoomDepth; d++)
                {
                    if (state.Rooms[roomIndex][d] == '.') 
                        continue;
                    depth = d;
                    amphipod = state.Rooms[roomIndex][d];
                    break;
                }

                if (amphipod == '.') 
                    continue;
                
                var targetAmphipod = (char)('A' + roomIndex);
                var isRoomCorrect = true;
                for (var d = depth; d < state.RoomDepth; d++)
                {
                    if (state.Rooms[roomIndex][d] == targetAmphipod) 
                        continue;
                    isRoomCorrect = false;
                    break;
                }
                if (isRoomCorrect) continue;
                
                var roomExitPos = RoomEntrances[roomIndex];
                for (var corridorPos = 0; corridorPos < 11; corridorPos++)
                {
                    if (RoomEntrances.Contains(corridorPos)) continue;

                    if (!IsCorridorPathClear(state.Corridor, roomExitPos, corridorPos)) 
                        continue;
                    var steps = (depth + 1) + Math.Abs(roomExitPos - corridorPos);
                    var cost = steps * EnergyCosts[amphipod];
                    yield return (CreateStateByMovingFromRoom(state, roomIndex, depth, corridorPos), cost);
                }
            }
        }

        private IEnumerable<(BurrowState, int)> GenerateMovesFromCorridor(BurrowState state)
        {
            for (var corridorPos = 0; corridorPos < 11; corridorPos++)
            {
                var amphipod = state.Corridor[corridorPos];
                if (amphipod == '.') continue;

                var targetRoomIndex = amphipod - 'A';
                var targetRoomPos = RoomEntrances[targetRoomIndex];


                var canEnterRoom = state.Rooms[targetRoomIndex].All(occupant => occupant == '.' || occupant == amphipod);
                if (!canEnterRoom) continue;

                if (!IsCorridorPathClear(state.Corridor, corridorPos, targetRoomPos)) 
                    continue;
                var targetDepth = state.RoomDepth - 1;
                while (targetDepth >= 0 && state.Rooms[targetRoomIndex][targetDepth] != '.') 
                    targetDepth--;
                if (targetDepth == -1) continue;

                var steps = Math.Abs(corridorPos - targetRoomPos) + (targetDepth + 1);
                var cost = steps * EnergyCosts[amphipod];
                yield return (CreateStateByMovingToRoom(state, corridorPos, targetRoomIndex, targetDepth), cost);
            }
        }

        private bool IsCorridorPathClear(string corridor, int start, int end)
        {
            var min = Math.Min(start, end);
            var max = Math.Max(start, end);
            for (var i = min; i <= max; i++)
                if (i != start && corridor[i] != '.')
                    return false;
            return true;
        }
        
        private static BurrowState CreateStateByMovingFromRoom(BurrowState state, int roomIndex, int depth, int corridorPos)
        {
            var amphipod = state.Rooms[roomIndex][depth];
            
            var newCorridorChars = state.Corridor.ToCharArray();
            newCorridorChars[corridorPos] = amphipod;

            var newRooms = (string[])state.Rooms.Clone();
            var roomChars = newRooms[roomIndex].ToCharArray();
            roomChars[depth] = '.';
            newRooms[roomIndex] = new string(roomChars);
            
            return new BurrowState(new string(newCorridorChars), newRooms, state.RoomDepth);
        }

        private static BurrowState CreateStateByMovingToRoom(BurrowState state, int corridorPos, int roomIndex, int depth)
        {
            var enemies = state.Corridor[corridorPos];

            var newCorridorChars = state.Corridor.ToCharArray();
            newCorridorChars[corridorPos] = '.';
            
            var newRooms = (string[])state.Rooms.Clone();
            var roomChars = newRooms[roomIndex].ToCharArray();
            roomChars[depth] = enemies;
            newRooms[roomIndex] = new string(roomChars);

            return new BurrowState(new string(newCorridorChars), newRooms, state.RoomDepth);
        }
    }
    
    public static class PuzzleSolver
    {
        public static int FindLowestEnergySolution(BurrowState initialState)
        {
            var pQueue = new PriorityQueue<BurrowState, int>();
            var knownCosts = new Dictionary<string, int>();
            var generator = new MoveGenerator();

            pQueue.Enqueue(initialState, 0);
            knownCosts[initialState.StateKey] = 0;

            while (pQueue.Count > 0)
            {
                var currentState = pQueue.Dequeue();
                var currentEnergy = knownCosts[currentState.StateKey];
                
                if (currentState.IsOrganized())
                    return currentEnergy;

                foreach (var (nextState, moveCost) in generator.Generate(currentState))
                {
                    var newTotalEnergy = currentEnergy + moveCost;

                    if (knownCosts.TryGetValue(nextState.StateKey, out int existingEnergy) &&
                        newTotalEnergy >= existingEnergy) continue;
                    knownCosts[nextState.StateKey] = newTotalEnergy;
                    pQueue.Enqueue(nextState, newTotalEnergy);
                }
            }
            
            return -1; 
        }
    }
    
    class Program
    {
        static void Main()
        {
            var lines = new List<string>();
            string line;

            while ((line = Console.ReadLine()) != null) 
                lines.Add(line);
            
            var initialState = ParseInput(lines);
            var result = PuzzleSolver.FindLowestEnergySolution(initialState);
            Console.WriteLine(result);
        }
        
        private static BurrowState ParseInput(List<string> lines)
        {
            var hall = new string('.', 11);
            var depth = lines.Count - 3;
            var rooms = new string[4];

            for (var i = 0; i < 4; i++)
            {
                var roomChars = new char[depth];
                for (var j = 0; j < depth; j++) 
                    roomChars[j] = lines[2 + j][3 + 2 * i];
                rooms[i] = new string(roomChars);
            }

            return new BurrowState(hall, rooms, depth);
        }
    }
}