using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerscript : MonoBehaviour
{
    [SerializeField] public Rigidbody2D rb;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;

    private float horizontal;

    public float speed;
    public float jumpHeight = 5;
    public float buttonTime = 0.3f;
    public float cancelRate = 100;

    public GameObject jumpParticles;

    [Header("Ground collision")]
    public Vector2 boxSize;
    public float castDistance;
    public LayerMask groundLayer;

    [Header("Wall jumping")]
    public float wallSlidingSpeed = 2f;
    private bool isWallSliding;
    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    public float wallJumpingDuration = 0.4f;
    public Vector2 wallJumpingPower;

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

    // Start is called before the first frame update
    void Start()
    {
        isFacingRight = true;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
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

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded()){
            float jumpForce = Mathf.Sqrt(jumpHeight * -2 * (Physics2D.gravity.y * rb.gravityScale));
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
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
            SpawnJumpParticles();
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

    private void SpawnJumpParticles()
    {
        GameObject tmpParticles = (GameObject)Instantiate(jumpParticles, transform.position, Quaternion.identity);
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
