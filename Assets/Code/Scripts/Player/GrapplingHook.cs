using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrapplingHook : MonoBehaviour
{
    public LineRenderer line;
    public Transform hook;
    private Vector2 mousedir;

    public bool isHookActive;
    public bool isLineMax;
    public bool isAttach;
    public bool isEnemyAttach;

    // 슬로우 효과 변수
    public float slowFactor;    // 슬로우 비율
    public float slowLength;    // 원래 속도로 복귀하는 데 걸리는 시간
    Coroutine slowCoroutine;    // 슬로우 효과 코루틴

    public Vector3 enemyFollowOffset = Vector3.zero;
    private List<Transform> enemies = new List<Transform>();
    Rigidbody2D rb;
    SpriteRenderer sprite;
    Coroutine gravityCoroutine;
    float originalGravity;
    float originalSpeed;

    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundRadius = 0.15f;

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
    }

    void Start()
    {
        // 라인을 그리는 포지션을 두개로 설정하고 (PositionCount)
        // 한 점은 Player의 포지션, 한 점은 Hook의 포지션으로 설정 (SetPosition)
        line.positionCount = 2;
        line.endWidth = line.startWidth = 0.05f;
        line.SetPosition(0, transform.position);
        line.SetPosition(1, hook.position);
        line.useWorldSpace = true;
        isAttach = false;
        hook.gameObject.SetActive(false);

        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        line.SetPosition(0, transform.position);
        line.SetPosition(1, hook.position);

        if (Mouse.current.leftButton.wasPressedThisFrame && !isHookActive && !isAttach && !isEnemyAttach)
        {
            GameManager.Instance.cameraShake.ShakeForSeconds(0.1f);
            hook.position = transform.position;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorldPos.z = 0f;

            mousedir = mouseWorldPos - transform.position;
            isHookActive = true;
            hook.gameObject.SetActive(true);
        }

        // 훅이 발사된 상태이고, 아직 최대 사거리에 도달하지 않았을 때
        if (isHookActive && !isLineMax && !isAttach && !isEnemyAttach)
        {
            // 마우스 방향으로 훅을 전진시킴
            hook.Translate(mousedir.normalized * Time.deltaTime * GameManager.Instance.playerStatsRuntime.hookSpeed);
            // 플레이어와 훅 사이의 거리가 최대 사거리보다 커지면
            if (Vector2.Distance(transform.position, hook.position) > GameManager.Instance.playerStatsRuntime.hookDistance)
            {
                // 최대 사거리 도달 상태로 전환
                isLineMax = true;
            }
        }

        // 훅이 최대 사거리에 도달한 이후
        else if (isHookActive && isLineMax && !isAttach && !isEnemyAttach)
        {
            // 훅을 플레이어 위치로 부드럽게 되돌림
            hook.position = Vector2.MoveTowards(hook.position, transform.position, Time.deltaTime * GameManager.Instance.playerStatsRuntime.hookSpeed);

            // 훅이 거의 플레이어 위치까지 돌아왔을 경우
            if (Vector2.Distance(transform.position, hook.position) < 0.1f)
            {
                // 훅 상태 초기화
                isHookActive = false;
                isLineMax = false;
                // 훅 오브젝트 비활성화
                hook.gameObject.SetActive(false);
            }
        }

        else if (isAttach)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                isAttach = false;
                isHookActive = false;
                isLineMax = false;
                hook.GetComponent<Hooking>().joint2D.enabled = false;
                hook.gameObject.SetActive(false);
                if (gravityCoroutine != null)
                    StopCoroutine(gravityCoroutine);

                gravityCoroutine = StartCoroutine(TempChangeGravity(0.2f, 0f, 1f));

                // 슬로우 효과
                if (slowCoroutine != null)
                    StopCoroutine(slowCoroutine);

                slowCoroutine = StartCoroutine(SlowRoutine());
            } 
        }

        else if (isEnemyAttach)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && enemies.Count > 0)
            {
                Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

                Vector2 dir = mouseWorld - (Vector2)transform.position;

                ThrowEnemy(enemies[0], dir, GameManager.Instance.playerStatsRuntime.hookEnemyThrowForce);
            }

        }
        if (IsGrounded() && gravityCoroutine != null)
        {
            StopCoroutine(gravityCoroutine);
            gravityCoroutine = null;

            rb.gravityScale = originalGravity;
            GameManager.Instance.playerStatsRuntime.speed = originalSpeed;
            sprite.color = Color.white;
        }
    }

    void LateUpdate()
    {
        if (!isEnemyAttach) return;

        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;

            Vector3 offset = enemyFollowOffset;
            offset.x = playerSprite.flipX ? -Mathf.Abs(enemyFollowOffset.x) : Mathf.Abs(enemyFollowOffset.x);

            enemies[i].localPosition = offset; // 부모 transform 기준 localPosition
        }
    }

    public void AttachEnemy(Transform enemy)
    {
        if (enemies.Contains(enemy)) return;

        enemies.Add(enemy);

        Collider2D enemyCol = enemy.GetComponent<Collider2D>();
        Collider2D playerCol = GetComponent<Collider2D>();

        if (enemyCol != null && playerCol != null)
            Physics2D.IgnoreCollision(enemyCol, playerCol, true);

        // Rigidbody가 있으면 Kinematic으로
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;

        // 플레이어 자식으로
        enemy.SetParent(transform);

        // 플레이어 SpriteRenderer 가져오기
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();

        // enemyFollowOffset 기준으로 x를 왼쪽/오른쪽 맞춤
        Vector3 offset = enemyFollowOffset;
        offset.x = playerSprite.flipX ? -Mathf.Abs(enemyFollowOffset.x) : Mathf.Abs(enemyFollowOffset.x);

        enemy.localPosition = offset;

        // 훅 & 줄 숨기기
        hook.gameObject.SetActive(false);
        line.enabled = false;

        isEnemyAttach = true;
        isAttach = false;
        isHookActive = false;
        isLineMax = false;
    }

    public void ThrowEnemy(Transform enemy, Vector2 throwDir, float throwForce)
    {
        if (!enemies.Contains(enemy)) return;

        enemies.Remove(enemy);

        // 부모 해제
        enemy.SetParent(null);

        Collider2D enemyCol = enemy.GetComponent<Collider2D>();
        Collider2D playerCol = GetComponent<Collider2D>();

        // 1초간 충돌 무시
        if (enemyCol != null && playerCol != null)
            StartCoroutine(IgnoreCollisionTemporarily(enemyCol, playerCol, 0.3f));

        // Rigidbody 처리
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(throwDir.normalized * throwForce, ForceMode2D.Impulse);
        }

        if (enemies.Count == 0)
            isEnemyAttach = false;

        line.enabled = true;

        // 훅 상태 초기화
        isHookActive = false;
        isLineMax = false;
        hook.GetComponent<Hooking>().joint2D.enabled = false;
        hook.gameObject.SetActive(false);
    }

    // 충돌 무시 코루틴
    IEnumerator IgnoreCollisionTemporarily(Collider2D enemyCol, Collider2D playerCol, float duration)
    {
        Physics2D.IgnoreCollision(enemyCol, playerCol, true);

        yield return new WaitForSeconds(duration);

        // 오브젝트가 아직 살아있을 때만 복구
        if (enemyCol != null && playerCol != null)
            Physics2D.IgnoreCollision(enemyCol, playerCol, false);
    }
    IEnumerator TempChangeGravity(float tempGravity, float tempSpeed, float duration)
    {

        sprite.color = Color.red;
        originalSpeed = GameManager.Instance.playerStatsRuntime.speed;
        originalGravity = rb.gravityScale;
        rb.gravityScale = tempGravity;
        GameManager.Instance.playerStatsRuntime.speed = tempSpeed;

        yield return new WaitForSeconds(duration);

        rb.gravityScale = originalGravity;
        GameManager.Instance.playerStatsRuntime.speed = originalSpeed;
        gravityCoroutine = null;
        sprite.color = Color.white;
    }
    // 슬로우 효과 코루틴
    IEnumerator SlowRoutine()
    {
        // 슬로우 적용
        Time.timeScale = slowFactor;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 서서히 원래 속도로 복귀
        while (Time.timeScale < 1f)
        {
            Time.timeScale += (1f / slowLength) * Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
            Time.fixedDeltaTime = Time.timeScale * 0.02f;
            yield return null;
        }

        // 오차 방지를 위해 기본값으로 복구
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
