using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a Player - the Character is instantiated by the map once it's loaded.
/// This class handles camera tracking and player movement and is destroyed when the map is unloaded.
/// (I.e. the player gets a new avatar in each map)
/// </summary>
[RequireComponent(typeof(NetworkCharacterControllerPrototype), typeof(CharacterInput))]
public class PlayerCharacter : NetworkBehaviour, IDamageable
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
	
	[SerializeField] private AbilityController abilityController;
	public AbilityController AbilityController { get => abilityController; set => abilityController = value; }
	#endregion

	[SerializeField] private int health;
	public int Health { get => health; set => health = value; }
	
	
	#region Networked Properties
	[Networked]public CharacterState CharacterState { get; set; }
	[Networked]public Vector3 MoveDirection { get; set; }
	[Networked]public Vector3 LookDirection { get; set; }
	
	[Networked]public int HealthValue { get; set; }
    
	[SerializeField] private int healthMax;
	public int HealthMax { get => healthMax; set => healthMax = value; }
    
	[SerializeField] private Text healthText;
	public Text HealthText { get => healthText; set => healthText = value; }
	
	#endregion
	
	public float Speed = 6f;
	bool HasNCC => GetComponent<NetworkCharacterControllerPrototype>();
	bool ShowSpeed => this && !TryGetComponent<NetworkCharacterControllerPrototype>(out _);
	public Vector3 Velocity => networkCharacterController.Velocity;
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
		DebugLogMessage.Log(Color.yellow, $"CharacterSpawned Fusion Event -> Player [{player.PlayerId}]");
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
			PlayerCamera = Camera.main.transform.GetComponent<PlayerCamera>();
			if (PlayerCamera != null)
			{
				PlayerCamera.AssignFollowTarget(transform);
			}

			characterInput.InputEnabled = false;
		}
	}
	
	public override void FixedUpdateNetwork()
	{

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
	public static void OnStateChanged(Changed<PlayerCharacter> changed)
	{
		DebugLogMessage.Log($"[OnStateChanged({changed.Behaviour.CharacterState})]");
		changed.Behaviour.OnStateChanged();
	}
	
	public void OnStateChanged()
	{
		Debug.Log($"[{playerInfo.Id}] Player {CharacterState}");
		switch (CharacterState)
		{
			case CharacterState.NEW:
				Debug.Log($"[{playerInfo.Id}] Player {CharacterState}");
				break;
			case CharacterState.ACTIVATING:
				Debug.Log($"[{playerInfo.Id}] Player {CharacterState}");
				break;
			case CharacterState.ACTIVE:
				characterInput.InputEnabled = true;
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

	public void Move(Vector3 direction)
	{
		if (CharacterState == CharacterState.ACTIVE)
		{
			networkCharacterController.Move(direction);
		}
	}

	public void LookAt(Vector3 direction)
	{
		if (CharacterState == CharacterState.ACTIVE)
		{
			if (direction.sqrMagnitude > 0)
			{
				networkCharacterController.transform.rotation = Quaternion.Euler(direction);
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
	public void ApplyImpulse(Vector3 impulse)
	{
		if (Object.HasStateAuthority)
		{
			networkCharacterController.Velocity += impulse / 10.0f; // Magic constant to compensate for not properly dealing with masses
			networkCharacterController.Move(Vector3.zero);
		}
	}
	#endregion 
	#region Ability
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
	
	#region Health
	public void TakeDamage(int damageValue)
	{
		if (Object.HasStateAuthority)
		{
			health -= damageValue;
			DebugLogMessage.Log(Color.red, $"{gameObject.name} was hit!");
		}
	}
	public void TakeDamage(int damageValue, Vector3 impulse)
	{
		if (Object.HasStateAuthority)
		{
			health -= damageValue;
			ApplyImpulse(impulse);
		}
	}
	#endregion
}