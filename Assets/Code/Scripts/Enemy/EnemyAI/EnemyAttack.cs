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

    public GameObject aiming;          // 조준 프리팹
    private GameObject currentAiming;  // 현재 조준 오브젝트
    private Transform targetPlayer;

    private int moveDirection = 1; // 1: 오른쪽, -1: 왼쪽

    private void Start()
    {
        startPos = transform.position;
    }

    private void FixedUpdate()
    {
        Vector2 rayDir = transform.right * moveDirection;

        RaycastHit2D raycast = Physics2D.BoxCast(
            transform.position,
            new Vector2(1f, 2f),
            0f,
            rayDir,
            distance,
            isLayer
        );

        if (raycast.collider != null) // 플레이어 감지
        {
            curTime = 0;
            Transform player = raycast.collider.transform;

            float dist = Vector2.Distance(transform.position, player.position);

            if (dist < atkDistince)
            {
                // 조준 상태
                CreateAiming(player);

                if (currentTime <= 0)
                {
                    Instantiate(bullet, transform.position + bulletPos, transform.rotation);
                    currentTime = coolTime;

                    // 발사 후 조준 제거
                    RemoveAiming();
                }
            }
            else
            {
                // 추적 상태 (조준 X)
                RemoveAiming();
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
            RemoveAiming();

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

        // 순찰 방향 전환
        if (raycast.collider == null && curTime >= maxTime)
        {
            moveDirection *= -1;
        }

        // Debug
        Debug.DrawRay(transform.position + Vector3.up, rayDir * distance, Color.red);
        Debug.DrawRay(transform.position - Vector3.up, rayDir * distance, Color.red);
    }

    // 조준 생성
    void CreateAiming(Transform player)
    {
        if (currentAiming != null)
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
