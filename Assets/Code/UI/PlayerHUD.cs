using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private GameObject content;
    public GameObject Content { get => content; set => content = value; }
    public bool IsDisplayed => content.gameObject.activeSelf && content.gameObject.activeInHierarchy;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Display(bool value)
    {
        if (content != null)
        {
            content.gameObject.SetActive(value);
        }
    }
}
