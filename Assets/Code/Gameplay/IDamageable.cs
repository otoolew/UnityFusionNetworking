using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public interface IDamageable
{
    [Networked]public int HealthValue { get; set; }
    int HealthMax { get; set; }
    void TakeDamage(int damageValue);
    void TakeDamage(int damageValue, Vector3 impulse);
}
