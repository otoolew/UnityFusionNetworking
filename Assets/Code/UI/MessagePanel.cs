using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MessagePanel : MonoBehaviour
{
    [SerializeField] private GameObject panelContent;
    public GameObject Content { get => panelContent; set => panelContent = value; }
    public bool IsDisplayed => panelContent.gameObject.activeSelf && panelContent.gameObject.activeInHierarchy;
    
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text bodyText;
    private void Awake()
    {
        gameObject.SetActive(false);
    }
    
    public void Display(bool value)
    {
        DebugLogMessage.Log(Color.white, $"{panelContent.gameObject.name} Display {value}");
        if (panelContent != null)
        {
            panelContent.gameObject.SetActive(value);
        }
    }
    
    public void ToggleDisplay()
    {
        if (panelContent != null)
        {
            panelContent.gameObject.SetActive(!IsDisplayed);
        }
    }
    
    public void SetMessage(string message)
    {
        headerText.text = "Alert!";
        this.bodyText.text = message;
        gameObject.SetActive(true);
    }
    
    public void SetMessage(string header, string message)
    {
        headerText.text = header;
        this.bodyText.text = message;
        gameObject.SetActive(true);
    }
    public void OnClose()
    {
        Display(false);
    }
}
