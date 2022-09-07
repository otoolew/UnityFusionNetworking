using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handle player input by responding to Fusion input polling, filling an input struct and then working with
/// that input struct in the Fusion Simulation loop. Players should have Input Control over this.
/// </summary>
public class CharacterInput : NetworkBehaviour, INetworkRunnerCallbacks
{
    private CharacterInputData playerInput;

    [SerializeField] private LayerMask mouseLookMask;
    [SerializeField] private float heightOffset;
    [SerializeField] private bool inputEnabled;
    public bool InputEnabled { get => inputEnabled; set => inputEnabled = value; }
    
    [SerializeField] private PlayerCharacter character;
    public PlayerCharacter Character { get => character; set => character = value; }

    [SerializeField] private Vector3 moveDirectionInput;
    [SerializeField] private Vector3 mouseLookDirection;

    #region MonoBehaviour Callbacks
    private void Awake()
    {
        CacheComponents();
    }
    #endregion

    private void CacheComponents()
    {
        if (!character) character = GetComponent<PlayerCharacter>();
    }
    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Runner.AddCallbacks(this);
        }
    }
    private void Update()
    {
        moveDirectionInput = GetMoveDirection();
        mouseLookDirection = GetMouseLookDirection();

        playerInput.Buttons |= Input.GetKeyDown(KeyCode.E) ? InputButton.USE : 0;
        playerInput.Buttons |= Input.GetMouseButtonDown(0) ? InputButton.FIRE : 0;
        playerInput.Buttons |= Input.GetMouseButton(1) ? InputButton.FIRE_ALT : 0;
        playerInput.Buttons |= Input.GetKeyDown(KeyCode.R) ? InputButton.RELOAD : 0;
        playerInput.Buttons |= Input.GetKeyDown(KeyCode.Space) ? InputButton.JUMP : 0;
        playerInput.Buttons |= Input.GetKeyDown(KeyCode.LeftControl) ? InputButton.CROUCH : 0;
        playerInput.Buttons |= Input.GetKeyDown(KeyCode.LeftShift) ? InputButton.SPRINT : 0;
        
        playerInput.Buttons |= Input.GetKeyDown(KeyCode.LeftShift) ? InputButton.ACTION_01 : 0;
        playerInput.Buttons |= Input.GetKeyDown(KeyCode.LeftShift) ? InputButton.ACTION_02 : 0;
        playerInput.Buttons |= Input.GetKeyDown(KeyCode.LeftShift) ? InputButton.ACTION_03 : 0;
        playerInput.Buttons |= Input.GetKeyDown(KeyCode.LeftShift) ? InputButton.ACTION_04 : 0;
    }

    public override void FixedUpdateNetwork()
    {
        
        if(!inputEnabled)
            return;

        if (GetInput(out CharacterInputData input))
        {
            Character.Move(input.MoveDirection);
            Character.LookAt(input.AimDirection);
            
            if (input.GetButton(InputButton.FIRE))
            {
                Character.AbilityController.FireWeapon();
                //DebugLogMessage.Log(Color.white,$"Firing {character.PlayerInfo.DisplayName}");
            }
            if (input.GetButton(InputButton.RELOAD)) //TODO: Used as a quick means of debugging impulse hits from projectiles
            {
                Character.ApplyImpulse(new Vector3(-10,0,0));
            }
        }
    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(GetInput());
    }
    public CharacterInputData GetInput()
    {
        CharacterInputData input = new CharacterInputData();
        if (character != null && character.Object != null && character.CharacterState == CharacterState.ACTIVE)
        {
            input.MoveDirection = moveDirectionInput;
            input.AimDirection = mouseLookDirection;

            if (Input.GetMouseButton(0))
            {
                input.Buttons |= InputButton.FIRE;
            }

            if (Input.GetMouseButton(1))
            {
                input.Buttons |= InputButton.FIRE_ALT;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                input.Buttons |= InputButton.SPRINT;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                input.Buttons |= InputButton.CROUCH;
            }

            if (Input.GetKey(KeyCode.E))
            {
                input.Buttons |= InputButton.USE;
            }

            if (Input.GetKey(KeyCode.R))
            {
                input.Buttons |= InputButton.RELOAD;
            }

            if (Input.GetKey(KeyCode.Space))
            {
                input.Buttons |= InputButton.JUMP;
            }
        }
        return input;
    }
    private Vector3 GetMoveDirection()
    {
        moveDirectionInput = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDirectionInput += Vector3.forward;
        }

        if (Input.GetKey(KeyCode.S))
        {
            moveDirectionInput -= Vector3.forward;
        }

        if (Input.GetKey(KeyCode.A))
        {
            moveDirectionInput -= Vector3.right;
        }

        if (Input.GetKey(KeyCode.D))
        {
            moveDirectionInput += Vector3.right;
        }
        
        return moveDirectionInput.normalized;
    }
    private Vector3 GetMouseLookDirection()
    {
        mouseLookDirection = Vector3.zero;
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            ray.origin += new Vector3(0, heightOffset, 0);
            // Raycast towards the mouse collider box in the world
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, mouseLookMask))
            {
                if (hit.collider != null)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(hit.point - character.WorldPosition);
                    lookRotation.x = 0f;
                    lookRotation.z = 0f;
                    
                    mouseLookDirection = lookRotation.eulerAngles;
                    return mouseLookDirection;
                }
            }
        }

        return mouseLookDirection;
    }

    #region INetworkRunnerCallbacks
    public void OnConnectedToServer(NetworkRunner runner) {}
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {}
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {}
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {}
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {}
    public void OnDisconnectedFromServer(NetworkRunner runner) {}
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