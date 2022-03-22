using Fusion;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
	[SerializeField] private Player player;
	public Player Player { get => player; set => player = value; }
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
		
		player = GameManager.Instance.GetPlayer(Object.InputAuthority);
		nameTagText.text = player.DisplayName;
		meshRenderer.material.color = player.Color;

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
			
			Move(direction);
			LookAt(input.AimDirection);
		}
		
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
				Debug.Log($"[{player.Id}] Player {CurrentState}");
				// TODO Play Spawning Effect
				break;
			case State.ACTIVE:
				Debug.Log($"[{player.Id}] Player {CurrentState}");
				// Do any clean up here
				// TODO Stop Spawning Effect
				break;
			case State.DEAD:
				Debug.Log($"[{player.Id}] Player {CurrentState}");
				// TODO Spawn Dead Body Here
				break;
			case State.DESPAWNING:
				Debug.Log($"[{player.Id}] Player {CurrentState}");
				// TODO Play Despawning Effect
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
	public void LookAt()
	{
		if (CurrentState == State.ACTIVE)
		{
			networkCharacterController.Move(LookDirection);
		}
	}
	public void LookAt(Vector3 direction)
	{
		if (CurrentState == State.ACTIVE)
		{
			networkCharacterController.Move(LookDirection);
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

	/// <summary>
	/// Control the rotation of hull and turret
	/// </summary>
	private void SetMeshOrientation()
	{
		// To prevent the tank from making a 180 degree turn every time we reverse the movement direction
		// we define a driving direction that creates a multiplier for the hull.forward. This allows us to
		// drive "backwards" as well as "forwards"
		/*switch (_driveDirection)
		{
			case DriveDirection.FORWARD:
				if (moveDirection.magnitude > 0.1f && Vector3.Dot(_lastMoveDirection, moveDirection.normalized) < 0f)
					_driveDirection = DriveDirection.BACKWARD;
				break;
			case DriveDirection.BACKWARD:
				if (moveDirection.magnitude > 0.1f && Vector3.Dot(_lastMoveDirection, moveDirection.normalized) < 0f)
					_driveDirection = DriveDirection.FORWARD;
				break;
		}*/

		//float multiplier = _driveDirection == DriveDirection.FORWARD ? 1 : -1;

		/*if (moveDirection.magnitude > 0.1f)
			_hull.forward = Vector3.Lerp(_hull.forward, moveDirection * multiplier, Time.deltaTime * 10f);*/

		if (LookDirection.sqrMagnitude > 0)
		{
			
		}
	}

}