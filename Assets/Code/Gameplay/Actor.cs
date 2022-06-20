using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public abstract class Actor : NetworkBehaviour, IPredictedSpawnBehaviour
{
    private Vector3 interpolateFrom;
    private Vector3 interpolateTo;
    private NetworkTransform networkTransform;
    // Start is called before the first frame update
    public abstract void InitNetworkState(Vector3 ownerVelocity);

    public void PredictedSpawnSpawned()
    {
        networkTransform = GetComponent<NetworkTransform>();
        interpolateTo = transform.position;
        interpolateFrom = interpolateTo;
        networkTransform.InterpolationTarget.position = interpolateTo;
        Spawned();
    }

    public void PredictedSpawnUpdate()
    {
        interpolateFrom = interpolateTo;
        interpolateTo = transform.position;
        FixedUpdateNetwork();
    }

    void IPredictedSpawnBehaviour.PredictedSpawnRender() {
        var a = Runner.Simulation.StateAlpha;
        networkTransform.InterpolationTarget.position = Vector3.Lerp(interpolateFrom, interpolateTo, a);
    }

    public void PredictedSpawnFailed()
    {
        Debug.LogWarning($"Predicted Spawn Failed Object = {Object.Id}, Instance = {gameObject.GetInstanceID()}, IsResimulation = {Runner.IsResimulation}");
        Runner.Despawn(Object, true);
    }

    public void PredictedSpawnSuccess()
    {
        DebugLogMessage.Log(Color.green, $"Predicted Spawn Failed Object = {Object.Id}, Instance = {gameObject.GetInstanceID()}, IsResimulation = {Runner.IsResimulation}");
    }
}
