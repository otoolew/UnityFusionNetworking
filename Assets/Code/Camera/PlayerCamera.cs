using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    public Transform CameraTransform { get => cameraTransform; set => cameraTransform = value; }
    
    [SerializeField] private Vector3 transformOffset;
    public Vector3 TransformOffset { get => transformOffset; set => transformOffset = value; }

    [SerializeField] private Vector3 rotationOffset;
    public Vector3 RotationOffset { get => rotationOffset; set => rotationOffset = value; }

    [SerializeField] private float followSpeed;
    public float FollowSpeed { get => followSpeed; set => followSpeed = value; }
    
    [SerializeField] private Transform cameraTarget;
    public Transform CameraTarget { get => cameraTarget; set => cameraTarget = value; }
    
    private float step;
    
    #region MonoBehaviour

    private void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            cameraTransform.position = transformOffset;
            cameraTransform.rotation = Quaternion.Euler(rotationOffset);
        }
    }
    private void LateUpdate()
    {
        if (CameraTarget == null)
        {
            return;
        }
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, cameraTarget.position + transformOffset, Time.deltaTime * followSpeed);
    }
    #endregion

    #region Methods
    
    public void AssignFollowTarget(Transform targetTransform)
    {
        cameraTarget = targetTransform;
    }
    public void UnfollowTarget()
    {
        cameraTarget = null;
    }
    public void ResetValues()
    {
        transform.parent = null;
        cameraTransform.position = transformOffset;
        cameraTransform.rotation = Quaternion.Euler(rotationOffset);
        transform.position = cameraTarget.position;
    }
    #endregion
}
