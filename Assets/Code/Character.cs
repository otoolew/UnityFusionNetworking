using Fusion;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utility;

/// <summary>
/// Visual representation of a Player - the Character is instantiated by the map once it's loaded.
/// Collider-based character controller movement (not related to Unity's CharacterController type).
/// Replicates both the internal state (Velocity, MaxSpeed, etc) and the Unity Transform data from the
/// NetworkObject.StateAuthority to all other peers. Add this component to a GameObject to control
/// movement and sync the position and/or rotation accurately, including client-side prediction.
/// </summary>

[RequireComponent(typeof(NetworkCharacterControllerPrototype))]
public class Character : NetworkBehaviour
{
	#region Player
	[SerializeField] private PlayerInfo playerInfo;
	public PlayerInfo PlayerInfo { get => playerInfo; set => playerInfo = value; }
	#endregion
	
	#region Components
	[SerializeField] private PlayerCamera playerCamera;
	public PlayerCamera PlayerCamera { get => playerCamera; set => this.playerCamera = value; }
	
	[SerializeField] private NetworkCharacterControllerPrototype networkCharacterController;
	public NetworkCharacterControllerPrototype NetworkCharacterController { get => networkCharacterController; set => networkCharacterController = value; }
	
	[SerializeField] private MeshRenderer meshRenderer;
	public MeshRenderer MeshRenderer { get => meshRenderer; set => meshRenderer = value; }
	
	[SerializeField] private Text nameTagText;
	public Text NameTagText { get => nameTagText; set => nameTagText = value; }
	#endregion

	#region Networked Properties
	[Networked]public Vector3 MoveDirection { get; set; }
	[Networked]public Vector3 LookDirection { get; set; }
	 
	#endregion
	
	public bool TransformLocal = false;

	[DrawIf(nameof(ShowSpeed), DrawIfHideType.Hide, DoIfCompareOperator.NotEqual)]
	public float Speed = 6f;
	bool HasNCC => GetComponent<NetworkCharacterControllerPrototype>();
	bool ShowSpeed => this && !TryGetComponent<NetworkCharacterControllerPrototype>(out _);
	
	[SerializeField] private FusionEvent onCharacterSpawn;
	public void Awake() 
	{
		CacheComponents();
	}
	private void OnEnable()
	{
		onCharacterSpawn.RegisterResponse(CharacterSpawned);
	}
	private void OnDisable()
	{
		onCharacterSpawn.RemoveResponse(CharacterSpawned);
	}

	private void CharacterSpawned(PlayerRef player, NetworkRunner runner)
	{
		Debug.Log($"CharacterSpawned Fusion Event -> Player [{player.PlayerId}]");
	}
	private void CacheComponents() 
	{
		if (playerCamera == null) PlayerCamera = FindObjectOfType<PlayerCamera>();
		if (networkCharacterController == null) NetworkCharacterController = GetComponent<NetworkCharacterControllerPrototype>();
		if (meshRenderer == null) MeshRenderer = GetComponent<MeshRenderer>();
		if (nameTagText == null) NameTagText = GetComponent<Text>();
	}
	
	
	public enum State
	{
		NEW,
		DESPAWNING,
		SPAWNING,
		ACTIVE,
		DEAD
	}
	
	[Networked(OnChanged = nameof(OnStateChanged))]
	public State CurrentState { get; set; }
	
	public override void Spawned()
	{
		CacheComponents();
		
		playerInfo = GameManager.Instance.GetPlayer(Object.InputAuthority);
		nameTagText.text = playerInfo.DisplayName;
		meshRenderer.material.color = playerInfo.Color;

		if (Object.HasInputAuthority)
		{
			PlayerCamera = Camera.main?.transform.GetComponent<PlayerCamera>();
			if (PlayerCamera != null)
			{
				PlayerCamera.AssignFollowTarget(transform);
			}
		}
	}
	
	public override void FixedUpdateNetwork() 
	{
		if (Runner.Config.PhysicsEngine == NetworkProjectConfig.PhysicsEngines.None) {
			return;
		}
		
		if (GetInput(out CharacterInputData input))
		{
			Vector3 direction = default;

			if (input.IsDown(CharacterInputData.FORWARD))
			{
				direction += TransformLocal ? transform.forward : Vector3.forward;
			}

			if (input.IsDown(CharacterInputData.BACKWARD))
			{
				direction -= TransformLocal ? transform.forward : Vector3.forward;
			}

			if (input.IsDown(CharacterInputData.LEFT))
			{
				direction -= TransformLocal ? transform.right : Vector3.right;
			}

			if (input.IsDown(CharacterInputData.RIGHT))
			{
				direction += TransformLocal ? transform.right : Vector3.right;
			}

			direction = direction.normalized;
			MoveDirection = direction;
			LookDirection = input.AimDirection; 
			Move(direction);
			LookAt(input.AimDirection);
		}
	}
	/// <summary>
	/// Render is the Fusion equivalent of Unity's Update() and unlike FixedUpdateNetwork which is very different from FixedUpdate,
	/// Render is in fact exactly the same. It even uses the same Time.deltaTime time steps. The purpose of Render is that
	/// it is always called *after* FixedUpdateNetwork - so to be safe you should use Render over Update if you're on a
	/// SimulationBehaviour.
	///
	/// Here, we use Render to update visual aspects of the Tank that does not involve changing of networked properties.
	/// </summary>
	public override void Render()
	{
		meshRenderer.gameObject.SetActive(CurrentState == State.ACTIVE);
		/*collider.enabled = state != State.Dead;
		_hitBoxRoot.enabled = state == State.Active;
		_damageVisuals.CheckHealth(life);*/
	}
	#region State Change
	public static void OnStateChanged(Changed<Character> changed)
	{
		if(changed.Behaviour)
			changed.Behaviour.OnStateChanged();
	}
	
	public void OnStateChanged()
	{
		switch (CurrentState)
		{
			case State.SPAWNING:
				Debug.Log($"[{playerInfo.Id}] Player {CurrentState}");
				break;
			case State.ACTIVE:
				Debug.Log($"[{playerInfo.Id}] Player {CurrentState}");
				break;
			case State.DEAD:
				Debug.Log($"[{playerInfo.Id}] Player {CurrentState}");
				break;
			case State.DESPAWNING:
				Debug.Log($"[{playerInfo.Id}] Player {CurrentState}");
				break;
		}
	}
	#endregion

	#region Character Movement
	
	public void Move()
	{
		if (CurrentState == State.ACTIVE)
		{
			networkCharacterController.Move(MoveDirection);
		}
	}
	public void Move(Vector3 direction)
	{
		if (CurrentState == State.ACTIVE)
		{
			networkCharacterController.Move(direction);
		}
	}
	
	
	public void LookAt(Vector3 direction)
	{
		LookDirection = direction;
		if (CurrentState == State.ACTIVE)
		{
			if (direction.sqrMagnitude > 0)
			{
				networkCharacterController.transform.rotation = Quaternion.Euler(direction);
				
				//networkCharacterController.transform.forward = Vector3.Lerp(networkCharacterController.transform.forward, direction, Time.deltaTime * 100f);
			}
		}
	}
	public void Jump()
	{
		if (networkCharacterController)
		{
			networkCharacterController.Jump();
		}
		else
		{
			MoveDirection += (TransformLocal ? transform.up : Vector3.up);
		}
	}
	#endregion

	#region Behaviour

	public void Kill()
	{
		Rpc_Kill();
	}
	
	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void Rpc_Kill()
	{
		
		CurrentState = State.DEAD;
	}
	#endregion

}