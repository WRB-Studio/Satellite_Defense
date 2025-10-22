using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIToastMessage : MonoBehaviour
{
    public static UIToastMessage Instance;

    [Header("UI")]
    public GameObject toastPanel;
    public TextMeshProUGUI txtMessage;
    private Image panelImage;

    [Header("Timing")]
    public float showDuration = 2f;     // Wie lange sichtbar
    public float fadeDuration = 0.3f;   // Dauer für Ein-/Ausblenden

    private readonly Queue<(string msg, float dur)> queue = new();
    private bool isShowing;

    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        StopAllCoroutines();
        queue.Clear();
        isShowing = false;
        toastPanel.SetActive(false);
        panelImage = toastPanel.GetComponent<Image>();

        var c = panelImage.color;
        c.a = 0f;
        panelImage.color = c;
    }

    public void ShowToast(string message, float dur = 0f)
    {
        if (queue.Any(q => q.msg == message)) return;

        queue.Enqueue((message, dur > 0f ? dur : showDuration));

        if (!isShowing) StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isShowing = true;

        while (queue.Count > 0)
        {
            var (msg, dur) = queue.Dequeue();

            txtMessage.text = msg;
            toastPanel.SetActive(true);

            // Fade In
            yield return Fade(0f, 1f, fadeDuration);

            // Anzeigen für showDuration
            yield return new WaitForSecondsRealtime(dur);

            // Fade Out
            yield return Fade(1f, 0f, fadeDuration);

            toastPanel.SetActive(false);
        }

        isShowing = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        var c = panelImage.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);
            panelImage.color = new Color(c.r, c.g, c.b, alpha);

            if (txtMessage) // Text gleich mitfaden
            {
                var tc = txtMessage.color;
                txtMessage.color = new Color(tc.r, tc.g, tc.b, alpha);
            }

            yield return null;
        }
    }
}
