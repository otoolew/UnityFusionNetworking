using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Object = UnityEngine.Object;
/// <summary>
/// ObjectPool speeds up creation of prefab instances by re-cycling old instances rather than destroying them.
/// 
/// Simply attach the PooledObject to your prefab (or derive your own component from PooledObject) and then use
/// ObjectPool.Instantiate( prefab ); to create a pooled object.
/// 
/// It is important that you do not destroy the pooled object explicitly but instead call ObjectPool.Recycle( instance );
/// 
/// Also, since objects are re-used and potentially modified between uses you cannot assume that a new instance will 
/// have the default prefab property values, but must explicitly reset the instance by overriding the OnRecycled() method
/// 
/// Finally, when objects are not used, they are re-parented to a common pool root node - this will be created for you.
/// </summary>
public class NetworkObjectPool
{
    private List<NetworkObject> pool = new List<NetworkObject>();

    public NetworkObject GetFromPool(Vector3 p, Quaternion q, Transform parent = null)
    {
        NetworkObject newt = null;

        while (pool.Count > 0 && newt==null)
        {
            var t = pool[0];
            if (t) // In case a recycled object was destroyed
            {
                Transform xform = t.transform;
                xform.SetParent(parent, false);
                xform.position = p;
                xform.rotation = q;
                newt = t;
            }
            else
            {
                Debug.LogWarning("Recycled object was destroyed - not re-using!");
            }

            pool.RemoveAt(0);
        }

        return newt;
    }

    public void Clear()
    {
        foreach (var pooled in pool)
        {
            if (pooled)
            {
                Debug.Log($"Destroying pooled object: {pooled.gameObject.name}");
                Object.Destroy(pooled.gameObject);
            }
        }

        pool = new List<NetworkObject>();
    }

    public void ReturnToPool(NetworkObject no)
    {
        pool.Add(no);
    }
}
