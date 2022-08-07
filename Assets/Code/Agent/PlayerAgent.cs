using UnityEngine;
using Fusion.KCC;

namespace UnityFusionNetworking
{
    [RequireComponent(typeof(Health))]
    public class PlayerAgent : Agent
    {
        // PUBLIC MEMBERS
        public Player Owner { get; set; }

        //public Weapons Weapons { get; private set; }
        public Health Health { get; private set; }

        // PRIVATE MEMBERS

        /*[SerializeField] private Transform _cameraPivot;
        [SerializeField] private Transform _cameraHandle;*/
        [SerializeField] private GameObject _visual;

        [SerializeField] private float _maxCameraAngle = 75f;
        [SerializeField] private Vector3 _jumpImpulse = new(0f, 6f, 0f);
        [SerializeField] private Transform cameraHandle;
        [SerializeField] private LayerMask mouseLookMask;

        [SerializeField] private Vector3 aimOffset;
        // MONOBEHAVIOUR

        protected override void Awake()
        {
            base.Awake();

            //Weapons = GetComponent<Weapons>();
            Health = GetComponent<Health>();
        }

        // Agent INTERFACE

        protected override void OnSpawned()
        {
            name = Object.InputAuthority.ToString();
            
            if (Object.HasInputAuthority)
            {
                // Adjust Camera Handle Position and Rotation
                if (Context.Camera != null)
                {
                    DebugLogMessage.Log(Color.green, $"Camera Comp {Context.Camera.Context.ObservedPlayerRef}");
                    var offset = transform.position + Context.Camera.TransformOffset;
                    cameraHandle.transform.position = offset;
                    cameraHandle.transform.rotation = Quaternion.Euler(Context.Camera.RotationOffset);
                    Context.Camera.transform.position = cameraHandle.transform.position;
                    Context.Camera.transform.rotation = cameraHandle.transform.rotation;
                    
                    /*Context.Camera.transform.position = transform.position + Context.Camera.TransformOffset;
                    Context.Camera.transform.rotation = Quaternion.Euler(Context.Camera.RotationOffset);*/
                    
                }
            }
        }

        protected override void OnDespawned()
        {
            //Weapons.OnDespawned();

            Owner = null;
        }

        protected override void ProcessEarlyFixedInput()
        {
            if (Owner == null || Health.IsAlive == false)
                return;

            var input = Owner.Input.FixedInput;

            // Here we process input and set properties related to movement / look.
            // For following lines, we should use Input.FixedInput only. This property holds input for fixed updates.

            // Clamp input look rotation delta
            
            var lookRotation = KCC.FixedData.GetLookRotation(true, true);
            
            var lookRotationDelta = input.LookRotationDelta;
            //DebugLogMessage.Log(Color.yellow, $"Agent Fixed{lookRotation}");
            /*var lookRotationDelta = KCCUtility.GetClampedLookRotationDelta(lookRotation, input.LookRotationDelta,
                -_maxCameraAngle, _maxCameraAngle);*/
            KCC.SetLookRotation(lookRotationDelta);
            // Apply clamped look rotation delta
            //KCC.AddLookRotation(lookRotationDelta);

            // Calculate input direction based on recently updated look rotation (the change propagates internally also to KCCData.TransformRotation)
            var inputDirection = KCC.FixedData.TransformRotation * new Vector3(input.MoveDirection.x, 0.0f, input.MoveDirection.y);

            KCC.SetInputDirection(inputDirection);

            if (Owner.Input.WasPressed(EInputButton.JUMP) == true)
            {
                // By default the character jumps forward in facing direction
                var jumpRotation = KCC.FixedData.TransformRotation;

                if (inputDirection.IsAlmostZero() == false)
                    // If we are moving, jump in that direction instead
                    jumpRotation = Quaternion.LookRotation(inputDirection);

                // Applying jump impulse
                KCC.Jump(jumpRotation * _jumpImpulse);
            }

            // Another movement related actions here (crouch, ...)
        }

