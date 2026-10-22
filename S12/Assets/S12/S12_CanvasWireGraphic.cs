using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A transparent UI canvas layer. Each projected mesh edge becomes one thin quad.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class S12_CanvasWireGraphic : Graphic
{
    public struct Segment
    {
        public Vector2 start;
        public Vector2 end;
        public Color color;

        public Segment(Vector2 start, Vector2 end, Color color)
        {
            this.start = start;
            this.end = end;
            this.color = color;
        }
    }

    [SerializeField, Min(0.5f)] private float lineWidth = 2f;
    private readonly List<Segment> segments = new List<Segment>();
    public int SegmentCount => segments.Count;

    public void SetSegments(List<Segment> source)
    {
        segments.Clear();
        if (source != null) segments.AddRange(source);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        foreach (Segment segment in segments)
        {
            Vector2 direction = segment.end - segment.start;
            if (direction.sqrMagnitude < 0.0001f) continue;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (lineWidth * 0.5f);
            int index = helper.currentVertCount;
            AddVertex(helper, segment.start - normal, segment.color);
            AddVertex(helper, segment.start + normal, segment.color);
            AddVertex(helper, segment.end + normal, segment.color);
            AddVertex(helper, segment.end - normal, segment.color);
            helper.AddTriangle(index, index + 1, index + 2);
            helper.AddTriangle(index, index + 2, index + 3);
        }
    }

    private static void AddVertex(VertexHelper helper, Vector2 position, Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        helper.AddVert(vertex);
    }
}
