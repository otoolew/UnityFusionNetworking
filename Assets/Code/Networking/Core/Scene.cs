using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.Serialization;
using UnityFusionNetworking.UI;

namespace UnityFusionNetworking
{
    [System.Serializable]
    public class SceneContext
    {
        public SceneUI UI;
        public ObjectCache ObjectCache;
        public SceneInput Input;
        public PlayerCamera Camera;
        
        [HideInInspector] public GameSession GameSession;
        [HideInInspector] public NetworkRunner Runner;

        [HideInInspector] public PlayerRef LocalPlayerRef;
        [HideInInspector] public PlayerRef ObservedPlayerRef;
        [HideInInspector] public PlayerAgent ObservedAgent;
    }

    public class Scene : CoreBehaviour
    {
        // PUBLIC MEMBERS

        public bool ContextReady { get; private set; }
        public bool IsActive { get; private set; }
        public SceneContext Context => context;

        // PRIVATE MEMBERS

        [SerializeField] private bool selfInitialize;
        [FormerlySerializedAs("_context")] [SerializeField] private SceneContext context;

        private bool isInitialized;
        private readonly List<SceneService> services = new List<SceneService>();

        // PUBLIC METHODS

        public void PrepareContext()
        {
            if (ContextReady == true) return;

            OnPrepareContext(context);
            ContextReady = true;
        }

        public void Initialize()
        {
            if (isInitialized == true) return;

            PrepareContext();
            CollectServices();

            OnInitialize();

            isInitialized = true;
        }

        public void Deinitialize()
        {
            if (isInitialized == false) return;

            Deactivate();

            OnDeinitialize();

            isInitialized = false;
        }

        public IEnumerator Activate()
        {
            if (isInitialized == false) yield break;

            yield return OnActivate();

            IsActive = true;
        }

        public void Deactivate()
        {
            if (IsActive == false) return;

            OnDeactivate();

            IsActive = false;
        }

        public T GetService<T>() where T : SceneService
        {
            for (int i = 0, count = services.Count; i < count; i++)
            {
                if (services[i] is T service) return service;
            }

            return null;
        }

        public void Quit()
        {
            Deinitialize();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
        }
        
        protected void Awake()
        {
            if (selfInitialize == true)
            {
                Initialize();
            }
        }

        protected IEnumerator Start()
        {
            if (isInitialized == false) yield break;

            if (selfInitialize == true && IsActive == false)
            {
                // UI cannot be initialized in Awake, Canvas elements need to Awake first
                AddService(context.UI);

                yield return Activate();
            }
        }

        protected virtual void Update()
        {
            if (IsActive == false) return;

            OnTick();
        }

        protected virtual void LateUpdate()
        {
            if (IsActive == false) return;

            OnLateTick();
        }

        protected void OnDestroy()
        {
            Deinitialize();
        }

        protected void OnApplicationQuit()
        {
            Deinitialize();
        }
        
        protected virtual void OnPrepareContext(SceneContext context)
        {
        }

        protected virtual void CollectServices()
        {
            var services = GetComponentsInChildren<SceneService>(true);

            foreach (var service in services)
            {
                AddService(service);
            }
        }

        protected virtual void OnInitialize()
        {
            for (int i = 0; i < services.Count; i++)
            {
                services[i].Initialize(this, Context);
            }
        }

        protected virtual IEnumerator OnActivate()
        {
            for (int i = 0; i < services.Count; i++)
            {
                services[i].Activate();
            }

            yield break;
        }

        protected virtual void OnTick()
        {
            for (int i = 0, count = services.Count; i < count; i++)
            {
                services[i].Tick();
            }
        }

        protected virtual void OnLateTick()
        {
            for (int i = 0, count = services.Count; i < count; i++)
            {
                services[i].LateTick();
            }
        }

        protected virtual void OnDeactivate()
        {
            for (int i = 0; i < services.Count; i++)
            {
                services[i].Deactivate();
            }
        }

        protected virtual void OnDeinitialize()
        {
            for (int i = 0; i < services.Count; i++)
            {
                services[i].Deinitialize();
            }

            services.Clear();
        }

        protected void AddService(SceneService service)
        {
            if (service == null)
            {
                Debug.LogError($"Missing service");
                return;
            }

            if (services.Contains(service) == true)
            {
                Debug.LogError($"Service {service.gameObject.name} already added.");
                return;
            }

            services.Add(service);

            if (isInitialized == true)
            {
                service.Initialize(this, Context);
            }

            if (IsActive == true)
            {
                service.Activate();
            }
        }
    }
}