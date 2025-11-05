using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private const string VirusStartPosition = "a";
    private const int InfiniteDistance = 1_000_000_000;
    
    private static readonly Dictionary<(string, string), ((string, string)?, List<(string, string)>)?> Memo = new();
    
    static List<string> Solve(List<(string, string)> initialEdges)
    {
        var edgeSet = new HashSet<(string, string)>();
        foreach (var (a, b) in initialEdges) 
            edgeSet.Add(CanonicalEdge(a, b));

        var sequence = new List<(string, string)>();
        var curEdges = edgeSet;
        var curVirus = VirusStartPosition;

        while (true)
        {
            var res = Search(curEdges, curVirus);
            if (res == null) break;

            var (firstPair, _) = res.Value;
            if (!firstPair.HasValue) break;

            sequence.Add(firstPair.Value);
            var ce = CanonicalEdge(firstPair.Value.Item1, firstPair.Value.Item2);
            
            var newEdges = new HashSet<(string, string)>(curEdges);
            newEdges.Remove(ce);
            curEdges = newEdges;

            var graph = BuildGraph(curEdges);
            var move = VirusMove(graph, curVirus);

            if (move == null) break;
            
            var (_, nextNode, _) = move.Value;
            if (char.IsUpper(nextNode[0])) break;
            
            curVirus = nextNode;
        }

        return sequence.Select(p => $"{p.Item1}-{p.Item2}").ToList();
    }

    static void Main()
    {
        var edges = new List<(string, string)>();
        string line;

        while ((line = Console.ReadLine()) != null)
        {
            line = line.Trim();
            if (!string.IsNullOrEmpty(line))
            {
                var parts = line.Split('-');
                if (parts.Length == 2)
                {
                    edges.Add((parts[0], parts[1]));
                }
            }
        }

        var result = Solve(edges);
        foreach (var edge in result)
            Console.WriteLine(edge);
    }
    
    private static Dictionary<string, HashSet<string>> BuildGraph(ISet<(string, string)> edges)
    {
        var g = new Dictionary<string, HashSet<string>>();
        foreach (var (u, v) in edges)
        {
            if (!g.ContainsKey(u)) 
                g[u] = new HashSet<string>();
            if (!g.ContainsKey(v)) 
                g[v] = new HashSet<string>();
            g[u].Add(v);
            g[v].Add(u);
        }
        return g;
    }

    private static (string, string, Dictionary<string, int>)? VirusMove(Dictionary<string, HashSet<string>> graph, string virus)
    {
        var nodes = new HashSet<string>(graph.Keys);
        var gates = GetGates(nodes);

        string bestGate = null!;
        var bestDist = InfiniteDistance;
        Dictionary<string, int> bestDistMap = null!;

        foreach (var gate in gates)
        {
            var dist = new Dictionary<string, int> { { gate, 0 } };
            var q = new Queue<string>();
            q.Enqueue(gate);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (!graph.TryGetValue(cur, out var neighbors)) 
                    continue;
                foreach (var nb in neighbors.Where(nb => !dist.ContainsKey(nb)))
                {
                    dist[nb] = dist[cur] + 1;
                    q.Enqueue(nb);
                }
            }

            if (!dist.TryGetValue(virus, out var d)) 
                continue;
            if (d >= bestDist && (d != bestDist || string.CompareOrdinal(gate, bestGate) >= 0)) 
                continue;
            bestGate = gate;
            bestDist = d;
            bestDistMap = dist;
        }

        if (bestGate == null) 
            return null;

        var dvirus = bestDistMap![virus];
        if (dvirus == 0) 
            return (bestGate, bestGate, bestDistMap);

        var candidates = new List<string>();
        if (graph.TryGetValue(virus, out var virusNeighbors))
            foreach (var nb in virusNeighbors.OrderBy(n => n))
                if (bestDistMap.TryGetValue(nb, out var nbDist) && nbDist == dvirus - 1) 
                    candidates.Add(nb);
        
        if (candidates.Count == 0) 
            return null;

        return (bestGate, candidates[0], bestDistMap);
    }

    private static ((string, string)?, List<(string, string)>)? Search(ISet<(string, string)> edges, string virusPos)
    {
        var key = (CreateEdges(edges), virusPos);
        if (Memo.TryGetValue(key, out var search)) 
            return search;

        var graph = BuildGraph(edges);
        var move = VirusMove(graph, virusPos);

        if (move == null)
            return ((null, null)!, new List<(string, string)>());

        var candidates = new SortedSet<(string, string)>(Comparer<(string, string)>.Create((a, b) =>
        {
            var comp1 = string.CompareOrdinal(a.Item1, b.Item1);
            return comp1 != 0 ? comp1 : string.CompareOrdinal(a.Item2, b.Item2);
        }));

        foreach (var (u, v) in edges)
        {
            if (char.IsUpper(u[0]) && !char.IsUpper(v[0])) 
                candidates.Add((u, v));
            else if (char.IsUpper(v[0]) && !char.IsUpper(u[0])) 
                candidates.Add((v, u));
        }

        if (candidates.Count == 0)
        {
            Memo[key] = null;
            return null;
        }

        foreach (var (gate, node) in candidates)
        {
            var ce = CanonicalEdge(gate, node);
            if (!edges.Contains(ce)) 
                continue;
            
            var newEdges = new HashSet<(string, string)>(edges);
            newEdges.Remove(ce);

            var newGraph = BuildGraph(newEdges);
            var newMove = VirusMove(newGraph, virusPos);

            if (newMove == null)
            {
                var result = ((gate, node), new List<(string, string)>());
                Memo[key] = result;
                return result;
            }

            var (_, nextPos2, _) = newMove.Value;
            if (char.IsUpper(nextPos2[0])) 
                continue;

            var deeper = Search(newEdges, nextPos2);
            if (deeper == null) 
                continue;
            var (firstPair, restList) = deeper.Value;
            var seq = new List<(string, string)> { (gate, node) };
            if (firstPair.HasValue) 
                seq.Add(firstPair.Value);
            seq.AddRange(restList);

            var finalResult = ((seq[0], seq.Count > 1 ? seq.Skip(1).ToList() : new List<(string, string)>()));
            Memo[key] = finalResult;
            return finalResult;
        }
        
        Memo[key] = null;
        return null;
    }
    
    private static string CreateEdges(ISet<(string, string)> edges) 
        => string.Join(";", edges.OrderBy(e => e.Item1).ThenBy(e => e.Item2).Select(e => $"{e.Item1}-{e.Item2}"));
    
    private static (string, string) CanonicalEdge(string a, string b) 
        => string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
    
    private static List<string> GetGates(IEnumerable<string> nodes) 
        => nodes.Where(n => !string.IsNullOrEmpty(n) && char.IsUpper(n[0])).OrderBy(n => n).ToList();
}