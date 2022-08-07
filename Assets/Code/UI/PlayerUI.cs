using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    public GameObject PauseMenu { get => pauseMenu; set => pauseMenu = value; }
    
    public void TogglePauseMenu()
    {
        pauseMenu.gameObject.SetActive(!pauseMenu.gameObject.activeInHierarchy);
    }
}
