using Fusion;
using UnityEngine;

namespace UnityFusionNetworking
{
    public struct PlayerStatistics : INetworkStruct
    {
        public PlayerRef PlayerRef;
        public short Lives;
        public short Kills;
        public short Deaths;
        public short Score;
        public TickTimer RespawnTimer;
        public byte Position;
        public bool IsAlive { get => _flags.IsBitSet(0); set => _flags.SetBit(0, value); }
        private byte _flags;
    }

    [RequireComponent(typeof(PlayerInput))]
    public class Player : ContextBehaviour
    {
        // PUBLIC MEMBERS

        [Networked(OnChanged = nameof(OnActiveAgentChanged), OnChangedTargets = OnChangedTargets.All), HideInInspector]
        public PlayerAgent ActiveAgent { get; private set; }

        [Networked] public ref PlayerStatistics Statistics => ref MakeRef<PlayerStatistics>();

        public PlayerInput Input { get; private set; }

        public PlayerAgent AgentPrefab => _agentPrefab;

        // PRIVATE MEMBERS

        [SerializeField] private PlayerAgent _agentPrefab;

        private int _lastWeaponSlot;
        
        protected void Awake()
        {
            Input = GetComponent<PlayerInput>();
        }
        
        public void AssignAgent(PlayerAgent agent)
        {
            ActiveAgent = agent;
            ActiveAgent.Owner = this;

            if (Object.HasStateAuthority == true && _lastWeaponSlot != 0)
            {
                //agent.Weapons.SwitchWeapon(_lastWeaponSlot, true);
            }
        }

        public void ClearAgent()
        {
            if (ActiveAgent == null)
                return;

            ActiveAgent.Owner = null;
            ActiveAgent = null;
        }

        public void UpdateStatistics(PlayerStatistics statistics)
        {
            Statistics = statistics;
        }
        
        public override void Spawned()
        {
            Statistics.PlayerRef = Object.InputAuthority;

            if (Context.GameSession != null)
            {
                Context.GameSession.Join(this);
            }

            if (Context.Camera != null)
            {
                DebugLogMessage.Log(Color.cyan, $"Camera {Object.InputAuthority}");
            }
            gameObject.name = $"Player{Object.InputAuthority}";
        }

        public override void FixedUpdateNetwork()
        {
            bool agentValid = ActiveAgent != null && ActiveAgent.Object != null;

            Input.InputBlocked = agentValid == false || ActiveAgent.Health.IsAlive == false;

            if (agentValid == true && Object.HasStateAuthority == true)
            {
                //_lastWeaponSlot = ActiveAgent.Weapons.CurrentWeaponSlot;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (hasState == false)
                return;

            if (Context.GameSession != null)
            {
                Context.GameSession.Leave(this);
            }

            if (Object.HasStateAuthority == true && ActiveAgent != null)
            {
                Runner.Despawn(ActiveAgent.Object);
            }

            ActiveAgent = null;
        }
        
        public static void OnActiveAgentChanged(Changed<Player> changed)
        {
            if (changed.Behaviour.ActiveAgent != null)
            {
                changed.Behaviour.AssignAgent(changed.Behaviour.ActiveAgent);
            }
            else
            {
                changed.Behaviour.ClearAgent();
            }
        }
    }
}