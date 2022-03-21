using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handle player input by responding to Fusion input polling, filling an input struct and then working with
/// that input struct in the Fusion Simulation loop.
/// </summary>
public class CharacterInput : NetworkBehaviour, INetworkRunnerCallbacks
{
	public static bool FetchInput { get; set; } = true;
	[SerializeField] private Character character;
	[SerializeField] private LayerMask mouseLookMask;
	[SerializeField] private bool transformLocal;
	[SerializeField] private Vector3 moveDirection;
	public Vector3 MoveDirection { get => moveDirection; set => moveDirection = value; }
	
	[SerializeField] private Vector3 lookDirection;
	public Vector3 LookDirection { get => lookDirection; set => lookDirection = value; }
	

	#region Monobehaviour

	private void Awake()
	{
		CacheComponents();
	}
	private void CacheComponents() 
	{
		if (!character) character = GetComponent<Character>();
	}
	#endregion
	
	#region NetworkBehaviour
    /// <summary>
    /// Hook up to the Fusion callbacks so we can handle the input polling
    /// </summary>
    public override void Spawned()
    {
	    CacheComponents();
	    // Technically, it does not really matter which InputController fills the input structure, since the actual data will only be sent to the one that does have authority,
	    // but in the name of clarity, let's make sure we give input control to the gameobject that also has Input authority.
	    if (Object.HasInputAuthority)
	    {
		    Runner.AddCallbacks(this);
	    }
	    Debug.Log("Spawned [" + this + "] IsClient=" + Runner.IsClient + " IsServer=" + Runner.IsServer + " HasInputAuth=" + Object.HasInputAuthority + " HasStateAuth=" + Object.HasStateAuthority);
    }
    
    /*/// <summary>
    /// FixedUpdateNetwork is the main Fusion simulation callback - this is where
    /// we modify network state.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
	    if (Runner.Config.PhysicsEngine == NetworkProjectConfig.PhysicsEngines.None) {
		    return;
	    }
	    
	    character.Move(CalculateMoveDirection());
	    
	    character.LookAt(LookAtMouseDirection());
	    
    }*/

    #endregion
    
    /// <summary>
    /// Get Unity input and store them in a struct for Fusion
    /// </summary>
    /// <param name="runner">The current NetworkRunner</param>
    /// <param name="input">The target input handler that we'll pass our data to</param>
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    	var characterInput = new CharacterInputData();

    	if (character!=null && character.Object!=null && character.CurrentState == Character.State.ACTIVE && FetchInput)
    	{
	        // Instantiate our custom input structure
    		// Fill it with input data
    		if (Input.GetKey(KeyCode.W))
    		{
    			characterInput.Buttons |= CharacterInputData.FORWARD;
    		}

    		if (Input.GetKey(KeyCode.S))
    		{
    			characterInput.Buttons |= CharacterInputData.BACKWARD;
    		}

    		if (Input.GetKey(KeyCode.A))
    		{
    			characterInput.Buttons |= CharacterInputData.LEFT;
    		}

    		if (Input.GetKey(KeyCode.D))
    		{
    			characterInput.Buttons |= CharacterInputData.RIGHT;
    		}

    		if (Input.GetMouseButton(0))
    		{
    			characterInput.Buttons |= CharacterInputData.FIRE;
    		}

    		if (Input.GetMouseButton(1))
    		{
    			characterInput.Buttons |= CharacterInputData.FIRE_ALT;
    		}

            //characterInput.MoveDirection = CalculateMoveDirection();
    		characterInput.AimDirection = LookAtMouseDirection();
    	}
        // Hand over the data to Fusion
    	input.Set(characterInput);
	}
 
    public Vector3 CalculateMoveDirection()
    {
	    if (GetInput(out CharacterInputData input))
	    {
		    Vector3 direction = default;

		    if (input.IsDown(CharacterInputData.FORWARD))
		    {
			    direction += transformLocal ? transform.forward : Vector3.forward;
		    }

		    if (input.IsDown(CharacterInputData.BACKWARD))
		    {
			    direction -= transformLocal ? transform.forward : Vector3.forward;
		    }

		    if (input.IsDown(CharacterInputData.LEFT))
		    {
			    direction -= transformLocal ? transform.right : Vector3.right;
		    }

		    if (input.IsDown(CharacterInputData.RIGHT))
		    {
			    direction += transformLocal ? transform.right : Vector3.right;
		    }

		    direction = direction.normalized;

		    MoveDirection = direction;
		    return MoveDirection;
	    }
	    else
	    {
		    return MoveDirection;
	    }
    }
    private Vector3 LookAtMouseDirection()
    {
	    Vector3 mousePos = Input.mousePosition;

	    RaycastHit hit;
	    Ray ray = Camera.main.ScreenPointToRay(mousePos);

	    Vector3 mouseCollisionPoint = Vector3.zero;
	    // Raycast towards the mouse collider box in the world
	    if (Physics.Raycast(ray, out hit, Mathf.Infinity, mouseLookMask))
	    {
		    if (hit.collider != null)
		    {
			    mouseCollisionPoint = hit.point;
		    }
	    }

	    Vector3 aimDirection = mouseCollisionPoint - transform.position;
	    aimDirection.y = 0;
	    aimDirection.Normalize();
	    return aimDirection;
    }

    #region INetworkRunnerCallbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    #endregion

}

/// <summary>
/// Our custom definition of an INetworkStruct. Keep in mind that
/// * bool does not work (C# does not define a consistent size on different platforms)
/// * Must be a top-level struct (cannot be a nested class)
/// * Stick to primitive types and structs
/// * Size is not an issue since only modified data is serialized, but things that change often should be compact (e.g. button states)
/// </summary>
public struct CharacterInputData : INetworkInput
{
	public const uint USE = 1 << 0;
	public const uint FIRE = 1 << 1;
	public const uint FIRE_ALT = 1 << 2;

	public const uint FORWARD = 1 << 3;
	public const uint BACKWARD = 1 << 4;
	public const uint LEFT = 1 << 5;
	public const uint RIGHT = 1 << 6;

	public const uint JUMP = 1 << 7;
	public const uint CROUCH = 1 << 8;
	public const uint SPRINT  = 1 << 9;

	public const uint ACTION_01 = 1 << 10;
	public const uint ACTION_02 = 1 << 11;
	public const uint ACTION_03 = 1 << 12;
	public const uint ACTION_04 = 1 << 13;

	public const uint BUTTON_RELOAD = 1 << 14;
	
	public Vector3 MoveDirection;
	public Vector3 AimDirection;
	public Angle Yaw;
	public Angle Pitch;
	
	public uint Buttons;
	public byte Weapon;
	public bool IsUp(uint button) 
	{
		return IsDown(button) == false;
	}

	public bool IsDown(uint button) 
	{
		return (Buttons & button) == button;
	}
}