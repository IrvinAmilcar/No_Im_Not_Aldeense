using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackrendEntertainment
{
    public class FirstPersonController : MonoBehaviour
    {
        public float mouseSens = 100f;
        public Transform bodyCapsule;
        float xRotation = 0f;
        public Transform cameraTransform;

        public CharacterController controller;
        public float moveSpeed = 10f;
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        private void Update()
        {
            Gravity();
            MouseLook();
            Movement();
            Crouch();
        }
        private void MouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSens * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSens * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            bodyCapsule.Rotate(Vector3.up * mouseX);
        }
        private void Movement()
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * moveSpeed * Time.deltaTime);
            controller.Move(velocity * Time.deltaTime);
        }
        public bool isCrouching = false;
        private void Crouch()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (!isCrouching)
                {
                    isCrouching = true;
                    controller.height = .25f;
                }
            }
            else if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                isCrouching = false;
                controller.height = 2f;
            }
        }
        Vector3 velocity;
        public float gravity = -9.81f;
        public float groundDistance = .4f;
        public Transform GroundChecker;
        bool isGrounded;
        private void Gravity()
        {
            isGrounded = Physics.CheckSphere(GroundChecker.position, groundDistance);
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
        }
    }
}