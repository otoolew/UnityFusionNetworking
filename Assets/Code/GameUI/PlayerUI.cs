using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private MenuPanel pauseMenu;
    public MenuPanel PauseMenu { get => pauseMenu; set => pauseMenu = value; }
}
