using Fusion;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a Player - the Character is instantiated by the map once it's loaded.
/// This class handles camera tracking and player movement and is destroyed when the map is unloaded.
/// (I.e. the player gets a new avatar in each map)
/// </summary>

[RequireComponent(typeof(NetworkCharacterController))]
public class Character : NetworkBehaviour
{
	[SerializeField] private NetworkCharacterController networkCharacterController;
	
	[SerializeField] private Text _name;
	[SerializeField] private MeshRenderer _mesh;

	private Transform _camera;
	private Player _player;

	[SerializeField] private Vector3 moveDirection;
	[SerializeField] private Vector3 aimDirection;
	
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
		_player = GameManager.Instance.GetPlayer(Object.InputAuthority);
		_name.text = _player.Name;
		_mesh.material.color = _player.Color;
		
		if (Object.HasInputAuthority)
		{
			if (_camera == null)
			{
				_camera = Camera.main.transform;
			}

			Transform t = gameObject.transform;
			Vector3 p = t.position;
			_camera.position = p - 10 * t.forward + 10 * Vector3.up;
			_camera.LookAt(p + 2 * Vector3.up);
		}
	}

	public override void Render()
	{
		if (Object.HasInputAuthority)
		{
			if (_camera == null)
				_camera = Camera.main.transform;
			
			Transform t = gameObject.transform;
			Vector3 p = t.position;
			_camera.position = p - 10 * t.forward + 10 * Vector3.up;
			_camera.LookAt(p + 2 * Vector3.up);
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
				Debug.Log($"[{_player.Id}] Player {CurrentState}");
				// TODO Play Spawning Effect
				break;
			case State.ACTIVE:
				Debug.Log($"[{_player.Id}] Player {CurrentState}");
				// Do any clean up here
				// TODO Stop Spawning Effect
				break;
			case State.DEAD:
				Debug.Log($"[{_player.Id}] Player {CurrentState}");
				// TODO Spawn Dead Body Here
				break;
			case State.DESPAWNING:
				Debug.Log($"[{_player.Id}] Player {CurrentState}");
				// TODO Play Despawning Effect
				break;
		}
	}
	#endregion

	#region Character Movement
	/// <summary>
	/// Set the direction of movement and aim
	/// </summary>
	public void SetDirections(Vector3 moveDirection, Vector3 aimDirection)
	{
		this.moveDirection = moveDirection;
		this.aimDirection = aimDirection;
	}

	public void Move()
	{
		if (CurrentState == State.ACTIVE)
		{
			networkCharacterController.Move(moveDirection);
		}
	}
	

	#endregion

}