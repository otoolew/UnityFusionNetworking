using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MenuPanel : MonoBehaviour, IPanel
{
    [SerializeField] private Button menuButton;
    public Button MenuButton { get => menuButton; set => menuButton = value; }
    
    [SerializeField] private GameObject menuContent;
    public GameObject Content { get => menuContent; set => menuContent = value; }
    public bool IsDisplayed => menuContent.gameObject.activeSelf && menuContent.gameObject.activeInHierarchy;

    private void Start()
    {
        menuButton.onClick.AddListener(ToggleDisplay);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleDisplay();
        }
    }

    public void Display(bool value)
    {
        DebugLogMessage.Log(Color.white, $"{menuContent.gameObject.name} Display {value}");
        if (menuContent != null)
        {
            menuContent.gameObject.SetActive(value);
        }
    }
    
    public void ToggleDisplay()
    {
        if (menuContent != null)
        {
            menuContent.gameObject.SetActive(!IsDisplayed);
        }
    }
}
