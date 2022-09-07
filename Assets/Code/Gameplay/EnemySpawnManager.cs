using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemySpawnManager : NetworkBehaviour
{
    [SerializeField] private EnemyCharacter enemyCharacterPrefab; // Networked projectile
    [Networked] public TickTimer spawnDelayTimer { get; set; }
    [SerializeField] private float spawnTime;
    [SerializeField] private float spawnRadius;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool isRunning;
    
    public override void Spawned()
    {
        
    }
    
    public void InitNetworkState()
    {
        // The spawn boundaries are based of the camera settings
    }

    public override void FixedUpdateNetwork()
    {
        if (isRunning)
        {
            SpawnEnemy();
        }
    }
    
    public void StartSpawning()
    {
        if (Object.HasStateAuthority == false) return;
        spawnDelayTimer = TickTimer.CreateFromSeconds(Runner, spawnTime);
        isRunning = true;
    }
    
    public void StartSpawning(float delay)
    {
        if (Object.HasStateAuthority == false) return;
        spawnDelayTimer = TickTimer.CreateFromSeconds(Runner, delay);
        isRunning = true;
    }

    private void SpawnEnemy()
    {
        if (spawnDelayTimer.Expired(Runner) == false) return;

        Vector3 position =  Random.insideUnitCircle.normalized * spawnRadius;
        var enemy = Runner.Spawn(enemyCharacterPrefab, position, spawnPoint.rotation, PlayerRef.None);
        DebugLogMessage.Log(Color.green, $"{enemy.gameObject.name} Spawned");
        // Sets the delay until the next spawn.
        spawnDelayTimer = TickTimer.CreateFromSeconds(Runner, spawnTime);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position,spawnRadius);
    }
}