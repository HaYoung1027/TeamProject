using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float jumpForce;
    bool isGrounded;

    public Vector2 inputVec;
    Rigidbody2D rigid;
    SpriteRenderer sprite;
    GrapplingHook grappling;
    
    PlayerInteraction interaction;  // 상호작용

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        grappling = GetComponent<GrapplingHook>();
        interaction = GetComponent<PlayerInteraction>();
    }

    void FixedUpdate()
    {
        // 대화중일 경우 액션 X
        if (interaction && interaction.GetIsAction()) return;

        // x 이동
        float speed = GameManager.Instance.playerStatsRuntime.speed;

        if (grappling.isAttach) // 매달리기 중일 때
        {
            float hookSwingForce = GameManager.Instance.playerStatsRuntime.hookSwingForce; // rigidbody add force
            rigid.AddForce(new Vector2(inputVec.x * hookSwingForce, 0f));
        }
        else // 일반 이동 중일 때
        {
            float x = inputVec.x * speed * Time.deltaTime; // translate
            transform.Translate(x, 0, 0);
        }

        // 방향 플립
        if (inputVec.x > 0)
            sprite.flipX = false;
        else if (inputVec.x < 0)
            sprite.flipX = true;
    }

    void OnJump()
    {
        if (!isGrounded) return;

        rigid.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

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
