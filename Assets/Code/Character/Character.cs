using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a Player - the Character is instantiated by the map once it's loaded.
/// Collider-based character controller movement (not related to Unity's CharacterController type).
/// Replicates both the internal state (Velocity, MaxSpeed, etc) and the Unity Transform data from the
/// NetworkObject.StateAuthority to all other peers. Add this component to a GameObject to control
/// movement and sync the position and/or rotation accurately, including client-side prediction.
/// </summary>

[RequireComponent(typeof(NetworkCharacterControllerPrototype))]
public class Character : NetworkBehaviour, INetworkRunnerCallbacks
{
	#region Player
	[SerializeField] private NetworkPlayer playerInfo;
	public NetworkPlayer PlayerInfo { get => playerInfo; set => playerInfo = value; }
	#endregion
	
	#region Components

	[SerializeField] private PlayerCamera playerCamera;
	public PlayerCamera PlayerCamera { get => playerCamera; set => this.playerCamera = value; }
	
	[SerializeField] private CharacterInput characterInput;
	public CharacterInput CharacterInput { get => characterInput; set => this.characterInput = value; }
	
	[SerializeField] private NetworkCharacterControllerPrototype networkCharacterController;
	public NetworkCharacterControllerPrototype NetworkCharacterController { get => networkCharacterController; set => networkCharacterController = value; }
	
	[SerializeField] private MeshRenderer meshRenderer;
	public MeshRenderer MeshRenderer { get => meshRenderer; set => meshRenderer = value; }
	
	[SerializeField] private Text nameTagText;
	public Text NameTagText { get => nameTagText; set => nameTagText = value; }
	#endregion

	#region Networked Properties
	[Networked]public CharacterState CharacterState { get; set; }
	[Networked]public Vector3 MoveDirection { get; set; }
	[Networked]public Vector3 LookDirection { get; set; }
	
	#endregion

	[DrawIf(nameof(ShowSpeed), DrawIfHideType.Hide, DoIfCompareOperator.NotEqual)]
	public float Speed = 6f;
	bool HasNCC => GetComponent<NetworkCharacterControllerPrototype>();
	bool ShowSpeed => this && !TryGetComponent<NetworkCharacterControllerPrototype>(out _);

	public Vector3 WorldPosition => transform.position;
	public Vector3 CenterMassPosition => transform.position += new Vector3(0,1.25f,0);
	
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
	
	public override void Spawned()
	{
		CacheComponents();
		
		playerInfo = GameManager.Instance.GetNetworkPlayer(Object.InputAuthority);
		nameTagText.text = playerInfo.DisplayName;
		meshRenderer.material.color = playerInfo.Color;

		if (Object.HasInputAuthority)
		{
			PlayerCamera = Camera.main?.transform.GetComponent<PlayerCamera>();
			if (PlayerCamera != null)
			{
				PlayerCamera.AssignFollowTarget(transform);
			}
			Runner.AddCallbacks(this);
		}
	}
	
	public override void FixedUpdateNetwork()
	{
		if (Runner.Config.PhysicsEngine == NetworkProjectConfig.PhysicsEngines.None)
		{
			return;
		}

		if (GetInput(out CharacterInputData input))
		{
			Vector3 moveVector = default;

			if (Input.GetKey(KeyCode.W))
			{
				moveVector += Vector3.forward;
			}

			if (Input.GetKey(KeyCode.S))
			{
				moveVector -= Vector3.forward;
			}

			if (Input.GetKey(KeyCode.A))
			{
				moveVector -= Vector3.right;
			}

			if (Input.GetKey(KeyCode.D))
			{
				moveVector += Vector3.right;
			}
			moveVector = moveVector.normalized;
			
			MoveDirection = moveVector;
			LookDirection = input.AimDirection; 
			
			Move(MoveDirection);
			LookAt(LookDirection);
			
			if (input.IsDown(CharacterInputData.JUMP))
			{
				Jump();
			}
			if (input.IsDown(CharacterInputData.USE))
			{
				Use();
			}
			if (input.IsDown(CharacterInputData.RELOAD))
			{
				Reload();
			}
		}
		/*if (GetInput(out CharacterInputData input))
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
		}*/
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
		meshRenderer.gameObject.SetActive(CharacterState == CharacterState.ACTIVE);
		/*collider.enabled = state != State.Dead;
		_hitBoxRoot.enabled = state == State.Active;
		_damageVisuals.CheckHealth(life);*/
	}
	#region State Change
	public static void OnStateChanged(Changed<Character> changed)
	{
		DebugLogMessage.Log($"[OnStateChanged({changed.Behaviour.CharacterState})]");
		changed.Behaviour.OnStateChanged();
	}
	
	public void OnStateChanged()
	{
		switch (CharacterState)
		{
			case CharacterState.NEW:
				Debug.Log($"[{playerInfo.Id}] Player {CharacterState}");
				break;
			case CharacterState.ACTIVATING:
				Debug.Log($"[{playerInfo.Id}] Player {CharacterState}");
				break;
			case CharacterState.ACTIVE:
				Debug.Log($"[{playerInfo.Id}] Player {CharacterState}");
				meshRenderer.gameObject.SetActive(false);
				break;
			case CharacterState.DEACTIVATING:
				Debug.Log($"[{playerInfo.Id}] Player {CharacterState}");
				break;
			case CharacterState.INACTIVE:
				break;
			default:
				DebugLogMessage.Log($"[{playerInfo.Id}] Player {CharacterState}");
				break;
		
		}
	}
	#endregion

	#region Character Movement
	
	public void Move()
	{
		if (CharacterState == CharacterState.ACTIVE)
		{
			networkCharacterController.Move(MoveDirection);
		}
	}
	
	public void Move(Vector3 direction)
	{
		MoveDirection = direction;
		if (CharacterState == CharacterState.ACTIVE)
		{
			networkCharacterController.Move(MoveDirection);
		}
	}
	
	public void LookAt(Vector3 direction)
	{
		LookDirection = direction;
		if (CharacterState == CharacterState.ACTIVE)
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
		/*else
		{
			MoveDirection += (TransformLocal ? transform.up : Vector3.up);
		}*/
	}
	#endregion

	#region Behaviour
	public void Use()
	{
		DebugLogMessage.Log(Color.white, $"{gameObject.name} TODO: IMPLEMENT USE");
	}
	public void Reload()
	{
		DebugLogMessage.Log(Color.white, $"{gameObject.name} TODO: IMPLEMENT RELOAD");
	}
	public void Kill()
	{
		if (Object.HasStateAuthority)
		{
			CharacterState = CharacterState.DEACTIVATING;
		}
		else
		{
			Rpc_Kill();
		}
	}
	
	[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
	public void Rpc_Kill()
	{
		CharacterState = CharacterState.DEACTIVATING;

	}
	#endregion
	#region INetworkRunnerCallbacks
	public void OnConnectedToServer(NetworkRunner runner) {}
	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {}
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {}
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	public void OnDisconnectedFromServer(NetworkRunner runner) {}
	
	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		if (CharacterInput != null)
		{
			input.Set(CharacterInput.GetInput());
		}
	}
	
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {}
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {}
	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {}
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) {}
	public void OnSceneLoadDone(NetworkRunner runner) {}
	public void OnSceneLoadStart(NetworkRunner runner) {}
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {}
	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {}
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message){}
	#endregion
}