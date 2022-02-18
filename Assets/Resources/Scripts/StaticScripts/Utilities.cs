using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Utilities : MonoBehaviour
{

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
}
