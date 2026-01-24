using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 8f;
    public float lifeTime = 2f;

    private Vector2 moveDir;

    void OnEnable()
    {
        // 발사 순간 플레이어 방향 고정
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            moveDir = (player.transform.position - transform.position).normalized;
        else
            moveDir = transform.right;

        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 생존 시간 후 풀로 반환
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    void OnDisable()
    {
        // 풀에서 다시 꺼낼 때 중복 Invoke 방지
        CancelInvoke();
    }

    void Update()
    {
        transform.Translate(moveDir * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어 피격!");
            // TODO : 데미지 처리
            GameManager.Instance.playerController.TakeDamage(1);
            ReturnToPool();
        }

        if (!other.isTrigger && !other.CompareTag("Player") && !other.CompareTag("Enemy"))
        {
            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        GameManager.Instance.poolManager.ReturnToPool(gameObject);
    }
}
