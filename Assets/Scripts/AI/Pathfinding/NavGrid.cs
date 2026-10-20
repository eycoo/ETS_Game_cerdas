using System.Collections.Generic;
using UnityEngine;

// Grid navigasi per ruangan + pencarian jalur A* (materi Praktikum 04).
// Sel yang bertabrakan dengan layer Obstacle ditandai tidak bisa dilewati, lalu A*
// mencari jalur 8 arah dengan heuristik octile. Jalurnya dihaluskan (string pulling)
// supaya musuh tidak berjalan zig-zag.
public class NavGrid
{
    public readonly float CellSize;
    public readonly int Width;
    public readonly int Height;
    public readonly Vector3 Origin;

    public int LastExpanded { get; private set; }
    public bool IsBuilt { get; private set; }

    private readonly bool[] walkable;
    private readonly float[] gCost;
    private readonly int[] parent;
    private readonly int[] openStamp;
    private readonly int[] closedStamp;
    private readonly MinHeap heap = new MinHeap();
    private readonly List<Vector3> smoothBuffer = new List<Vector3>();
    private int stamp;

    private static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] DY = { 0, 0, 1, -1, 1, -1, 1, -1 };
    private const float Diagonal = 1.41421356f;

    public NavGrid(Vector3 center, Vector2 size, float cellSize)
    {
        CellSize = cellSize;
        Width = Mathf.Max(1, Mathf.RoundToInt(size.x / cellSize));
        Height = Mathf.Max(1, Mathf.RoundToInt(size.y / cellSize));
        Origin = new Vector3(center.x - Width * cellSize * 0.5f, 0f, center.z - Height * cellSize * 0.5f);

        int n = Width * Height;
        walkable = new bool[n];
        gCost = new float[n];
        parent = new int[n];
        openStamp = new int[n];
        closedStamp = new int[n];
    }

    public void Build(float agentRadius)
    {
        Vector3 half = new Vector3(CellSize * 0.5f + agentRadius * 0.5f, 0.45f, CellSize * 0.5f + agentRadius * 0.5f);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Vector3 c = CellCenter(x, y) + Vector3.up * 0.6f;
                walkable[y * Width + x] = !Physics.CheckBox(c, half, Quaternion.identity, Layers.ObstacleMask, QueryTriggerInteraction.Ignore);
            }
        }
        IsBuilt = true;
    }

    public Vector3 CellCenter(int x, int y)
    {
        return new Vector3(Origin.x + (x + 0.5f) * CellSize, 0f, Origin.z + (y + 0.5f) * CellSize);
    }

    private void WorldToCell(Vector3 p, out int x, out int y)
    {
        x = Mathf.Clamp(Mathf.FloorToInt((p.x - Origin.x) / CellSize), 0, Width - 1);
        y = Mathf.Clamp(Mathf.FloorToInt((p.z - Origin.z) / CellSize), 0, Height - 1);
    }

    public bool IsWalkable(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height && walkable[y * Width + x];
    }

    public bool IsWalkable(Vector3 p)
    {
        int x = Mathf.FloorToInt((p.x - Origin.x) / CellSize);
        int y = Mathf.FloorToInt((p.z - Origin.z) / CellSize);
        return IsWalkable(x, y);
    }

    private bool NearestWalkable(ref int x, ref int y, int maxRadius)
    {
        if (IsWalkable(x, y))
            return true;

        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                        continue;
                    if (IsWalkable(x + dx, y + dy))
                    {
                        x += dx;
                        y += dy;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static float Heuristic(int ax, int ay, int bx, int by)
    {
        int dx = Mathf.Abs(ax - bx);
        int dy = Mathf.Abs(ay - by);
        return dx + dy + (Diagonal - 2f) * Mathf.Min(dx, dy);
    }

    public bool FindPath(Vector3 start, Vector3 goal, List<Vector3> result)
    {
        result.Clear();
        if (!IsBuilt)
            return false;

        int sx, sy, gx, gy;
        WorldToCell(start, out sx, out sy);
        WorldToCell(goal, out gx, out gy);
        if (!NearestWalkable(ref sx, ref sy, 6) || !NearestWalkable(ref gx, ref gy, 8))
            return false;

        stamp++;
        heap.Clear();
        int s = sy * Width + sx;
        int target = gy * Width + gx;
        gCost[s] = 0f;
        parent[s] = -1;
        openStamp[s] = stamp;
        heap.Push(s, Heuristic(sx, sy, gx, gy));
        LastExpanded = 0;

        while (heap.Count > 0)
        {
            int cur = heap.Pop();
            if (closedStamp[cur] == stamp)
                continue;
            closedStamp[cur] = stamp;
            LastExpanded++;

            if (cur == target)
            {
                Reconstruct(cur, goal, result);
                Smooth(result);
                return true;
            }

            int cx = cur % Width;
            int cy = cur / Width;
            for (int k = 0; k < 8; k++)
            {
                int nx = cx + DX[k];
                int ny = cy + DY[k];
                if (!IsWalkable(nx, ny))
                    continue;

                // Diagonal tidak boleh memotong sudut obstacle.
                if (k >= 4 && (!IsWalkable(cx + DX[k], cy) || !IsWalkable(cx, cy + DY[k])))
                    continue;

                int n = ny * Width + nx;
                if (closedStamp[n] == stamp)
                    continue;

                float cost = gCost[cur] + (k < 4 ? 1f : Diagonal);
                if (openStamp[n] != stamp || cost < gCost[n])
                {
                    gCost[n] = cost;
                    parent[n] = cur;
                    openStamp[n] = stamp;
                    heap.Push(n, cost + Heuristic(nx, ny, gx, gy));
                }
            }
        }
        return false;
    }

    private void Reconstruct(int cur, Vector3 goal, List<Vector3> result)
    {
        while (cur >= 0)
        {
            result.Add(CellCenter(cur % Width, cur / Width));
            cur = parent[cur];
        }
        result.Reverse();
        if (IsWalkable(goal))
            result[result.Count - 1] = new Vector3(goal.x, 0f, goal.z);
    }

    // String pulling: dari setiap titik, lompat ke titik terjauh yang masih terlihat lurus di grid.
    private void Smooth(List<Vector3> path)
    {
        if (path.Count < 3)
            return;

        smoothBuffer.Clear();
        smoothBuffer.Add(path[0]);
        int i = 0;
        while (i < path.Count - 1)
        {
            int far = i + 1;
            for (int j = path.Count - 1; j > i + 1; j--)
            {
                if (StraightLineClear(path[i], path[j]))
                {
                    far = j;
                    break;
                }
            }
            smoothBuffer.Add(path[far]);
            i = far;
        }
        path.Clear();
        path.AddRange(smoothBuffer);
    }

    public bool StraightLineClear(Vector3 a, Vector3 b)
    {
        float dist = CapyMath.FlatDistance(a, b);
        int steps = Mathf.CeilToInt(dist / (CellSize * 0.5f));
        for (int s = 1; s < steps; s++)
        {
            if (!IsWalkable(Vector3.Lerp(a, b, s / (float)steps)))
                return false;
        }
        return true;
    }

    // Memilih sel bisa-dilewati dengan skor tertinggi dari sejumlah sampel acak.
    public bool BestPoint(System.Func<Vector3, float> score, int samples, out Vector3 best)
    {
        best = Vector3.zero;
        float bestScore = float.MinValue;
        bool found = false;
        for (int i = 0; i < samples; i++)
        {
            int x = Random.Range(0, Width);
            int y = Random.Range(0, Height);
            if (!IsWalkable(x, y))
                continue;
            Vector3 p = CellCenter(x, y);
            float sc = score(p);
            if (sc > bestScore)
            {
                bestScore = sc;
                best = p;
                found = true;
            }
        }
        return found;
    }

    public void DrawBlocked(Color color)
    {
        float h = CellSize * 0.32f;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (walkable[y * Width + x])
                    continue;
                Vector3 c = CellCenter(x, y) + Vector3.up * 0.06f;
                AIDebugDraw.Line(c + new Vector3(-h, 0f, -h), c + new Vector3(h, 0f, h), color, 0.03f);
                AIDebugDraw.Line(c + new Vector3(-h, 0f, h), c + new Vector3(h, 0f, -h), color, 0.03f);
            }
        }
    }

    private class MinHeap
    {
        private readonly List<int> items = new List<int>(256);
        private readonly List<float> priorities = new List<float>(256);

        public int Count => items.Count;

        public void Clear()
        {
            items.Clear();
            priorities.Clear();
        }

        public void Push(int item, float priority)
        {
            items.Add(item);
            priorities.Add(priority);
            int i = items.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (priorities[p] <= priorities[i])
                    break;
                Swap(i, p);
                i = p;
            }
        }

        public int Pop()
        {
            int top = items[0];
            int last = items.Count - 1;
            items[0] = items[last];
            priorities[0] = priorities[last];
            items.RemoveAt(last);
            priorities.RemoveAt(last);

            int i = 0;
            int count = items.Count;
            while (true)
            {
                int l = i * 2 + 1;
                int r = l + 1;
                int smallest = i;
                if (l < count && priorities[l] < priorities[smallest]) smallest = l;
                if (r < count && priorities[r] < priorities[smallest]) smallest = r;
                if (smallest == i)
                    break;
                Swap(i, smallest);
                i = smallest;
            }
            return top;
        }

        private void Swap(int a, int b)
        {
            int ti = items[a];
            items[a] = items[b];
            items[b] = ti;
            float tp = priorities[a];
            priorities[a] = priorities[b];
            priorities[b] = tp;
        }
    }
}
