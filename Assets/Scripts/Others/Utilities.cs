using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Utilities
{
    private static readonly List<RaycastResult> raycastResults = new();
    private static PointerEventData pointerData;
    private static EventSystem pointerEventSystem;

    public static long Round(double value)
    {
        if (double.IsNaN(value)) return 0;
        if (value >= long.MaxValue) return long.MaxValue;
        if (value <= long.MinValue) return long.MinValue;
        return (long)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    public static int Round(float value)
    {
        if (float.IsNaN(value)) return 0;
        if (value >= int.MaxValue) return int.MaxValue;
        if (value <= int.MinValue) return int.MinValue;
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    public static string NumberToString(long value) => value >= 10000 ? value.ToString("n0") : value.ToString();

    public static IEnumerator CountAnimationRoutine(TextMeshProUGUI label, long from, long to,
        float stepDelay, float duration, AudioClip sound = null)
    {
        float elapsed = 0f;
        float nextSound = 0f;
        while (label && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            double fraction = Mathf.Clamp01(elapsed / duration);
            label.text = NumberToString(Round(from + ((double)to - from) * fraction));
            if (sound && elapsed >= nextSound)
            {
                AudioController.PlaySound(sound, pitch: 1.2f);
                nextSound = elapsed + Mathf.Max(0.05f, stepDelay);
            }
            yield return null;
        }
        if (label) label.text = NumberToString(to);
    }

    public static Vector2 ScreenToWorld(Vector2 position)
    {
        var camera = GameController.Instance.GameCamera;
        return camera.ScreenToWorldPoint(new Vector3(position.x, position.y, -camera.transform.position.z));
    }

    public static bool TryGetAimPosition(out Vector2 position)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled || IsPointerOverUI(touch.position)) continue;
            position = touch.position;
            return true;
        }
        position = Input.mousePosition;
        return Input.touchCount == 0 && Input.GetMouseButton(0) && !IsPointerOverUI(position);
    }

    public static bool IsPointerOverUI(Vector2 position)
    {
        var system = EventSystem.current;
        if (!system) return false;
        if (pointerEventSystem != system || pointerData == null)
        {
            pointerEventSystem = system;
            pointerData = new PointerEventData(system);
        }
        pointerData.Reset();
        pointerData.position = position;
        raycastResults.Clear();
        system.RaycastAll(pointerData, raycastResults);
        bool overUI = false;
        foreach (var hit in raycastResults)
            if (hit.module is GraphicRaycaster && !hit.gameObject.GetComponentInParent<Joystick>())
            { overUI = true; break; }
        raycastResults.Clear();
        return overUI;
    }

    public static bool IsInsideViewWithPadding(Vector2 position, float padding)
    {
        var camera = GameController.Instance.GameCamera;
        Vector2 offset = position - (Vector2)camera.transform.position;
        float height = camera.orthographicSize;
        float width = height * camera.aspect;
        return Mathf.Abs(offset.x) < width - padding && Mathf.Abs(offset.y) < height - padding;
    }

    public static bool IsOutsideViewWithMargin(Vector2 position, float margin)
    {
        var camera = GameController.Instance.GameCamera;
        Vector2 offset = position - (Vector2)camera.transform.position;
        float height = camera.orthographicSize;
        float width = height * camera.aspect;
        return Mathf.Abs(offset.x) > width + margin || Mathf.Abs(offset.y) > height + margin;
    }

    public static void RefreshLayout(Transform root)
    {
        if (root is RectTransform rect) LayoutRebuilder.MarkLayoutForRebuild(rect);
    }
}
