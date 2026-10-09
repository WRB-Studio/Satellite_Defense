using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : Joystick
{
    protected override void Start()
    {
        base.Start();
        background.gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (background) background.gameObject.SetActive(false);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (IsPressed) return;
        background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
        background.gameObject.SetActive(true);
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        background.gameObject.SetActive(false);
        base.OnPointerUp(eventData);
    }
}