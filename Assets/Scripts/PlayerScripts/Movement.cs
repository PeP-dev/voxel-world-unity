using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private CharacterController controller;
    [SerializeField]
    private Transform camTransform = null;
    [SerializeField]
    private int gravityForce = 3;
    private float gravity;
    [SerializeField]
    private float speed = 6f;
    [SerializeField]
    private float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    private Vector3 moveDir;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }
    private void FixedUpdate()
    {
        gravity -= gravityForce * Time.deltaTime;
        if (controller.isGrounded)
        {
            gravity = 0;
            if (Input.GetKey("space"))
                gravity = 1;
        }


        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(moveHorizontal, 0f, moveVertical).normalized;

        if(direction.magnitude >= 1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        else
        {
            moveDir = Vector3.zero;
        }
        moveDir.y = gravity;
        controller.Move(moveDir * speed * Time.deltaTime);
    }
}
