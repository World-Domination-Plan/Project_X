using UnityEngine;
using UnityEngine.InputSystem;

namespace SandboxXRI
{
    [RequireComponent(typeof(CharacterController))]
    public class DesktopFirstPersonController : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;

        [Header("Movement")]
        public float moveSpeed = 3.5f;
        public float sprintSpeed = 6f;
        public float gravity = -9.81f;

        [Header("Look")]
        public float lookSensitivity = 0.1f;
        public float minPitch = -80f;
        public float maxPitch = 80f;
        public bool lockCursor = true;

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        void OnEnable()
        {
            Debug.Log("[DesktopFirstPersonController] Enabled on " + gameObject.name);

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            Debug.Log("[DesktopFirstPersonController] Disabled on " + gameObject.name);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (playerCamera == null)
                return;

            HandleLook();
            HandleMove();
        }

        void HandleLook()
        {
            if (Mouse.current == null)
                return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * lookSensitivity;

            transform.Rotate(Vector3.up * mouseDelta.x);

            pitch -= mouseDelta.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void HandleMove()
        {
            if (Keyboard.current == null)
                return;

            Vector3 move = Vector3.zero;

            if (Keyboard.current.wKey.isPressed) move += transform.forward;
            if (Keyboard.current.sKey.isPressed) move -= transform.forward;
            if (Keyboard.current.aKey.isPressed) move -= transform.right;
            if (Keyboard.current.dKey.isPressed) move += transform.right;

            move.y = 0f;
            move = move.normalized;

            float speed = Keyboard.current.leftShiftKey.isPressed ? sprintSpeed : moveSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = move * speed;
            velocity.y = verticalVelocity;

            controller.Move(velocity * Time.deltaTime);
        }
    }
}