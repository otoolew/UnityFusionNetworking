using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.KCC;
using Fusion.Sockets;
using UnityEngine;

namespace UnityFusionNetworking
{
    public class PlayerInput : ContextBehaviour, IBeforeUpdate, IBeforeTick
    {
        private Player _player;
        
        /// <summary>
        /// Stops the collection of input
        /// </summary>
        [Networked] public NetworkBool InputBlocked { get; set; }

        /// <summary>
        /// Holds input for fixed update.
        /// </summary>
        public CharacterInput FixedInput => _fixedInput;

        /// <summary>
        /// Holds input for current frame render update.
        /// </summary>
        public CharacterInput RenderInput => _renderInput;

        /// <summary>
        /// Holds combined inputs from all render frames since last fixed update. Used when Fusion input poll is triggered.
        /// </summary>
        public CharacterInput CachedInput => _cachedInput;

        // PRIVATE MEMBERS

        // We need to store last known input to compare current input against (to track actions activation/deactivation). It is also used if an input for current frame is lost.
        // This is not needed on proxies, only input authority is registered to nameof(PlayerInput) interest group.
        [Networked(nameof(PlayerInput))]
        private CharacterInput _lastKnownInput { get; set; }

        private CharacterInput _fixedInput;
        private CharacterInput _renderInput;
        private CharacterInput _cachedInput;
        private CharacterInput _baseFixedInput;
        private CharacterInput _baseRenderInput;

        private Vector2 _cachedMoveDirection;
        private float _cachedMoveDirectionSize;
        private Quaternion _cachedLookRotation;
        private float _cachedLookRotationSize;
        private bool _resetCachedInput;


        [SerializeField] private LayerMask mouseLookMask;

        [SerializeField] private Vector3 aimOffset;
        // PUBLIC METHODS

        /// <summary>
        /// Check if the button is set in current input. FUN/Render input is resolved automatically.
        /// </summary>
        public bool IsSet(EInputButton button)
        {
            return Runner.Stage != default ? _fixedInput.Buttons.IsSet(button) : _renderInput.Buttons.IsSet(button);
        }

        /// <summary>
        /// Check if the button was pressed in current input.
        /// In FUN this method compares current fixed input agains previous fixed input.
        /// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FUN).
        /// </summary>
        public bool WasPressed(EInputButton button)
        {
            return Runner.Stage != default
                ? _fixedInput.Buttons.WasPressed(_baseFixedInput.Buttons, button)
                : _renderInput.Buttons.WasPressed(_baseRenderInput.Buttons, button);
        }

        /// <summary>
        /// Check if the button was released in current input.
        /// In FUN this method compares current fixed input agains previous fixed input.
        /// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FUN).
        /// </summary>
        public bool WasReleased(EInputButton button)
        {
            return Runner.Stage != default
                ? _fixedInput.Buttons.WasReleased(_baseFixedInput.Buttons, button)
                : _renderInput.Buttons.WasReleased(_baseRenderInput.Buttons, button);
        }

        public NetworkButtons GetPressedButtons()
        {
            return Runner.Stage != default
                ? _fixedInput.Buttons.GetPressed(_baseFixedInput.Buttons)
                : _renderInput.Buttons.GetPressed(_baseRenderInput.Buttons);
        }

        public NetworkButtons GetReleasedButtons()
        {
            return Runner.Stage != default
                ? _fixedInput.Buttons.GetReleased(_baseFixedInput.Buttons)
                : _renderInput.Buttons.GetReleased(_baseRenderInput.Buttons);
        }

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            // Reset to default state.
            _fixedInput = default;
            _renderInput = default;
            _cachedInput = default;
            _lastKnownInput = default;
            _baseFixedInput = default;
            _baseRenderInput = default;

            if (Object.HasStateAuthority == true)
                // Only state and input authority works with input and access _lastKnownInput.
                Object.SetInterestGroup(Object.InputAuthority, nameof(CharacterController), true);

