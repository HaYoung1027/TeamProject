using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IDamageable
{
    public bool isGrounded;

    public Vector2 inputVec;
    Rigidbody2D rigid;
    SpriteRenderer sprite;
    GrapplingHook grappling;

    PlayerInteraction interaction;  // 상호작용

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        grappling = GetComponent<GrapplingHook>();
        interaction = GetComponent<PlayerInteraction>();
    }
    void Start()
    {
        isGrounded = true;
    }
    void FixedUpdate()
    {
        if (interaction && interaction.GetIsAction()) return;

        float speed = GameManager.Instance.playerStatsRuntime.speed;

        if (grappling.isAttach) // 훅 매달림
        {
            float hookSwingForce = GameManager.Instance.playerStatsRuntime.hookSwingForce;
            rigid.AddForce(new Vector2(inputVec.x * hookSwingForce, 0f));
        }
        else // 일반 이동
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

        rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);

        isGrounded = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts[0].normal.y > 0.7f)
        {
            isGrounded = true;
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocityY);
        }
    }


    void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
    }

    // 플레이어 데미지
    void IDamageable.TakeDamage(int attack)
    {
        // 플레이어 체력 줄어들기
        GameManager.Instance.playerStatsRuntime.currentHP -= attack;

        Debug.Log("[플레이어 데미지] 플레이어 현재 체력: " + GameManager.Instance.playerStatsRuntime.currentHP);
    }

}
