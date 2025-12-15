using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float jumpForce;
    bool isGrounded;

    public Vector2 inputVec;
    Rigidbody2D rigid;
    SpriteRenderer sprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        // x 이동
        float speed = GameManager.Instance.playerStatsRuntime.speed;
        float x = inputVec.x * speed * Time.deltaTime;
        transform.Translate(x, 0, 0);

        // 방향 플립
        if (inputVec.x > 0)
            sprite.flipX = false;
        else if (inputVec.x < 0)
            sprite.flipX = true;
    }

    void OnJump()
    {
        if (!isGrounded) return;

        rigid.linearVelocity = new Vector2(
            rigid.linearVelocity.x, jumpForce);

        isGrounded = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts[0].normal.y > 0.7f)
        {
            isGrounded = true;
        }
    }

    void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
    }
}
