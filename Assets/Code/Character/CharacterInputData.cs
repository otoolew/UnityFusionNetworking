using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[System.Flags]
public enum InputButton
{
    FORWARD = 1 << 0,
    BACKWARD = 1 << 1,
    LEFT = 1 << 2,
    RIGHT = 1 << 3,
    USE = 1 << 4,
    FIRE = 1 << 5,
    FIRE_ALT = 1 << 6,
    RELOAD = 1 << 7,
    JUMP = 1 << 8,
    CROUCH = 1 << 9,
    SPRINT = 1 << 10,
    ACTION_01 = 1 << 11,
    ACTION_02 = 1 << 12,
    ACTION_03 = 1 << 13,
    ACTION_04 = 1 << 14,
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
    public Vector3 MoveDirection;
    public Vector3 AimDirection;
    public InputButton Buttons;
    public bool GetButton(InputButton button)
    {
        return (Buttons & button) == button;
    }
}
