using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public record State(string Hall, IReadOnlyList<string> Rooms)
{
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("#############");
        sb.AppendLine("#" + Hall.Replace(' ', '.') + "#");
        
        var maxDepth = Rooms.Max(r => r.Length);
        for (var i = maxDepth - 1; i >= 0; i--)
        {
            sb.Append(i == maxDepth - 1 ? "###" : "  #");
            for (var j = 0; j < 4; j++)
            {
                sb.Append((Rooms[j].Length > i ? Rooms[j][i] : '.') + "#");
            }
            sb.AppendLine(i == maxDepth - 1 ? "##" : "  ");
        }
        sb.AppendLine("  #########  ");
        return sb.ToString();
    }
}

public class Program
{
    private const string EnemyTypes = "ABCD";

    private static readonly Dictionary<char, int> EnergyCost = new()
    {
        { 'A', 1 },
        { 'B', 10 },
        { 'C', 100 },
        { 'D', 1000 }
    };

    private static readonly Dictionary<char, int> TargetIdx = new()
    {
        { 'A', 0 },
        { 'B', 1 },
        { 'C', 2 },
        { 'D', 3 }
    };

    private static readonly int[] RoomPos = { 2, 4, 6, 8 };
    private static readonly int[] HallStops = { 0, 1, 3, 5, 7, 9, 10 };

    private static (char[] hall, List<List<char>> rooms, int roomDepth) ParseInput(List<string> lines)
    {
        var hall = new char[11];
        Array.Fill(hall, ' ');

        var rooms = new List<List<char>> { new(), new(), new(), new() };
        var depth = lines.Count - 3;
        
        for (var i = lines.Count - 2; i > 1; i--)
        {
            var line = lines[i];
            for (var roomIdx = 0; roomIdx < 4; roomIdx++)
            {
                var pos = 3 + roomIdx * 2;
                if (pos < line.Length && EnemyTypes.Contains(line[pos])) 
                    rooms[roomIdx].Add(line[pos]);
            }
        }
        
        return (hall, rooms, depth);
    }

    private static State StateToRecord(char[] hall, List<List<char>> rooms) 
        => new(new string(hall), rooms.Select(r => new string(r.ToArray())).ToList());

    private static bool IsGoalState(List<List<char>> rooms, int roomDepth)
    {
        for (var i = 0; i < 4; i++)
            if (rooms[i].Count != roomDepth || !rooms[i].All(obj => obj == EnemyTypes[i]))
                return false;
        return true;
    }

    private static bool CanEnterRoom(List<char> room, char objectType) 
        => room.All(obj => obj == objectType);

    private static bool IsHallPathClear(char[] hall, int startPos, int endPos)
    {
        if (startPos < endPos)
        {
            for (var i = startPos + 1; i <= endPos; i++)
                if (hall[i] != ' ') return false;
        }
        else
            for (var i = startPos - 1; i >= endPos; i--)
                if (hall[i] != ' ') return false;
        return true;
    }
    
