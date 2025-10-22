using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Utilities : MonoBehaviour
{
    public static long Round(double value)
    {
        return (long)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    public static int Round(float value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    public static string numberToString(long value)
    {
        if (value >= 10000)
            return value.ToString("n0");
        else
            return value.ToString();
    }

    public static string numberToString(int value)
    {
        if (value >= 10000)
            return value.ToString("n0");
        else
            return value.ToString();
    }

    public static IEnumerator CountAnimationRoutine(
        TextMeshProUGUI uiText,
        long fromValue,
        long toValue,
        float stepDelay,
        float totalDuration,
        AudioClip countingSound = null)
    {
        long current = fromValue;
        uiText.text = numberToString(current);

        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            yield return new WaitForSeconds(stepDelay);

            if (countingSound != null) AudioController.PlaySound(clip: countingSound, pitch: 1.2f);
            elapsed += stepDelay;

            // linear Interpolation between Start und 
            float t = Mathf.Clamp01(elapsed / totalDuration);
            current = (long)Mathf.Lerp(fromValue, toValue, t);

            uiText.text = numberToString(current);
        }

        // sicherstellen, dass das Ziel erreicht wird
        uiText.text = numberToString(toValue);
    }

    public static void recalculateBackgroundScale(GameObject background)
    {
        if (background == null)
            return;

        SpriteRenderer sr = background.GetComponent<SpriteRenderer>();

        float worldScreenHeight = Camera.main.orthographicSize * 2;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

        background.transform.localScale = new Vector3(
            worldScreenWidth / sr.sprite.bounds.size.x,
            worldScreenHeight / sr.sprite.bounds.size.y, 1);
    }


    public static bool isPointerOverUIElement()
    {
        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    public static bool isPointerOverUIElement(GameObject uiElement)
    {
        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (RaycastResult raycastElement in raycastResults)
        {
            if (raycastElement.gameObject.Equals(uiElement))
                return true;
        }

        return false;
    }

    public static bool isPointerOverJoyStick()
    {
        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (RaycastResult raycastElement in raycastResults)
        {
            if (raycastElement.gameObject.tag.Equals("Joystick"))
                return true;
        }

        return false;
    }


    public static bool IsInsideViewWithPadding(Transform target, float padding)
    {
        return IsInsideViewWithPadding(target.position, padding);
    }

    public static bool IsInsideViewWithPadding(Vector2 position, float padding)
    {
        float height = Camera.main.orthographicSize;
        float width = height * Camera.main.aspect;

        return (position.x < width - padding &&
                position.x > -width + padding &&
                position.y < height - padding &&
                position.y > -height + padding);
    }

    public static bool IsOutsideViewWithMargin(Transform target, float margin)
    {
        return IsOutsideViewWithMargin(target.position, margin);
    }

    public static bool IsOutsideViewWithMargin(Vector2 position, float margin)
    {
        float height = Camera.main.orthographicSize;
        float width = height * Camera.main.aspect;

        return (position.x > width + margin ||
                position.x < -width - margin ||
                position.y > height + margin ||
                position.y < -height - margin);
    }


    public static void SetAllParticlesPaused(bool pause, List<GameObject> exceptions = null)
    {
        foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            if (exceptions != null)
            {
                if (exceptions.Any(e =>
                    e != null && (ps.gameObject == e || ps.transform.IsChildOf(e.transform))))
                {
                    continue;
                }
            }

            if (pause)
                ps.Pause();
            else
                ps.Play();
        }
    }


    /// <summary>
    /// Erzwingt einen vollständigen UI-Layout-Rebuild für alle aktiven Canvases.
    /// Nutze das direkt nach Änderungen an Texten, Aktivierungen oder Kind-Änderungen.
    /// </summary>
    public static void ForceUIRefresh()
    {
        Canvas.ForceUpdateCanvases();

        // Alle aktiven Layout-Gruppen rebuilden (Horizontal/Vertical/Grid etc.)
        var groups = FindObjectsByType<LayoutGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < groups.Length; i++)
        {
            var rt = groups[i].transform as RectTransform;
            if (rt != null && rt.gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        // Alle aktiven ContentSizeFitter „einrasten“
        var fitters = FindObjectsByType<ContentSizeFitter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < fitters.Length; i++)
        {
            var rt = fitters[i].transform as RectTransform;
            if (rt != null && rt.gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Erzwingt den Rebuild nur für einen UI-Teilbaum (performanter).
    /// </summary>
    public static void ForceUIRefresh(RectTransform root)
    {
        if (root == null || !root.gameObject.activeInHierarchy) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        // Optional: bekannte Kinder-Layouts/Fitter im Subtree
        var groups = root.GetComponentsInChildren<LayoutGroup>(includeInactive: false);
        for (int i = 0; i < groups.Length; i++)
        {
            var rt = groups[i].transform as RectTransform;
            if (rt != null && rt.gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        var fitters = root.GetComponentsInChildren<ContentSizeFitter>(includeInactive: false);
        for (int i = 0; i < fitters.Length; i++)
        {
            var rt = fitters[i].transform as RectTransform;
            if (rt != null && rt.gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Gleiche Funktion, aber sicher „nächstes Frame“ – hilft bei Fällen,
    /// in denen Unity erst nach einem Frame korrekte Größen hat (SetActive/Instantiate).
    /// </summary>
    public static void ForceUIRefreshNextFrame(MonoBehaviour host, RectTransform root = null)
    {
        if (host == null) { ForceUIRefresh(root); return; }
        host.StartCoroutine(CoForceUIRefreshNextFrame(root));
    }

    private static IEnumerator CoForceUIRefreshNextFrame(RectTransform root)
    {
        // ein Frame warten (bis End-of-Frame reicht meist auch ein yield null)
        yield return null;
        if (root == null) ForceUIRefresh();
        else ForceUIRefresh(root);
    }


    /// Ein Aufruf reicht: Utilities.UIRefreshDeep(root);
    public static void UIRefreshDeep(RectTransform root)
    {
        if (root == null || !root.gameObject.activeInHierarchy) return;
        Runner.Run(Co());
        IEnumerator Co()
        {
            // TMP erst aktualisieren (korrekte preferred-Sizes)
            var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(false);
            for (int i = 0; i < tmps.Length; i++) tmps[i].ForceMeshUpdate();

            // Eltern bis zum Canvas markieren
            Transform t = root;
            while (t != null)
            {
                var rt = t as RectTransform;
                if (rt) LayoutRebuilder.MarkLayoutForRebuild(rt);
                if (t.GetComponent<Canvas>()) break;
                t = t.parent;
            }

            // zwei Ticks warten
            yield return new WaitForEndOfFrame();
            ForceOnce(root);
            yield return null;
            ForceOnce(root);
        }

        static void ForceOnce(RectTransform target)
        {
            if (!target) return;

            // obersten Layout-Container finden
            RectTransform container = null;
            Transform p = target;
            while (p != null)
            {
                var rt = p as RectTransform;
                if (rt && rt.GetComponent<LayoutGroup>()) container = rt;
                if (p.GetComponent<Canvas>()) break;
                p = p.parent;
            }
            if (container == null) container = target;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
            Canvas.ForceUpdateCanvases();
        }
    }

    // Mini-Runner, lebt nur für die Coroutine und zerstört sich danach
    private sealed class Runner : MonoBehaviour
    {
        public static void Run(IEnumerator routine)
        {
            var go = new GameObject("_UIRefreshRunner");
            go.hideFlags = HideFlags.HideInHierarchy;
            var r = go.AddComponent<Runner>();
            r.StartCoroutine(r.Wrap(routine));
        }

        private IEnumerator Wrap(IEnumerator routine)
        {
            yield return routine;
            if (this) Destroy(gameObject);
        }
    }

}
