using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Projectile Data", fileName = "projectileData")]
public class ProjectileData : ScriptableObject
{
    public byte damage;
    public float speed = 100;
    public float radius = 0.05f;
    public float gravity = 0f;
    public float timeToLive = 1.5f;
    public float timeToFade = 0.5f;
    public float ownerVelocityMultiplier = 1f;
}
