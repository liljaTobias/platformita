using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class playerscript : MonoBehaviour
{
    [SerializeField] public Rigidbody2D rb;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    private PlayerState playerState;

    private float horizontal;

    public float speed;

    [Header("Jumping")]
    public float jumpPower = 16;
    public float buttonTime = 0.3f;
    public float cancelRate = 100;
    public int maxNumberOfJumps = 1;
    private int amountOfJumpsLeft = 1;

    [Space]
    [Header("Ground collision")]
    public Vector2 boxSize;
    public float castDistance;
    public LayerMask groundLayer;

    [Space]
    [Header("Wall jumping")]
    public float wallSlidingSpeed = 2f;
    private bool isWallSliding;
    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    public float wallJumpingDuration = 0.4f;
    public Vector2 wallJumpingPower;

    [Space]
    [Header("Dashing")]
    public TrailRenderer trailRenderer;
    public float dashingPower;
    public float dashingTime;
    public float dashingCooldown;
    private bool canDash = true;
    private bool isDashing;

    bool jumping;
    bool jumpCancelled;
    float jumptime;

    private Animator anim;

    private bool isFacingRight;

    private Vector3 checkpointPosition;

    // Start is called before the first frame update
    void Start()
    {
        isFacingRight = true;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerState = GetComponent<PlayerState>();
        checkpointPosition = gameObject.transform.position;
    }

    // Update is called once per frame, use this for stuff like reading input
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.transform.position = checkpointPosition;
        }

        if(isDashing)
        {
            return;
        }

        horizontal = Input.GetAxis("Horizontal");

        HandleJump();

        WallSlide();
        WallJump();

        if(Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }

        if (!isWallJumping)
        {
            Flip();
        }

        HandleAnimations();
    }

    // Fixed update is best used for physics calculations
    private void FixedUpdate()
    {
        if (isDashing)
        {
            return;
        }


        if (!isWallJumping)
        {
            rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
        }

        if (jumpCancelled && jumping && rb.velocity.y > 0)
        {
            rb.AddForce(Vector2.down * cancelRate);
        }
    }

    public bool isGrounded()
    {
        if(Physics2D.BoxCast(transform.position, boxSize, 0, -transform.up, castDistance, groundLayer))
        {
           return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position-transform.up * castDistance, boxSize);
    }

    public void Flip()
    {
        if (!isFacingRight && horizontal > 0f || isFacingRight && horizontal < 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private void Jump(Vector2 dir)
    {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpPower;
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded()){
            //float jumpForce = Mathf.Sqrt(jumpPower * -2 * (Physics2D.gravity.y * rb.gravityScale));
            //rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            Jump(Vector2.up);
            jumping = true;
            jumptime = 0;
            jumpCancelled = false;
        }

        if (jumping)
        {            
            jumptime += Time.deltaTime;
            if (Input.GetKeyUp(KeyCode.Space) | jumptime > buttonTime)
            {
                jumpCancelled = true;
            }
            if(jumptime > buttonTime)
            {
                jumping = false;
            }
        }

        if(!isGrounded())
        {
            // Handle double jump
            if (Input.GetKeyDown(KeyCode.Space) && amountOfJumpsLeft > 0)
            {
                //float jumpForce = Mathf.Sqrt(jumpPower * -2 * (Physics2D.gravity.y * rb.gravityScale));
                //rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
                Jump(Vector2.up);
                amountOfJumpsLeft -= 1;
            }
        } else
        {
            ResetDoubleJump();
        }
    }

    private bool isWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }

    private void WallSlide()
    {
        if(isWalled() && !isGrounded() && horizontal != 0f)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        if(isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if(Input.GetKeyDown(KeyCode.Space) && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if(transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        ResetDoubleJump();
        isWallJumping = false;
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        trailRenderer.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        trailRenderer.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }

    private void ResetDoubleJump()
    {
        amountOfJumpsLeft = maxNumberOfJumps;
    }

    private void HandleAnimations()
    {
        if (horizontal != 0)
        {
            anim.SetBool("isRunning", true);
        }
        else
        {
            anim.SetBool("isRunning", false);
        }

        if (rb.velocity.y < 0 && !isGrounded())
        {
            anim.SetBool("isFalling", true);
        }
        else
        {
            anim.SetBool("isFalling", false);
        }

        anim.SetBool("isJumping", !isGrounded());

        anim.SetBool("isWallSliding", isWallSliding);
    }
}
