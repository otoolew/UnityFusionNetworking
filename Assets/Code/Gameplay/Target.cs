using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Target : NetworkBehaviour, IDamageable
{
    [SerializeField] private int startingHealth;
    [SerializeField] private Text healthText;
    public Text HealthText { get => healthText; set => healthText = value; }
    
    [Networked]public int HealthValue { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        HealthValue = startingHealth;
        healthText.text = HealthValue.ToString();
    }

    public void TakeDamage(int damageValue)
    {
        HealthValue -= damageValue;
        healthText.text = HealthValue.ToString();
    }

    public void TakeDamage(int damageValue, Vector3 impulse)
    {
        HealthValue -= damageValue;
        healthText.text = HealthValue.ToString();
    }
}
