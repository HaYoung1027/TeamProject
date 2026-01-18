using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public float distance;
    public float atkDistince;
    public LayerMask isLayer;
    public float speed;

    public GameObject bullet;
    public Vector3 bulletPos;
    public float coolTime;
    private float currentTime;

    public Vector3 startPos;
    public float maxTime;
    public float curTime;

    [Header("Attack Delay")]
    public float detectDelay = 1.0f;   // 감지 후 발사까지 대기 시간
    private float detectTimer = 0f;

    [Header("Attack Window")]
    public float attackWindowTime = 3f;   // 발사 가능한 유지 시간
    private float attackWindowTimer = 0f;

    [Header("Blink Setting")]
    public float blinkSpeed = 6f;

    public GameObject aiming;          // 조준 프리팹
    private GameObject currentAiming;  // 현재 조준 오브젝트

    private Transform enemyTransform;
    private Transform targetPlayer;

    private int moveDirection = 1; // 1: 오른쪽, -1: 왼쪽

    LineRenderer line;

    private bool blinking = false;

    private void Start()
    {
        startPos = transform.position;
        enemyTransform = transform;

        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.enabled = false;
        line.startColor = Color.red;
        line.endColor = Color.red;
    }

    private void FixedUpdate()
    {
        Vector2 rayDir = transform.right * moveDirection;

        RaycastHit2D raycast = Physics2D.CapsuleCast(
            transform.position,
            new Vector2(3f, 16f),
            CapsuleDirection2D.Vertical,
            0f,
            rayDir,
            distance,
            isLayer
        );

        if (raycast.collider != null) // 플레이어 감지
        {
            curTime = 0;
            Transform player = raycast.collider.transform;

            detectTimer += Time.deltaTime;

            float dist = Vector2.Distance(transform.position, player.position);

            if (dist < atkDistince)
            {
                line.SetPosition(0, enemyTransform.position);
                line.SetPosition(1, player.position);

                if (detectTimer < detectDelay)
                {
                    float remain = detectDelay - detectTimer;

                    if (remain > 1f)
                    {
                        line.enabled = true;
                        blinking = false;
                    }
                    else
                    {
                        // 1초 남았을 때 깜빡임
                        blinking = true;
                    }

                    CreateAiming(player);
                }
                else
                {
                    attackWindowTimer += Time.deltaTime;

                    // 쿨타임 진입해도 깜빡임 유지
                    blinking = true;

                    if (attackWindowTimer >= attackWindowTime)
                    {
                        currentTime = coolTime;
                        detectTimer = 0f;
                        attackWindowTimer = 0f;
                        RemoveAiming();
                    }
                    else if (currentTime <= 0)
                    {
                        Instantiate(bullet, transform.position + bulletPos, transform.rotation);
                        currentTime = coolTime;
                        RemoveAiming();
                    }
                }
            }
            else
            {
                // 추적 상태 (조준 X)
                ResetAttackState();

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    player.position,
                    Time.deltaTime * speed
                );
            }

            if (currentTime > 0)
                currentTime -= Time.deltaTime;
        }
        else // 플레이어 미감지
        {
            ResetAttackState();

            curTime += Time.deltaTime;
            if (curTime >= maxTime)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    startPos,
                    Time.deltaTime * speed
                );
            }
        }

        // ===== 깜빡임 처리 (enable on/off) =====
        if (blinking)
        {
            line.enabled = Mathf.FloorToInt(Time.time * blinkSpeed) % 2 == 0;
            line.startColor = Color.red;
            line.endColor = Color.red;
        }

        // 순찰 방향 전환
        if (raycast.collider == null && curTime >= maxTime)
        {
            moveDirection *= -1;
        }

        Debug.DrawRay(transform.position + Vector3.up, rayDir * distance, Color.red);
        Debug.DrawRay(transform.position - Vector3.up, rayDir * distance, Color.red);
    }

    void ResetAttackState()
    {
        detectTimer = 0f;
        attackWindowTimer = 0f;
        blinking = false;
        line.enabled = false;
        RemoveAiming();
    }

    // 조준 생성
    void CreateAiming(Transform player)
    {
        if (currentAiming == null)
        {
            currentAiming = Instantiate(aiming);
            currentAiming.transform.SetParent(player);
            currentAiming.transform.localPosition = Vector3.zero;
            currentAiming.transform.localRotation = Quaternion.identity;

            targetPlayer = player;
        }
    }

    // 조준 제거
    void RemoveAiming()
    {
        if (currentAiming != null)
        {
            Destroy(currentAiming);
            currentAiming = null;
            targetPlayer = null;
        }
    }
}
