using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIToastMessage : MonoBehaviour
{
    public static UIToastMessage Instance { get; private set; }
    public GameObject toastPanel;
    public TextMeshProUGUI txtMessage;
    [Min(0f)] public float showDuration = 2f;
    [Min(0f)] public float fadeDuration = 0.3f;

    private Image panelImage;
    private readonly Queue<(string message, float duration)> queue = new();
    private bool showing;
    private string currentMessage;

    private void Awake() => Instance = this;

    public void Init()
    {
        StopAllCoroutines();
        queue.Clear();
        showing = false;
        currentMessage = null;
        panelImage = toastPanel.GetComponent<Image>();
        toastPanel.SetActive(false);
        SetAlpha(0f);
    }

    public void ShowToast(string message, float duration = 0f)
    {
        if (string.IsNullOrWhiteSpace(message) || message == currentMessage) return;
        foreach (var pending in queue)
            if (pending.message == message) return;
        queue.Enqueue((message, duration > 0f ? duration : showDuration));
        if (!showing) StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        showing = true;
        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            currentMessage = next.message;
            txtMessage.text = next.message;
            SetAlpha(0f);
            toastPanel.SetActive(true);
            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, next.duration));
            yield return Fade(1f, 0f);
            toastPanel.SetActive(false);
            currentMessage = null;
        }
        showing = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        var color = panelImage.color;
        color.a = alpha;
        panelImage.color = color;
        color = txtMessage.color;
        color.a = alpha;
        txtMessage.color = color;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
