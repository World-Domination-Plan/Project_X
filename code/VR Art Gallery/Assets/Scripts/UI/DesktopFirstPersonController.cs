using UnityEngine;
using UnityEngine.InputSystem;

namespace SandboxXRI
{
    public class DesktopFirstPersonController : MonoBehaviour
    {
        [Header("References — auto-found if left empty")]
        public Transform xrOriginRoot;
        public Transform cameraOffset;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float sprintMultiplier = 2f;

        [Header("Mouse Look")]
        public float mouseSensitivity = 0.2f;
        public float maxPitchAngle = 85f;

        private float _pitch = 0f;
        private bool _cursorLocked = true;

        void OnEnable()
        {
            // Auto-find references if not assigned in Inspector
            if (xrOriginRoot == null)
            {
                var origin = GameObject.Find("XR Interaction Setup MP Variant");
                if (origin != null)
                    xrOriginRoot = origin.transform;
                else
                    Debug.LogError("[DesktopFPC] Could not find 'XR Interaction Setup MP Variant'");
            }

            if (cameraOffset == null && xrOriginRoot != null)
            {
                var cam = xrOriginRoot.Find("Camera Offset");
                if (cam != null)
                    cameraOffset = cam;
                else
                    Debug.LogError("[DesktopFPC] Could not find 'Camera Offset' under XR rig");
            }

            Debug.Log($"[DesktopFPC] xrOriginRoot: {xrOriginRoot}");
            Debug.Log($"[DesktopFPC] cameraOffset: {cameraOffset}");

            LockCursor(true);
        }

        void OnDisable()
        {
            LockCursor(false);
        }

        void Update()
        {
            // null guards for input system and references
            if (Mouse.current == null || Keyboard.current == null) return;

            if (xrOriginRoot == null || cameraOffset == null) return;

            HandleMouseLook();
            HandleMovement();
            HandleCursorToggle();
        }

        void HandleMouseLook()
        {
            if (!_cursorLocked) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float mouseX = mouseDelta.x * mouseSensitivity;
            float mouseY = mouseDelta.y * mouseSensitivity;

            xrOriginRoot.Rotate(Vector3.up, mouseX, Space.World);

            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -maxPitchAngle, maxPitchAngle);
            cameraOffset.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        void HandleMovement()
        {
            Vector2 moveInput = Vector2.zero;

            if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;

            bool sprinting = Keyboard.current.leftShiftKey.isPressed;
            float speed = moveSpeed * (sprinting ? sprintMultiplier : 1f);

            Vector3 move = (xrOriginRoot.right * moveInput.x + xrOriginRoot.forward * moveInput.y).normalized
                           * speed * Time.deltaTime;
            xrOriginRoot.position += move;
        }

        void HandleCursorToggle()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                LockCursor(false);

            if (Mouse.current.leftButton.wasPressedThisFrame && !_cursorLocked)
                LockCursor(true);
        }

        void LockCursor(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
