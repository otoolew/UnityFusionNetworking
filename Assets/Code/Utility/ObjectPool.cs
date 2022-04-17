using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utility
{
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
    public class ObjectPool
    {
        private static Dictionary<object, ObjectPool> pools = new Dictionary<object, ObjectPool>();
        
        private static ObjectPoolRoot poolRoot;  
        
        private List<PooledObject> pool = new List<PooledObject>();
         
        [SerializeField] private PooledObject prefab;
        public PooledObject Prefab { get => prefab; set => this.prefab = value; }
        
        public static T Instantiate<T>(T prefab) where T : PooledObject
        {
            return Instantiate(prefab, Vector3.zero, Quaternion.identity);
        }
        
        public ObjectPool(PooledObject value)
        {
            prefab = value;
        }

        public PooledObject Instantiate()
        {
            var transform = Prefab.transform;
            return Instantiate(transform.position, transform.rotation);
        }

        public PooledObject Instantiate(Vector3 p, Quaternion q, Transform parent = null)
        {
            PooledObject pooledObject = null;

            if (pool.Count > 0)
            {
                var t = pool[0];
                if (t) // In case a recycled object was destroyed
                {
                    Transform xform = t.transform;
                    xform.SetParent(parent, false);
                    xform.position = p;
                    xform.rotation = q;
                    pooledObject = t;
                }
                else
                {
                    Debug.LogWarning("Recycled object of type <" + prefab + "> was destroyed - not re-using!");
                }

                pool.RemoveAt(0);
            }

            if (pooledObject == null)
            {
                pooledObject = Object.Instantiate(prefab, p, q, parent);
                pooledObject.name = "Instance(" + pooledObject.name + ")";
                pooledObject.pool = this;
            }

            pooledObject.OnRecycled();
            pooledObject.gameObject.SetActive(true);
            return pooledObject;
        }

        private void Clear()
        {
            foreach (var pooled in pool)
            {
                Object.Destroy(pooled);
            }
            pool = new List<PooledObject>();
        }
        


        public static ObjectPool Get<T>(T prefab) where T : PooledObject
        {
            ObjectPool pool;
            if (!pools.TryGetValue(prefab, out pool))
            {
                pool = new ObjectPool(prefab);
                pools[prefab] = pool;
            }

            return pool;
        }

        public static T Instantiate<T>(T prefab, Vector3 pos, Quaternion q, Transform parent = null)
            where T : PooledObject
        {
            return (T)Get(prefab).Instantiate(pos, q, parent);
        }

        public static T Instantiate<T>(Vector3 pos, Quaternion q, Transform parent = null) where T : PooledObject
        {
            if (!pools.TryGetValue(typeof(T), out var pool))
            {
                GameObject go = new GameObject("Prefab<" + typeof(T).Name + ">");
                go.SetActive(false);
                T prefab = go.AddComponent<T>();
                pool = new ObjectPool(prefab);
                pools[typeof(T)] = pool;
            }

            return (T)pool.Instantiate(pos, q, parent);
        }

        public static void Recycle(PooledObject po)
        {
            if (po != null)
            {
                if (po.pool == null)
                {
                    po.gameObject
                        .SetActive(false); // Should always disable before re-parenting, or we will dirty it twice
                    po.transform.SetParent(null, false);
                    Object.Destroy(po.gameObject);
                }
                else
                {
                    po.pool.pool.Add(po);
                    if (poolRoot == null)
                    {
                        poolRoot = Singleton<ObjectPoolRoot>.Instance;
                        poolRoot.name = "ObjectPoolRoot";
                    }

                    po.gameObject
                        .SetActive(false); // Should always disable before re-parenting, or we will dirty it twice
                    po.transform.SetParent(poolRoot.transform, false);
                }
            }
        }

        public static void ClearPools()
        {
            foreach (ObjectPool pool in pools.Values)
            {
                pool.Clear();
            }

            pools = new Dictionary<object, ObjectPool>();
        }
    }
}