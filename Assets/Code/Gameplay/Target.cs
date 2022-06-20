using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Target : NetworkBehaviour, IDamageable
{
    [Networked]public int HealthValue { get; set; }
    
    [SerializeField] private int healthMax;
    public int HealthMax { get => healthMax; set => healthMax = value; }
    
    [SerializeField] private Text healthText;
    public Text HealthText { get => healthText; set => healthText = value; }
    
    public override void Spawned()
    {
        base.Spawned();
        HealthValue = healthMax;
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
