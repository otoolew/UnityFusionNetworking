using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LeaveGameButton : Button
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        //GameManager.Instance.ExitSession();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
    }
    protected override void OnDisable()
    {
        base.OnEnable();
        
    }
}