            if (Runner.LocalPlayer == Object.InputAuthority)
            {
                var events = Runner.GetComponent<NetworkEvents>();
                events.OnInput.RemoveListener(OnInput);
                events.OnInput.AddListener(OnInput);

                Context.Input.RequestCursorLock();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            var events = Runner.GetComponent<NetworkEvents>();
            events.OnInput.RemoveListener(OnInput);

            if (Runner.LocalPlayer == Object.InputAuthority) Context.Input.RequestCursorRelease();
        }

        // MONOBEHAVIOUR

        protected void Awake()
        {
            _player = GetComponent<Player>();
        }

        // IBeforeUpdate INTERFACE

        /// <summary>
        /// 1. Collect input from devices, can be executed multiple times between FixedUpdateNetwork() calls because of faster rendering speed.
        /// </summary>
        void IBeforeUpdate.BeforeUpdate()
        {
            if (Object == null || Object.HasInputAuthority == false) return;

            // Store last render input as a base to current render input.
            _baseRenderInput = _renderInput;

            // Reset input for current frame to default
            _renderInput = default;

            // Cached input was polled and explicit reset requested
            if (_resetCachedInput == true)
            {
                _resetCachedInput = false;

                _cachedMoveDirection = default;
                _cachedMoveDirectionSize = default;
                _cachedInput = default;
            }

            // Input is tracked only if the cursor is locked and runner should provide input
            if (Runner.ProvideInput == false || Context.Input.IsLocked == false || InputBlocked == true) return;

            ProcessStandaloneInput();
        }

        // IBeforeTick INTERFACE

        /// <summary>
        /// 3. Read input from Fusion. On input authority the FixedInput will match CachedInput.
        /// We have to prepare fixed input before tick so it is ready when read from other objects (agents)
        /// </summary>
        void IBeforeTick.BeforeTick()
        {
            if (InputBlocked == true)
            {
                _fixedInput = default;
                _baseFixedInput = default;
                _lastKnownInput = default;
                return;
            }

            // Store last known fixed input. This will be compared against new fixed input
            _baseFixedInput = _lastKnownInput;

            // Set correct fixed input (in case of resimulation, there will be value from the future)
            _fixedInput = _lastKnownInput;

            if (GetInput<CharacterInput>(out var input) == true)
            {
                _fixedInput = input;

                // Update last known input. Will be used next tick as base and fallback
                _lastKnownInput = input;
            }
            else
            {
                // In case we do not get input, clear look rotation delta so player will not rotate but repeat other actions
                _fixedInput.LookRotationDelta = default;
            }

            // The current fixed input will be used as a base to first Render after FUN
            _baseRenderInput = _fixedInput;
        }

        // PRIVATE METHODS

        /// <summary>
        /// 2. Push cached input and reset properties, can be executed multiple times within single Unity frame if the rendering speed is slower than Fusion simulation (or there is a performance spike).
        /// </summary>
        private void OnInput(NetworkRunner runner, NetworkInput networkInput)
        {
            if (InputBlocked == true) return;

            var characterInput = _cachedInput;

            // Input is polled for single fixed update, but at this time we don't know how many times in a row OnInput() will be executed.
            // This is the reason for having a reset flag instead of resetting input immediately, otherwise we could lose input for next fixed updates (for example move direction).

            _resetCachedInput = true;

            // Now we reset all properties which should not propagate into next OnInput() call (for example LookRotationDelta - this must be applied only once and reset immediately).
            // If there's a spike, OnInput() and OnFixedUpdate() will be called multiple times in a row without OnBeforeUpdate() in between, so we don't reset move direction to preserve movement.
            // Instead, move direction and other sensitive properties are reset in next OnBeforeUpdate() - driven by _resetCachedInput.

            _cachedInput.LookRotationDelta = default;

            // Input consumed by OnInput() call will be read in OnFixedUpdate() and immediately propagated to KCC.
            // Here we should reset render properties so they are not applied twice (fixed + render update).

            _renderInput.LookRotationDelta = default;

            networkInput.Set(characterInput);
        }

