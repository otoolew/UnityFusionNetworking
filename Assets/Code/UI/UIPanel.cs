using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanel : MonoBehaviour
{
    [SerializeField] private GameObject content;
    public virtual GameObject Content { get => content; set => content = value; }
    public virtual bool IsDisplayed => content.gameObject.activeSelf && content.gameObject.activeInHierarchy;
    
    public virtual void Display(bool value)
    {
        if (content != null)
        {
            content.gameObject.SetActive(value);
        }
    }
}
