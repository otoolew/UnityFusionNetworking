using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkPoolManager : MonoBehaviour, INetworkObjectPool
{
    private Dictionary<object, NetworkObjectPool> prefabPoolDictionary = new Dictionary<object, NetworkObjectPool>();

    private Dictionary<NetworkObject, NetworkObjectPool> poolInstanceDictionary =
        new Dictionary<NetworkObject, NetworkObjectPool>();

    public NetworkObjectPool GetPool<T>(T prefab) where T : NetworkObject
    {
        if (!prefabPoolDictionary.TryGetValue(prefab, out var pool))
        {
            pool = new NetworkObjectPool();
            prefabPoolDictionary[prefab] = pool;
        }
        return pool;
    }

    public NetworkObject AcquireInstance(NetworkRunner runner, NetworkPrefabInfo info)
    {
        if (NetworkProjectConfig.Global.PrefabTable.TryGetPrefab(info.Prefab, out var prefab))
        {
            NetworkObjectPool pool = GetPool(prefab);
            NetworkObject newt = pool.GetFromPool(Vector3.zero, Quaternion.identity);
            if (newt == null)
            {
                newt = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                poolInstanceDictionary[newt] = pool;
            }

            newt.gameObject.SetActive(true);
            return newt;
        }

        Debug.LogError("No prefab for " + info.Prefab);
        return null;
    }

    public void ReleaseInstance(NetworkRunner runner, NetworkObject no, bool isSceneObject)
    {
        Debug.Log($"Releasing {no} instance, isSceneObject={isSceneObject}");
        if (no != null)
        {
            NetworkObjectPool pool;
            if (poolInstanceDictionary.TryGetValue(no, out pool))
            {
                pool.ReturnToPool(no);
                no.gameObject.SetActive(false); // Should always disable before re-parenting, or we will dirty it twice
                no.transform.SetParent(transform, false);
            }
            else
            {
                no.gameObject.SetActive(false); // Should always disable before re-parenting, or we will dirty it twice
                no.transform.SetParent(null, false);
                Destroy(no.gameObject);
            }
        }
    }

    public void ClearPools()
    {
        foreach (NetworkObjectPool pool in prefabPoolDictionary.Values)
        {
            pool.Clear();
        }

        foreach (NetworkObjectPool pool in poolInstanceDictionary.Values)
        {
            pool.Clear();
        }

        prefabPoolDictionary = new Dictionary<object, NetworkObjectPool>();
    }
}
