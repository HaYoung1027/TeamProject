using UnityEngine;

public class LongRangeEnemy : MonoBehaviour
{
    [Header("감지 거리")]
    public float distance;
    [Header("조준 거리")]
    public float atkDistince;
    [Header("감지 대상 레이어 (보통 Player 레이어)")]
    public LayerMask isLayer;
    [Header("이동 속도")]
    public float speed;
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
    public bool isGrabbed = false;
    bool facingRight = true;

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
        HandleRaycast();
        HandleFlip();
        HandleGrabbed();
        HandleAttack();
    }

    public void HandleRaycast() // Raycast 설정
    {
        Vector2 rightDir = Vector2.right;
        Vector2 leftDir = Vector2.left;

        RaycastHit2D hitRight = Physics2D.CapsuleCast(
            transform.position,
            new Vector2(3f, 16f),
            CapsuleDirection2D.Vertical,
            0f,
            rightDir,
            distance,
            isLayer
        );

        RaycastHit2D hitLeft = Physics2D.CapsuleCast(
            transform.position,
            new Vector2(3f, 16f),
            CapsuleDirection2D.Vertical,
            0f,
            leftDir,
            distance,
            isLayer
        );

        RaycastHit2D raycast = hitRight.collider != null ? hitRight : hitLeft;

        if (raycast.collider != null && targetPlayer == null)
        {
            curTime = 0;
            targetPlayer = raycast.collider.transform;
            targetAimPoint = targetPlayer.Find("AimPoint");
        }

        if (raycast.collider == null && targetPlayer == null && curTime >= maxTime)
            moveDirection *= -1;

        HandleDrawRaycast();
    }
    public void HandleDrawRaycast() // Raycast 시각화
    {
        Vector3 upOffset = Vector3.up;
        Vector3 downOffset = Vector3.down;
        Debug.DrawRay(transform.position + upOffset, Vector2.right * distance, Color.red);
        Debug.DrawRay(transform.position + downOffset, Vector2.right * distance, Color.red);
        Debug.DrawRay(transform.position + upOffset, Vector2.left * distance, Color.blue);
        Debug.DrawRay(transform.position + downOffset, Vector2.left * distance, Color.blue);
    }
    void HandleFlip()
    {
        if (targetPlayer == null) return;

        float dir = targetPlayer.position.x - transform.position.x;

        if (dir < 0 && !facingRight)
            Flip();
        else if (dir > 0 && facingRight)
            Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

	public void HandleGrabbed()
	{
		if (isGrabbed)
		{
			ResetAttackState();

			targetPlayer = null;
			targetAimPoint = null;

			return;
		}
	}

	public void HandleAttack()
    {
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
                        GameManager.Instance.audioManager.EnemyShootSound(1f);      // 발사 사운드 재생
                        GameManager.Instance.poolManager.SpawnFromPool("Bullet", transform.position + bulletPos, transform.rotation);   // Bullet 소환
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
    }

    public void ResetAttackState()
    {
        detectTimer = 0f;
        attackWindowTimer = 0f;
        blinking = false;
        line.enabled = false;
        RemoveAiming();
        isAttacking = false;
    }

    public void ResetAfterThrown()
    {
        isGrabbed = false;
        isAttacking = false;

        // 타겟을 null로 만들면 적이 다시 플레이어를 감지(Raycast)해야 공격을 시작합니다.
        // 만약 던져지자마자 다시 공격하게 하고 싶다면 targetPlayer는 유지하는게 좋습니다.
        targetPlayer = null;
        targetAimPoint = null;

        detectTimer = 0f;
        attackWindowTimer = 0f;
        currentTime = 0f; // 즉시 발사 가능하도록 0으로 설정
        curTime = 0f;     // 복귀 타이머 초기화

        blinking = false;
        if (line != null)
            line.enabled = false;

        RemoveAiming();
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