        private void ProcessStandaloneInput()
        {
            // Calc Move Direction
            Vector2 moveDirection = Vector2.zero;

            if (Input.GetKey(KeyCode.W) == true) moveDirection += Vector2.up;

            if (Input.GetKey(KeyCode.S) == true) moveDirection += Vector2.down;

            if (Input.GetKey(KeyCode.A) == true) moveDirection += Vector2.left;

            if (Input.GetKey(KeyCode.D) == true) moveDirection += Vector2.right;

            if(moveDirection.IsZero() == false) moveDirection.Normalize();
            
            DebugLogMessage.Log(Color.yellow, $"moveDirection {moveDirection}");
            // Set
            _renderInput.MoveDirection = moveDirection;

            // Calc Look Rotation
            /*Vector2 mouseVec = new Vector2(Input.mousePosition.x / Screen.width - 0.5f,
                Input.mousePosition.y / Screen.height - 0.5f);
            DebugLogMessage.Log($"Mouse Vector {mouseVec}");
            
            Vector3 lookRotation = Vector3.zero;
            // Raycast towards the mouse collider box in the world
            var ray = Context.Camera.CameraComponent.ScreenPointToRay(Input.mousePosition);
            ray.origin += aimOffset;
            DebugLogMessage.Log(Color.green, $"Ray {ray.GetPoint(100).ToString()}");
            // Raycast towards the mouse collider box in the world
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, mouseLookMask))
            {
                DebugLogMessage.Log(Color.red, $"HIT {lookRotation}");
                if (hit.collider != null)
                {
                    DebugLogMessage.Log(Color.red, $"hit {lookRotation}");
                    Quaternion lookDirection = Quaternion.LookRotation(hit.point - transform.position);
                    if (lookDirection.eulerAngles != Vector3.zero) // It already shouldn't be...
                    {
                        lookDirection.x = 0f;
                        lookDirection.z = 0f;
                        lookRotation = lookDirection.eulerAngles += aimOffset;
                        if(lookRotation.IsZero() == false) lookRotation.Normalize();
                        _cachedLookRotation = lookRotation;
                    }
                }
            }*/
            
            Quaternion lookRotation = Quaternion.identity;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            ray.origin += aimOffset;
            // Raycast towards the mouse collider box in the world
            if (Physics.Raycast(ray, out var hit, 200.0f, mouseLookMask))
            {
                if (hit.collider != null)
                {
                    lookRotation = Quaternion.LookRotation(hit.point - transform.position);
                    if (lookRotation.eulerAngles != Vector3.zero) // It already shouldn't be...
                    {
                        lookRotation.x = 0f;
                        lookRotation.z = 0f;
                        lookRotation.eulerAngles += aimOffset;
                        if(lookRotation.IsZero() == false) lookRotation.Normalize();
                        _renderInput.LookRotation = lookRotation;
                    }
                }
            }
            else
            {
                DebugLogMessage.Log(Color.magenta, $"Miss  {ray.GetPoint(20).ToString()}");
                lookRotation = Quaternion.LookRotation(ray.GetPoint(20) - _player.ActiveAgent.transform.position);
                if (lookRotation.eulerAngles != Vector3.zero) // It already shouldn't be...
                {
                    lookRotation.x = 0f;
                    lookRotation.z = 0f;
                    lookRotation.eulerAngles += aimOffset;
                    if(lookRotation.IsZero() == false) lookRotation.Normalize();
                    _renderInput.LookRotation = lookRotation;
                }
            }
            _renderInput.LookRotation = lookRotation;
            DebugLogMessage.Log(Color.yellow, $"lookRotation {lookRotation}");
            //var lookRotationDelta = GetMouseLookDirection();
            /*Vector2 mouseVec = new Vector2(Input.mousePosition.x / Screen.width - 0.5f, Input.mousePosition.y / Screen.height - 0.5f);
            _renderInput.Yaw = Mathf.Atan2(mouseVec.y, mouseVec.x) * Mathf.Rad2Deg;*/
            //var screenPoint = Context.Camera.CameraComponent.WorldToScreenPoint(worldPosition);
            //DebugLogMessage.Log(Color.yellow, $"PlayerInput Look Rotation {lookRotationDelta}");
            
            // Set
            //_renderInput.LookRotationDelta = lookRotation;

            DebugLogMessage.Log(Color.yellow, $"lookRotation {lookRotation}");
     
            
            _renderInput.Buttons.Set(EInputButton.USE, Input.GetKeyDown(KeyCode.E));
            _renderInput.Buttons.Set(EInputButton.FIRE, Input.GetMouseButton(0));
            _renderInput.Buttons.Set(EInputButton.FIRE_ALT, Input.GetMouseButton(1));
            _renderInput.Buttons.Set(EInputButton.RELOAD, Input.GetKey(KeyCode.R));
            
            _renderInput.Buttons.Set(EInputButton.CROUCH, Input.GetKeyDown(KeyCode.LeftControl));
            _renderInput.Buttons.Set(EInputButton.SPRINT, Input.GetKeyDown(KeyCode.LeftShift));
            _renderInput.Buttons.Set(EInputButton.JUMP, Input.GetKey(KeyCode.Space));
            
            _renderInput.Buttons.Set(EInputButton.ACTION_01, Input.GetKey(KeyCode.Z));
            _renderInput.Buttons.Set(EInputButton.ACTION_02, Input.GetKey(KeyCode.X));
            _renderInput.Buttons.Set(EInputButton.ACTION_03, Input.GetKey(KeyCode.C));
            _renderInput.Buttons.Set(EInputButton.ACTION_04, Input.GetKey(KeyCode.V));

            for (var i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha9; i++)
            {
                if (Input.GetKey((KeyCode)i) == true)
                {
                    _renderInput.WeaponButton = (byte)(i - (int)KeyCode.Alpha0 + 1);
                    break;
                }
            }

            //if (_renderInput.WeaponButton == 0) _renderInput.WeaponButton = GetScrollWeaponButton();

            // Process cached input for next OnInput() call, represents accumulated inputs for all render frames since last fixed update.

            var deltaTime = Time.deltaTime;

            // Move direction accumulation is a special case. Let's say simulation runs 30Hz (33.333ms delta time) and render runs 300Hz (3.333ms delta time).
            // If the player hits W key in last frame before fixed update, the KCC will move in render update by (velocity * 0.003333f).
            // Treating this input the same way for next fixed update results in KCC moving by (velocity * 0.03333f) - 10x more.
            // Following accumulation proportionally scales move direction so it reflects frames in which input was active.
            // This way the next fixed update will correspond more accurately to what happened in render frames.

            _cachedMoveDirection += moveDirection * deltaTime;
            _cachedMoveDirectionSize += deltaTime;
            _cachedInput.MoveDirection = _cachedMoveDirection / _cachedMoveDirectionSize;
            
            //_cachedLookRotation += lookRotation.eulerAngles * deltaTime;
            _cachedLookRotationSize += deltaTime;
            //_cachedInput.LookRotationDelta = _cachedLookRotation / _cachedLookRotationSize;
            //_cachedInput.LookRotation = _cachedLookRotation / _cachedLookRotationSize;
            
            
            _cachedInput.Buttons = new NetworkButtons(_cachedInput.Buttons.Bits | _renderInput.Buttons.Bits);
            _cachedInput.WeaponButton = _renderInput.WeaponButton != 0 ? _renderInput.WeaponButton : _cachedInput.WeaponButton;
        }

