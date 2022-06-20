using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IDamageable
{
    void TakeDamage(int damageValue);
    void TakeDamage(int damageValue, Vector3 impulse);
}
