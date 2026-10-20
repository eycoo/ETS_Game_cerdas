using System.Collections.Generic;
using UnityEngine;

// "AI Vision" (tombol V): menggambar isi kepala AI langsung di arena, misalnya jalur A*,
// grid obstacle, tetangga flocking, radius deteksi, dan arah serangan.
// Garis dibangun sebagai mesh quad datar setiap frame, jadi aman dipakai di URP.
public class AIDebugDraw : MonoBehaviour
{
    private static AIDebugDraw I;

    private struct Segment
    {
        public Vector3 a;
        public Vector3 b;
        public Color color;
        public float width;
    }

    private readonly List<Segment> segments = new List<Segment>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> indices = new List<int>();
    private Mesh mesh;
    private Material material;

    public static bool Enabled => GameManager.I != null && GameManager.I.AIVision;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        I = null;
    }

    public static void Ensure()
    {
        if (I != null)
            return;
        GameObject go = new GameObject("AIDebugDraw");
        I = go.AddComponent<AIDebugDraw>();
    }

    private void Awake()
    {
        mesh = new Mesh();
        mesh.name = "AIDebugLines";
        mesh.MarkDynamic();
        material = GameDatabase.I.lineMaterial;
    }

    public static void Line(Vector3 a, Vector3 b, Color color, float width = 0.06f)
    {
        if (I == null)
            return;
        Segment s;
        s.a = a;
        s.b = b;
        s.color = color;
        s.width = width;
        I.segments.Add(s);
    }

    public static void Circle(Vector3 center, float radius, Color color, float width = 0.05f, int steps = 40)
    {
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= steps; i++)
        {
            float a = i / (float)steps * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Line(prev, p, color, width);
            prev = p;
        }
    }

    public static void Path(List<Vector3> path, Vector3 from, Color color)
    {
        if (path == null || path.Count == 0)
            return;
        Vector3 prev = from + Vector3.up * 0.12f;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = path[i] + Vector3.up * 0.12f;
            Line(prev, p, color, 0.09f);
            Circle(p, 0.14f, color, 0.04f, 10);
            prev = p;
        }
    }

    private void LateUpdate()
    {
        segments.Clear();
        if (!Enabled)
        {
            mesh.Clear();
            return;
        }

        GameManager gm = GameManager.I;
        if (gm.CurrentRoom != null && gm.CurrentRoom.Grid != null && gm.CurrentRoom.State == RoomController.RoomState.Active)
            gm.CurrentRoom.Grid.DrawBlocked(new Color(1f, 0.3f, 0.3f, 0.35f));

        for (int i = 0; i < EnemyBase.Active.Count; i++)
        {
            EnemyBase e = EnemyBase.Active[i];
            if (e != null && e.IsAlive)
                e.DrawAIDebug();
        }

        AutoPilot pilot = gm.Player != null ? gm.Player.GetComponent<AutoPilot>() : null;
        if (pilot != null && pilot.enabled)
            pilot.DrawAIDebug();

        BuildMesh();
        if (vertices.Count > 0)
            Graphics.RenderMesh(new RenderParams(material), mesh, 0, Matrix4x4.identity);
    }

    private void BuildMesh()
    {
        vertices.Clear();
        colors.Clear();
        indices.Clear();
        for (int i = 0; i < segments.Count; i++)
        {
            Segment s = segments[i];
            Vector3 d = (s.b - s.a).Flat();
            if (d.sqrMagnitude < 0.000001f)
                continue;
            Vector3 side = Vector3.Cross(Vector3.up, d.normalized) * (s.width * 0.5f);
            int baseIndex = vertices.Count;
            vertices.Add(s.a - side);
            vertices.Add(s.a + side);
            vertices.Add(s.b + side);
            vertices.Add(s.b - side);
            colors.Add(s.color);
            colors.Add(s.color);
            colors.Add(s.color);
            colors.Add(s.color);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }

        mesh.Clear();
        if (vertices.Count == 0)
            return;
        mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
    }
}
