using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityFusionNetworking
{
    public class PlayerCamera : SceneService
    {
        [SerializeField] private Camera cameraComponent;
        public Camera CameraComponent { get => cameraComponent; set => cameraComponent = value; }

        [SerializeField] private Vector3 transformOffset;
        public Vector3 TransformOffset { get => transformOffset; set => transformOffset = value; }

        [SerializeField] private Vector3 rotationOffset;
        public Vector3 RotationOffset { get => rotationOffset; set => rotationOffset = value; }
    }
}