using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class S07_Clipping : MonoBehaviour
{
    private const int CanvasSize = 256;
    private const float Left = 40f, Right = 216f, Bottom = 40f, Top = 216f;
    private static readonly Color Background = new Color(0.025f, 0.035f, 0.075f, 1f);
    private static readonly Color Border = new Color(0.60f, 0.63f, 0.69f, 1f);
    private static readonly Color Outline = new Color(1f, 0.80f, 0.38f, 1f);
    private static readonly Color Fill = new Color(0.98f, 0.34f, 0.48f, 1f);
    private static readonly Vector2[] Original = {
        new Vector2(-16f, 104f), new Vector2(84f, 262f),
        new Vector2(208f, 190f), new Vector2(180f, 65f), new Vector2(30f, 70f)
    };
    private Texture2D canvas;

    private void OnEnable() { Render(); }
    private void OnDisable()
    {
        GetComponent<RawImage>().texture = null;
        if (canvas == null) return;
        if (Application.isPlaying) Destroy(canvas);
        else DestroyImmediate(canvas);
        canvas = null;
    }

    public static List<Vector2> ClipPolygon()
    {
        var polygon = new List<Vector2>(Original);
        polygon = ClipLeft(polygon, Left);
        polygon = ClipRight(polygon, Right);
        polygon = ClipBottom(polygon, Bottom);
        polygon = ClipTop(polygon, Top);
        return polygon;
    }

    private static List<Vector2> ClipLeft(List<Vector2> input, float boundary)
    {
        return ClipBoundary(input, boundary, 0, true);
    }

    // TODO completion: right boundary keeps x <= boundary.
    private static List<Vector2> ClipRight(List<Vector2> input, float boundary)
    {
        return ClipBoundary(input, boundary, 0, false);
    }

    // TODO completion: bottom boundary keeps y >= boundary.
    private static List<Vector2> ClipBottom(List<Vector2> input, float boundary)
    {
        return ClipBoundary(input, boundary, 1, true);
    }

    // TODO completion: top boundary keeps y <= boundary.
    private static List<Vector2> ClipTop(List<Vector2> input, float boundary)
    {
        return ClipBoundary(input, boundary, 1, false);
    }

    // The same four transition rules work for all four window edges.
    private static List<Vector2> ClipBoundary(List<Vector2> input, float boundary, int axis, bool keepGreater)
    {
        var output = new List<Vector2>();
        if (input.Count == 0) return output;
        Vector2 previous = input[input.Count - 1];
        bool previousInside = IsInside(previous, boundary, axis, keepGreater);
        foreach (Vector2 current in input)
        {
            bool currentInside = IsInside(current, boundary, axis, keepGreater);
            if (previousInside != currentInside)
                output.Add(axis == 0 ? GetIntersectionX(previous, current, boundary) : GetIntersectionY(previous, current, boundary));
            if (currentInside) output.Add(current);
            previous = current;
            previousInside = currentInside;
        }
        return output;
    }

    private static bool IsInside(Vector2 point, float boundary, int axis, bool keepGreater)
    {
        float coordinate = axis == 0 ? point.x : point.y;
        return keepGreater ? coordinate >= boundary : coordinate <= boundary;
    }

    private static Vector2 GetIntersectionX(Vector2 a, Vector2 b, float boundary)
    {
        float t = (boundary - a.x) / (b.x - a.x);
        return new Vector2(boundary, Mathf.LerpUnclamped(a.y, b.y, t));
    }

    // TODO completion: intersect a segment with the horizontal y boundary.
    private static Vector2 GetIntersectionY(Vector2 a, Vector2 b, float boundary)
    {
        float t = (boundary - a.y) / (b.y - a.y);
        return new Vector2(Mathf.LerpUnclamped(a.x, b.x, t), boundary);
    }

    private void Render()
    {
        if (canvas != null) DestroyImmediate(canvas);
        canvas = new Texture2D(CanvasSize, CanvasSize, TextureFormat.RGBA32, false);
        canvas.name = "S07_Sutherland_Hodgman_Result";
        canvas.filterMode = FilterMode.Point;
        canvas.wrapMode = TextureWrapMode.Clamp;
        canvas.hideFlags = HideFlags.HideAndDontSave;
        for (int y = 0; y < CanvasSize; y++)
            for (int x = 0; x < CanvasSize; x++) canvas.SetPixel(x, y, Background);

        List<Vector2> clipped = ClipPolygon();
        // Triangle-fan fill for the convex clipped polygon.
        for (int i = 1; i + 1 < clipped.Count; i++) FillTriangle(clipped[0], clipped[i], clipped[i + 1]);

        // Gold outline shows the original polygon beyond the gray clipping window.
        for (int i = 0; i < Original.Length; i++) DrawLine(Original[i], Original[(i + 1) % Original.Length], Outline);
        DrawLine(new Vector2(Left, Bottom), new Vector2(Left, Top), Border);
        DrawLine(new Vector2(Left, Top), new Vector2(Right, Top), Border);
        DrawLine(new Vector2(Right, Top), new Vector2(Right, Bottom), Border);
        DrawLine(new Vector2(Right, Bottom), new Vector2(Left, Bottom), Border);

        canvas.Apply(false, false);
        GetComponent<RawImage>().texture = canvas;
    }

    private void FillTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        float area = Cross(b - a, c - a);
        if (Mathf.Approximately(area, 0f)) return;
        for (int y = 0; y < CanvasSize; y++)
            for (int x = 0; x < CanvasSize; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float u = Cross(b - p, c - p) / area;
                float v = Cross(c - p, a - p) / area;
                float w = 1f - u - v;
                if (u >= 0f && v >= 0f && w >= 0f) canvas.SetPixel(x, y, Fill);
            }
    }

    private void DrawLine(Vector2 a, Vector2 b, Color color)
    {
        float length = Vector2.Distance(a, b);
        int steps = Mathf.CeilToInt(length * 2f);
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y);
            if (x >= 0 && x < CanvasSize && y >= 0 && y < CanvasSize) canvas.SetPixel(x, y, color);
        }
    }

    private static float Cross(Vector2 u, Vector2 v) { return u.x * v.y - u.y * v.x; }
}
