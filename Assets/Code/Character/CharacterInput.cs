using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Handle player input by responding to Fusion input polling, filling an input struct and then working with
/// that input struct in the Fusion Simulation loop. Players should have Input Control over this.
/// </summary>
public class CharacterInput : MonoBehaviour
{
    public static bool FetchInput { get; set; } = true;
    [SerializeField] private Character character;
    [SerializeField] private LayerMask mouseLookMask;
    [SerializeField] private bool transformLocal;
    [SerializeField] private Vector3 aimOffset;
    
    #region MonoBehaviour Callbacks
    private void Awake()
    {
        CacheComponents();
    }
    #endregion

    private void CacheComponents()
    {
        if (!character) character = GetComponent<Character>();
    }
    
    public CharacterInputData GetInput()
    {
        CharacterInputData input = new CharacterInputData();
        if (character != null && character.Object != null && character.CharacterState == CharacterState.ACTIVE &&
            FetchInput)
        {
            input.AimDirection = GetMouseLookDirection();

            if (Input.GetMouseButton(0))
            {
                input.Buttons |= CharacterInputData.FIRE;
            }

            if (Input.GetMouseButton(1))
            {
                input.Buttons |= CharacterInputData.FIRE_ALT;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                input.Buttons |= CharacterInputData.SPRINT;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                input.Buttons |= CharacterInputData.CROUCH;
            }

            if (Input.GetKey(KeyCode.E))
            {
                input.Buttons |= CharacterInputData.USE;
            }

            if (Input.GetKey(KeyCode.R))
            {
                input.Buttons |= CharacterInputData.RELOAD;
            }

            if (Input.GetKey(KeyCode.Space))
            {
                input.Buttons |= CharacterInputData.JUMP;
            }
        }
        return input;
    }
        
    public bool TryGetCharacterInput(out CharacterInputData input)
    {
        input = new CharacterInputData();
        if (character != null && character.Object != null && character.CharacterState == CharacterState.ACTIVE && FetchInput)
        {
            input.AimDirection = GetMouseLookDirection();
        
            if (Input.GetMouseButton(0))
            {
                input.Buttons |= CharacterInputData.FIRE;
            }

            if (Input.GetMouseButton(1))
            {
                input.Buttons |= CharacterInputData.FIRE_ALT;
            }
            
            if (Input.GetKey(KeyCode.LeftControl))
            {
                input.Buttons |= CharacterInputData.SPRINT;
            }
            
            if (Input.GetKey(KeyCode.LeftShift))
            {
                input.Buttons |= CharacterInputData.CROUCH;
            }
            
            if (Input.GetKey(KeyCode.E))
            {
                input.Buttons |= CharacterInputData.USE;
            }
            
            if (Input.GetKey(KeyCode.R))
            {
                input.Buttons |= CharacterInputData.RELOAD;
            }
            
            if (Input.GetKey(KeyCode.Space))
            {
                input.Buttons |= CharacterInputData.JUMP;
            }
            
            return true;
        }
        return false;
    }
    
    private Vector3 GetMovementVector()
    {
        Vector3 direction = default;

        if (Input.GetKey(KeyCode.W))
        {
            direction += transformLocal ? transform.forward : Vector3.forward;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction -= transformLocal ? transform.forward : Vector3.forward;
        }

        if (Input.GetKey(KeyCode.A))
        {
            direction -= transformLocal ? transform.right : Vector3.right;
        }

        if (Input.GetKey(KeyCode.D))
        {
            direction += transformLocal ? transform.right : Vector3.right;
        }
        direction = direction.normalized;
        
        return direction;
    }
    private Vector3 GetMovementVector(Component character)
    {
        Vector3 direction = default;

        if (Input.GetKey(KeyCode.W))
        {
            direction += transformLocal ? character.transform.forward : Vector3.forward;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction -= transformLocal ? character.transform.forward : Vector3.forward;
        }

        if (Input.GetKey(KeyCode.A))
        {
            direction -= transformLocal ? character.transform.right : Vector3.right;
        }

        if (Input.GetKey(KeyCode.D))
        {
            direction += transformLocal ? character.transform.right : Vector3.right;
        }
        direction = direction.normalized;
        
        return direction;
    }
    private Vector3 GetMouseLookDirection()
    {
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            ray.origin += aimOffset;

            //ray.origin += offset;
            // Raycast towards the mouse collider box in the world
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, mouseLookMask))
            {
                if (hit.collider != null)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(hit.point - transform.position);
                    if (lookRotation.eulerAngles != Vector3.zero) // It already shouldn't be...
                    {
                        lookRotation.x = 0f;
                        lookRotation.z = 0f;
                        lookRotation.eulerAngles += aimOffset;
                        return lookRotation.eulerAngles;
                    }
                }
            }
        }

        return Vector3.zero;
    }
    private Vector3 GetMouseLookDirection(Component character)
    {
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            ray.origin += aimOffset;

            //ray.origin += offset;
            // Raycast towards the mouse collider box in the world
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, mouseLookMask))
            {
                if (hit.collider != null)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(hit.point - character.transform.position);
                    if (lookRotation.eulerAngles != Vector3.zero) // It already shouldn't be...
                    {
                        lookRotation.x = 0f;
                        lookRotation.z = 0f;
                        lookRotation.eulerAngles += aimOffset;
                        return lookRotation.eulerAngles;
                    }
                }
            }
        }

        return Vector3.zero;
    }
}