        protected override void OnFixedUpdate()
        {
            // Regular fixed update for Agent class.
            // Executed after all agent KCC updates and before HitboxManager.

            // Setting camera pivot rotation
            //var pitchRotation = KCC.FixedData.GetLookRotation(true, false);
            //_cameraPivot.localRotation = Quaternion.Euler(pitchRotation);
        }

        protected override void ProcessLateFixedInput()
        {
            // Executed after HitboxManager. Process other non-movement actions like shooting.

            if (Owner != null)
            {
                //Weapons.ProcessInput(Owner.Input);
            }
        }

        protected override void OnLateFixedUpdate()
        {
            //Weapons.OnLateFixedUpdate();
        }

        protected override void ProcessRenderInput()
        {
            /*if (Owner == null || Health.IsAlive == false)
                return;*/
            if (Owner == null)
                return;
            // Here we process input and set properties related to movement / look.
            // For following lines, we should use Input.RenderInput and Input.CachedInput only. These properties hold input for render updates.
            // Input.RenderInput holds input for current render frame.
            // Input.CachedInput holds combined input for all render frames from last fixed update. This property will be used to set input for next fixed update (polled by Fusion).

            var input = Owner.Input;

            // Get look rotation from last fixed update (not last render!)
            
            var ray = Context.Camera.CameraComponent.ScreenPointToRay(Input.mousePosition);
            ray.origin += aimOffset;
            // Raycast towards the mouse collider box in the world
            if (Physics.Raycast(ray, out var hit, 200.0f, mouseLookMask))
            {
                DebugLogMessage.Log(Color.yellow, $"Hit Something {Input.mousePosition} \n{hit.point}");
                
                if (hit.collider != null)
                {
                    var lookRotation = Quaternion.LookRotation(hit.point - transform.position).eulerAngles;
                    DebugLogMessage.Log(Color.yellow, $"GetMouseLook {lookRotation}");
                    if (lookRotation != Vector3.zero) // It already shouldn't be...
                    {
                        lookRotation.x = 0f;
                        lookRotation.z = 0f;
                        lookRotation += aimOffset;
                        DebugLogMessage.Log($"Ray Look {lookRotation}");
                    }
                }
                
            }
            //var lookRotation = input.RenderInput.LookRotationDelta; // Maybe Cache
            // For correct look rotation, we have to apply deltas from all render frames since last fixed update => stored in Input.CachedInput
            var lookRotationDelta = input.CachedInput.LookRotationDelta;

            //KCC.SetLookRotation(lookRotation + lookRotationDelta);
            //KCC.SetLookRotation(lookRotation);
            Vector3 inputDirection = default;

            // MoveDirection values from previous render frames are already consumed and applied by KCC, so we use Input.RenderInput (non-accumulated input for this frame)
            var moveDirection = input.RenderInput.MoveDirection.X0Y();
            if (moveDirection.IsZero() == false) inputDirection = KCC.RenderData.TransformRotation * moveDirection;

            KCC.SetInputDirection(inputDirection);

            // Jump is extrapolated for render as well.
            if (Owner.Input.WasPressed(EInputButton.JUMP) == true)
            {
                // By default the character jumps forward in facing direction
                var jumpRotation = KCC.RenderData.TransformRotation;

                if (inputDirection.IsZero() == false)
                    // If we are moving, jump in that direction instead
                    jumpRotation = Quaternion.LookRotation(inputDirection);

                KCC.Jump(jumpRotation * _jumpImpulse);
            }

            // At his point, KCC haven't been updated yet (except look rotation, which propagates immediately) so camera have to be synced later.
            // Because this is advanced implementation, base class triggers manual KCC update immediately after this method.
            // This allows us to synchronize camera in OnEarlyRender(). To keep consistency with fixed update, camera related properties are updated in regular render update - OnRender().
        }

        // 5.
        protected override void OnLateRender()
        {
            // Setting base camera transform based on handle

            // For render we care only about input authority.
            // This can be extended to state authority if needed (inner code won't be executed on host for other agents, having camera pivots to be set only from fixed update, causing jitter if spectating that player)
            if (Object.HasInputAuthority == true)
            {
                Context.Camera.transform.position = cameraHandle.position;
            }
  
            //Weapons.OnRender();
        }
    }
}