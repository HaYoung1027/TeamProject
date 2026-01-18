using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("감지 거리")]
    public float distance;
    [Header("조준 거리")]
    public float atkDistince;
    [Header("감지 대상 레이어 (보통 Player 레이어)")]
    public LayerMask isLayer;
    [Header("이동 속도")]
    public float speed;
    [Header("발사할 총알 프리팹")]
    public GameObject bullet;
    [Header("총알이 생성 위치 오프셋 (적 기준)")]
    public Vector3 bulletPos;
    [Header("한 번 발사 후 다음 발사까지의 쿨타임")]
    public float coolTime;

    private float currentTime;              // 현재 남아있는 쿨타임 시간
    [Header("적의 최초 위치 (복귀 지점)")]
    public Vector3 startPos;
    [Header("플레이어를 놓친 후 복귀를 시작하기까지의 대기 시간")]
    public float maxTime;
    private float curTime;                  // 플레이어 미감지 상태에서 흐르는 시간

    [Header("감지 후 발사까지 대기 시간")]
    public float detectDelay = 1.0f;
    private float detectTimer = 0f;         // 감지 상태가 유지된 시간 누적용 타이머

    [Header("발사 유지 시간")]
    public float attackWindowTime = 3f;
    private float attackWindowTimer = 0f;   // 발사 가능 상태가 유지된 시간 누적용 타이머

    [Header("조준선이 깜빡이는 속도")]
    public float blinkSpeed = 6f;
    [Header("플레이어에게 붙는 조준 프리팹")]
    public GameObject aiming;
    private GameObject currentAiming;       // 현재 생성되어 플레이어에게 붙어 있는 조준 오브젝트

    private Transform enemyTransform;       // 적 자신의 Transform 위치
    private Transform targetPlayer;         // 현재 타겟으로 잡힌 플레이어 Transform

    private int moveDirection = 1;          // 적의 순찰 및 감지 방향 1: 오른쪽, -1: 왼쪽

    LineRenderer line;                      // 플레이어와 적 사이를 잇는 조준선

    private bool blinking = false;          // 조준선이 깜빡이는 상태인지 여부

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
            new Vector2(3f, 16f),           // 감지 범위
            CapsuleDirection2D.Vertical,
            0f,
            rayDir,
            distance,
            isLayer
        );

        if (raycast.collider != null)      // 플레이어 감지
        {
            curTime = 0;
            Transform player = raycast.collider.transform;

            detectTimer += Time.deltaTime; // 감지 시간 증가

            float dist = Vector2.Distance(transform.position, player.position);

            if (dist < atkDistince)
            {
                // 조준선 잇기
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

        // 조준선 깜빡임 처리
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

    void ResetAttackState() // 초기화
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