        /*private byte GetScrollWeaponButton()
        {
            var weapons = _player.ActiveAgent != null ? _player.ActiveAgent.Weapons : null;

            if (weapons == null || weapons.Object == null) return 0;

            var wheelAxis = Input.GetAxis("Mouse ScrollWheel");

            if (wheelAxis == 0f) return 0;

            byte weaponButton = 0;

            if (wheelAxis > 0f)
                weaponButton = (byte)(weapons.GetNextWeaponSlot(weapons.PendingWeaponSlot, true) + 1);
            else if (wheelAxis < 0f)
                weaponButton = (byte)(weapons.GetPreviousWeaponSlot(weapons.PendingWeaponSlot, true) + 1);

            return weaponButton;
        }*/

        /*private void MovementLook()
        {
            if (input.IsDown(NetworkInputPrototype.BUTTON_FORWARD))
            {
            direction += TransformLocal ? transform.forward : Vector3.forward;
            }

            if (input.IsDown(NetworkInputPrototype.BUTTON_BACKWARD))
            {
            direction -= TransformLocal ? transform.forward : Vector3.forward;
            }

            if (input.IsDown(NetworkInputPrototype.BUTTON_LEFT))
            {
            direction -= TransformLocal ? transform.right : Vector3.right;
            }

            if (input.IsDown(NetworkInputPrototype.BUTTON_RIGHT))
            {
            direction += TransformLocal ? transform.right : Vector3.right;
            }

            direction = direction.normalized;
        }*/
        private Vector3 GetMouseLookDirection()
        {

            /*Vector2 mouseVec = new Vector2(Input.mousePosition.x / Screen.width - 0.5f,
                Input.mousePosition.y / Screen.height - 0.5f);
            DebugLogMessage.Log($"Mouse Vector {mouseVec}");
            
            
            DebugLogMessage.Log($"Yaw {_renderInput.Yaw}");*/
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            ray.origin += aimOffset;
            // Raycast towards the mouse collider box in the world
            if (Physics.Raycast(ray, out var hit, 200.0f, mouseLookMask))
            {
                if (hit.collider != null)
                {
                    var lookRotation = Quaternion.LookRotation(hit.point - transform.position);
                    if (lookRotation.eulerAngles != Vector3.zero) // It already shouldn't be...
                    {
                        lookRotation.x = 0f;
                        lookRotation.z = 0f;
                        lookRotation.eulerAngles += aimOffset;
                        return lookRotation.eulerAngles;
                    }
                }
            }
            
            /*_renderInput.Yaw = Mathf.Atan2(mouseVec.y, mouseVec.x) * Mathf.Rad2Deg;
            
            Vector3 direction = new Vector3(
                Mathf.Cos((float)_renderInput.Yaw * Mathf.Deg2Rad),
                0,
                Mathf.Sin((float)_renderInput.Yaw * Mathf.Deg2Rad)
            );*/
            
            
                //input.Yaw = Mathf.Atan2(mouseVec.y, mouseVec.x) * Mathf.Rad2Deg;
            /*if (Input.GetMouseButton(0) && EventSystem.current.IsPointerOverGameObject() == false)
            {
                frameworkInput.Buttons.Set(NetworkInputPrototype.BUTTON_WALK, true);

                Vector2 mouseVec = new Vector2(Input.mousePosition.x / Screen.width - 0.5f, Input.mousePosition.y / Screen.height - 0.5f);
                frameworkInput.Yaw = Mathf.Atan2(mouseVec.y, mouseVec.x) * Mathf.Rad2Deg;
            }
            
            Vector3 direction = new Vector3(
                Mathf.Cos((float)input.Yaw * Mathf.Deg2Rad),
                0,
                Mathf.Sin((float)input.Yaw * Mathf.Deg2Rad)
            );*/
            
            
            /*if (Camera.main != null)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                ray.origin += aimOffset;
                // Raycast towards the mouse collider box in the world
                if (Physics.Raycast(ray, out var hit, 200.0f, mouseLookMask))
                {
                    if (hit.collider != null)
                    {
                        var lookRotation = Quaternion.LookRotation(hit.point - transform.position);
                        if (lookRotation.eulerAngles != Vector3.zero) // It already shouldn't be...
                        {
                            lookRotation.x = 0f;
                            lookRotation.z = 0f;
                            lookRotation.eulerAngles += aimOffset;
                            return lookRotation.eulerAngles;
                        }
                    }
                }
            }*/
            return Vector3.zero;
        }
        

        #region INetworkRunnerCallbacks

        public void OnConnectedToServer(NetworkRunner runner)
        {
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        public void OnDisconnectedFromServer(NetworkRunner runner)
        {
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
        {
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        #endregion

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(_player.ActiveAgent.transform.position + Vector3.up,_renderInput.LookRotation.eulerAngles + Vector3.forward * 100);
  
        }
    }
}
