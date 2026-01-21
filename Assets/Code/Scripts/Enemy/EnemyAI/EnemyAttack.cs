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
    private float currentTime;
    [Header("적의 최초 위치 (복귀 지점)")]
    public Vector3 startPos;
    [Header("플레이어를 놓친 후 복귀를 시작하기까지의 대기 시간")]
    public float maxTime;
    private float curTime;
    [Header("감지 후 발사까지 대기 시간")]
    public float detectDelay = 1.0f;
    private float detectTimer = 0f;
    [Header("발사 유지 시간")]
    public float attackWindowTime = 2f;
    private float attackWindowTimer = 0f;
    [Header("조준선이 깜빡이는 속도")]
    public float blinkSpeed = 6f;
    [Header("플레이어에게 붙는 조준 프리팹")]
    public GameObject aiming;

    private GameObject currentAiming;
    private Transform enemyTransform;
    private Transform targetPlayer;
    private Transform targetAimPoint;
    private int moveDirection = 1;
    private bool blinking = false;
    LineRenderer line;

    private bool isAttacking = false;

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

        if (raycast.collider != null && targetPlayer == null)
        {
            curTime = 0;
            targetPlayer = raycast.collider.transform;
            targetAimPoint = targetPlayer.Find("AimPoint");
        }

        if (targetPlayer != null && targetAimPoint != null)
        {
            float dist = Vector2.Distance(transform.position, targetPlayer.position);

            if (dist > atkDistince)
            {
                line.enabled = false;
                blinking = false;
            }

            if (dist < atkDistince || isAttacking)
            {
                if (dist < atkDistince)
                {
                    Vector2 origin = enemyTransform.position;
                    Vector2 dir = (targetAimPoint.position - enemyTransform.position).normalized;
                    float rayDistance = Vector2.Distance(origin, targetAimPoint.position);

                    int mask = ~LayerMask.GetMask("Player", "Enemy");

                    RaycastHit2D hit = Physics2D.Raycast(
                        origin,
                        dir,
                        rayDistance,
                        mask
                    );

                    line.SetPosition(0, origin);

                    if (hit.collider != null)
                        line.SetPosition(1, hit.point);
                    else
                        line.SetPosition(1, targetAimPoint.position);
                }

                detectTimer += Time.deltaTime;

                if (detectTimer < detectDelay)
                {
                    float remain = detectDelay - detectTimer;

                    if (remain > 1f)
                    {
                        line.enabled = true;
                        blinking = false;
                    }
                    else
                        blinking = true;

                    CreateAiming(targetPlayer);
                }
                else
                {
                    line.enabled = false;
                    blinking = false;

                    isAttacking = true;
                    attackWindowTimer += Time.deltaTime;

                    if (currentTime <= 0)
                    {
                        GameManager.Instance.poolManager.SpawnFromPool("Bullet", transform.position + bulletPos, transform.rotation);
                        currentTime = coolTime;
                        RemoveAiming();
                    }

                    if (attackWindowTimer >= attackWindowTime)
                    {
                        currentTime = coolTime;
                        ResetAttackState();
                    }
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, Time.deltaTime * speed);
            }

            if (currentTime > 0)
                currentTime -= Time.deltaTime;
        }
        else
        {
            curTime += Time.deltaTime;

            if (curTime >= maxTime)
                transform.position = Vector3.MoveTowards(transform.position, startPos, Time.deltaTime * speed);
        }

        if (blinking)
        {
            line.enabled = Mathf.FloorToInt(Time.time * blinkSpeed) % 2 == 0;
            line.startColor = Color.red;
            line.endColor = Color.red;
        }

        if (raycast.collider == null && targetPlayer == null && curTime >= maxTime)
            moveDirection *= -1;

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
        isAttacking = false;
    }

    void CreateAiming(Transform player)
    {
        if (currentAiming == null && targetAimPoint != null)
        {
            currentAiming = Instantiate(aiming);
            currentAiming.transform.SetParent(targetAimPoint);
            currentAiming.transform.localPosition = Vector3.zero;
            currentAiming.transform.localRotation = Quaternion.identity;
        }
    }

    void RemoveAiming()
    {
        if (currentAiming != null)
        {
            Destroy(currentAiming);
            currentAiming = null;
        }
    }

    private void OnDisable()
    {
        RemoveAiming();
        targetPlayer = null;
        targetAimPoint = null;
        isAttacking = false;
    }

    private void OnDestroy()
    {
        RemoveAiming();
        targetPlayer = null;
        targetAimPoint = null;
        isAttacking = false;
    }
}
