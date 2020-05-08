using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    private Rigidbody2D rb;    
    public float jumpForce;
    public float speed;

    public bool isGrounded = false;
    public Transform GroundedChecker;
    public float checkGroundRadius;
    public LayerMask groundLayer;

    public float rememberGroundedFor;
    float lastTimeGrounded;

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update () {

        CheckIsGrounded();
        Jump();
        Movement();    
        
	}

    public void CheckIsGrounded()
    {
        Collider2D coll = Physics2D.OverlapCircle(GroundedChecker.position, checkGroundRadius, groundLayer);

        if(coll != null)
        {
            isGrounded = true;
        } else
        {
            if (isGrounded)
            {
                lastTimeGrounded = Time.time;
            }
            isGrounded = false;
        }
    }

    public void Jump()
    {
        if (Input.GetButtonDown("Jump") && (isGrounded || Time.time - lastTimeGrounded <= rememberGroundedFor))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    public void Movement()
    {      
        float xIn = Input.GetAxisRaw("Horizontal");
        float moveBy = xIn * speed;

        rb.velocity = new Vector2(moveBy, rb.velocity.y);
    }
}
