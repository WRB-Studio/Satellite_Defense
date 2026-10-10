using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimDirectionHint : MonoBehaviour
{
    [Min(0f)] public float surfaceOffset = .6f;
    [Min(.01f)] public float arrowSize = .18f;
    [Min(.001f)] public float lineWidth = .016f;
    public Color color = new(.15f, 1f, .18f, .2f);

    private LineRenderer line;
    private Planet planet;
    private Collider2D planetCollider;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 3;
        float half = arrowSize * .5f;
        line.SetPositions(new[] { new Vector3(-half, -half * .4f),
            new Vector3(0, half * .7f), new Vector3(half, -half * .4f) });
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
        line.widthMultiplier = lineWidth;
        line.startColor = line.endColor = color;
        line.enabled = false;
    }

    private void LateUpdate()
    {
        var game = GameController.Instance;
        bool visible = game && game.enableJoystickControll && game.TryGetAimDirection(out _);
        line.enabled = visible;
        if (!visible) return;
        if (planet != game.ActivePlanet)
        {
            planet = game.ActivePlanet;
            planetCollider = planet ? planet.GetComponent<Collider2D>() : null;
        }
        if (!planet) { line.enabled = false; return; }
        game.TryGetAimDirection(out Vector2 direction);
        direction.Normalize();
        float radius = planetCollider ? Mathf.Max(planetCollider.bounds.extents.x, planetCollider.bounds.extents.y) : .5f;
        transform.position = planet.transform.position + (Vector3)(direction * (radius + surfaceOffset));
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
    }
}
