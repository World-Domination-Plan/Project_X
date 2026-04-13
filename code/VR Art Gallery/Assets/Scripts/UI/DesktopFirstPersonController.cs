using UnityEngine;
using UnityEngine.InputSystem;

namespace SandboxXRI
{
    public class DesktopFirstPersonController : MonoBehaviour
    {
        [Header("References - Auto-found if left empty")]
        public Transform xrOriginRoot;
        public Transform cameraOffset;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float sprintMultiplier = 2f;

        [Header("Mouse Look")]
        public float mouseSensitivity = 0.2f;
        public float maxPitchAngle = 85f;

        private float _pitch = 0f;
        private bool _cursorLocked = false;
        private CharacterController _cc;
        private Rigidbody _rb;

        void OnEnable()
        {
            ResolveReferences();
        }

        void OnDisable()
        {
            LockCursor(false);
        }

        public void Activate()
        {
            ResolveReferences();
            _pitch = 0f;
            LockCursor(true);
        }

        void ResolveReferences()
        {
            if (xrOriginRoot == null)
            {
                var origin = GameObject.Find("XR Interaction Setup MP Variant");
                if (origin != null) xrOriginRoot = origin.transform;
                else { Debug.LogError("[DesktopFPC] XR rig not found!"); return; }
            }

            if (cameraOffset == null)
            {
                var cam = xrOriginRoot.Find("Camera Offset");
                if (cam != null) cameraOffset = cam;
                else Debug.LogError("[DesktopFPC] Camera Offset not found!");
            }

            // Cache movement components — try CC first, then Rigidbody, then raw transform
            _cc = xrOriginRoot.GetComponent<CharacterController>();
            _rb = xrOriginRoot.GetComponent<Rigidbody>();

            Debug.Log($"[DesktopFPC] Ready. Root={xrOriginRoot?.name} | CC={_cc != null} | RB={_rb != null}");
        }

        void Update()
        {
            if (xrOriginRoot == null || cameraOffset == null) return;

            try
            {
                if (Mouse.current != null) HandleMouseLook();
                if (Keyboard.current != null) HandleMovement();
                HandleCursorToggle();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DesktopFPC] Exception in Update: {e.Message}");
            }
        }

        void HandleMouseLook()
        {
            if (!_cursorLocked) return;

            Vector2 delta = Mouse.current.delta.ReadValue();
            xrOriginRoot.Rotate(Vector3.up, delta.x * mouseSensitivity, Space.World);
            _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, -maxPitchAngle, maxPitchAngle);
            cameraOffset.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        void HandleMovement()
        {
            float h = 0f, v = 0f;
            if (Keyboard.current.wKey.isPressed) v += 1f;
            if (Keyboard.current.sKey.isPressed) v -= 1f;
            if (Keyboard.current.aKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed) h += 1f;

            if (h == 0f && v == 0f) return;

            float speed = moveSpeed * (Keyboard.current.leftShiftKey.isPressed ? sprintMultiplier : 1f);
            Vector3 move = (xrOriginRoot.right * h + xrOriginRoot.forward * v).normalized * speed * Time.deltaTime;

            if (_cc != null && _cc.enabled)
            {
                _cc.Move(move);
            }
            else if (_rb != null)
            {
                _rb.MovePosition(_rb.position + move);
            }
            else
            {
                xrOriginRoot.position += move;
            }
        }

        void HandleCursorToggle()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) LockCursor(false);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !_cursorLocked) LockCursor(true);
        }

        void LockCursor(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}