    static List<(int cost, char[] newHall, List<List<char>> newRooms)> FindPossibleMoves(char[] hall, List<List<char>> rooms, int roomDepth)
    {
        var moves = new List<(int, char[], List<List<char>>)>();
        
        for (var hallPos = 0; hallPos < hall.Length; hallPos++)
        {
            if (hall[hallPos] == ' ') 
                continue;

            var obj = hall[hallPos];
            var targetRoomIdx = TargetIdx[obj];
            
            if (!CanEnterRoom(rooms[targetRoomIdx], obj)) 
                continue;

            var targetPos = RoomPos[targetRoomIdx];
            
            if (!IsHallPathClear(hall, hallPos, targetPos)) 
                continue;
            
            var hallSteps = Math.Abs(hallPos - targetPos);
            var roomSteps = roomDepth - rooms[targetRoomIdx].Count;
            var cost = (hallSteps + roomSteps) * EnergyCost[obj];

            var newHall = (char[])hall.Clone();
            newHall[hallPos] = ' ';
            var newRooms = rooms.Select(r => new List<char>(r)).ToList();
            newRooms[targetRoomIdx].Add(obj);

            moves.Add((cost, newHall, newRooms));
        }
        
        for (var roomIdx = 0; roomIdx < 4; roomIdx++)
        {
            if (rooms[roomIdx].Count == 0) continue;

            if (rooms[roomIdx].All(obj => obj == EnemyTypes[roomIdx])) 
                continue;

            var obj = rooms[roomIdx].Last();
            var roomPos = RoomPos[roomIdx];

            foreach (var hallPos in HallStops)
            {
                if (!IsHallPathClear(hall, roomPos, hallPos)) continue;

                var roomSteps = roomDepth - rooms[roomIdx].Count + 1;
                var hallSteps = Math.Abs(hallPos - roomPos);
                var cost = (roomSteps + hallSteps) * EnergyCost[obj];

                var newHall = (char[])hall.Clone();
                newHall[hallPos] = obj;
                var newRooms = rooms.Select(r => new List<char>(r)).ToList();
                newRooms[roomIdx].RemoveAt(newRooms[roomIdx].Count - 1);
                
                moves.Add((cost, newHall, newRooms));
            }
        }
        return moves;
    }

    private static int Heuristic(char[] hall, List<List<char>> rooms)
    {
        var total = 0;
        
        for (var pos = 0; pos < hall.Length; pos++)
        {
            var obj = hall[pos];
            if (obj == ' ') 
                continue;
            var targetPos = RoomPos[TargetIdx[obj]];
            var dist = Math.Abs(pos - targetPos);
            total += dist * EnergyCost[obj];
        }

        for (var roomIdx = 0; roomIdx < rooms.Count; roomIdx++)
        {
            var room = rooms[roomIdx];
            for (var i = 0; i < room.Count; i++)
            {
                var obj = room[i];
                if (obj == EnemyTypes[roomIdx]) 
                    continue;
                var targetPos = RoomPos[TargetIdx[obj]];
                var dist = Math.Abs(RoomPos[roomIdx] - targetPos);
                var stepsOut = room.Count - i; 
                total += (stepsOut + dist) * EnergyCost[obj];
            }
        }
        
        return total;
    }

    static int Solve(List<string> lines)
    {
        var (hall, rooms, roomDepth) = ParseInput(lines);
        long counter = 0;
        var pq = new PriorityQueue<(char[] hall, List<List<char>> rooms, int cost), (int priority, long counter)>();
        
        var initialState = StateToRecord(hall, rooms);
        pq.Enqueue((hall, rooms, 0), (Heuristic(hall, rooms), counter++));

        var visited = new Dictionary<State, int> { { initialState, 0 } };
        
        while (pq.Count > 0)
        {
            var (curHall, curRooms, curCost) = pq.Dequeue();

            if (IsGoalState(curRooms, roomDepth))
                return curCost;
            
            var currentState = StateToRecord(curHall, curRooms);
            if (visited.GetValueOrDefault(currentState, int.MaxValue) < curCost)
                continue;

            foreach (var (moveCost, newHall, newRooms) in FindPossibleMoves(curHall, curRooms, roomDepth))
            {
                var newCost = curCost + moveCost;
                var newState = StateToRecord(newHall, newRooms);

                if (newCost >= visited.GetValueOrDefault(newState, int.MaxValue)) 
                    continue;
                visited[newState] = newCost;
                var priority = newCost + Heuristic(newHall, newRooms);
                pq.Enqueue((newHall, newRooms, newCost), (priority, counter++));
            }
        }
        
        return 0;
    }

    static void Main()
    {
        var lines = new List<string>();
        string line;

        while ((line = Console.ReadLine()) != null) 
            lines.Add(line);

        var result = Solve(lines);
        Console.WriteLine(result);
    }
}