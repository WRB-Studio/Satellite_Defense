using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Background : IngameEntity
{
    private SpriteRenderer spriteRenderer;
    private float lastAspect;
    private float lastSize;

    private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    private void LateUpdate()
    {
        var camera = GameController.Instance.GameCamera;
        if (!spriteRenderer.sprite || (Mathf.Approximately(lastAspect, camera.aspect) &&
            Mathf.Approximately(lastSize, camera.orthographicSize))) return;
        lastAspect = camera.aspect;
        lastSize = camera.orthographicSize;
        Vector2 size = spriteRenderer.sprite.bounds.size;
        transform.localScale = new Vector3(lastSize * 2f * lastAspect / size.x, lastSize * 2f / size.y, 1f);
    }
}
