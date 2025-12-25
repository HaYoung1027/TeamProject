using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    bool hasShakedOnAttach = false;
    bool hasPlayedAttachSound = false;
    bool isPlayedDraftSound = false;
    bool hasPlayedShootSound = false;

    // 슬로우 효과 변수
    public float slowFactor;    // 슬로우 비율
    public float slowLength;    // 원래 속도로 복귀하는 데 걸리는 시간
    Coroutine slowCoroutine;    // 슬로우 효과 코루틴

    public Vector3 enemyFollowOffset = Vector3.zero;
    private List<Transform> enemies = new List<Transform>();
    Rigidbody2D rb;
    SpriteRenderer sprite;
    DistanceJoint2D hookJoint;
    bool isStopped = false;

	PlayerController player;    // 플레이어

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
		player = GetComponent<PlayerController>();
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

		hookJoint = hook.GetComponent<DistanceJoint2D>();
	}
    void Update()
    {
        line.SetPosition(0, transform.position);
        line.SetPosition(1, hook.position);

        // 갈고리 or 적에 처음 붙었을 때
        if ((isAttach || isEnemyAttach) && !hasPlayedAttachSound)
        {
            GameManager.Instance.audioManager.HookAttachSound(1f);
            hasPlayedAttachSound = true;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && !isHookActive && !isAttach && !isEnemyAttach)
        {
            GameManager.Instance.cameraShake.ShakeForSeconds(0.1f); // 카메라 흔들기
            GameManager.Instance.audioManager.HookShootSound(0.7f); // 갈고리 발사 효과음
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
                hasPlayedShootSound = false;
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
                hasPlayedShootSound = false;
                // 훅 오브젝트 비활성화
                hook.gameObject.SetActive(false);
            }
        }

        else if (isAttach)
        {
            if (!hasShakedOnAttach)
            {
                GameManager.Instance.cameraShake.ShakeForSeconds(0.1f);
                hasShakedOnAttach = true;
            }

            // 마우스를 뗐을 때만 해제
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isAttach = false;
                isHookActive = false;
                isLineMax = false;
                hasShakedOnAttach = false;
                hasPlayedAttachSound = false;

                hook.GetComponent<Hooking>().joint2D.enabled = false;
                hook.gameObject.SetActive(false);

                if (slowCoroutine != null)
                    StopCoroutine(slowCoroutine);

                slowCoroutine = StartCoroutine(SlowRoutine());
            }

            // 우클릭 줄 당기기 로직은 그대로 OK
            if (Mouse.current.rightButton.isPressed)
            {
                if (hookJoint != null && hookJoint.enabled)
                {
                    hookJoint.distance = Mathf.Max(0.5f, hookJoint.distance - 0.1f);

                    if (!isPlayedDraftSound)
                    {
                        GameManager.Instance.audioManager.HookDraftSound(1f);
                        isPlayedDraftSound = true;
                    }
                }
            }

            if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                GameManager.Instance.audioManager.StopSFX();
                isPlayedDraftSound = false;
            }
        }

        else if (isEnemyAttach) // 적 끌고오기
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && enemies.Count > 0)
            {
                Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

                Vector2 dir = mouseWorld - (Vector2)transform.position;

                ThrowEnemy(enemies[0], dir, GameManager.Instance.playerStatsRuntime.hookEnemyThrowForce);
            }
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

        GameManager.Instance.audioManager.HookThrowEnemySound(1f); // 적 던지는 효과음
        enemies.Remove(enemy);

        // 부모 해제
        enemy.SetParent(null);

        Collider2D enemyCol = enemy.GetComponent<Collider2D>();
        Collider2D playerCol = GetComponent<Collider2D>();

        // Rigidbody 처리
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(throwDir.normalized * throwForce, ForceMode2D.Impulse);
        }

        if (enemies.Count == 0)
        {
            isEnemyAttach = false;
            hasPlayedAttachSound = false;
        }


        line.enabled = true;

        // 훅 상태 초기화
        isHookActive = false;
        isLineMax = false;
        hook.GetComponent<Hooking>().joint2D.enabled = false;
        hook.gameObject.SetActive(false);
    }

	// 슬로우 효과 코루틴
	IEnumerator SlowRoutine()
	{
		// 슬로우 적용
		sprite.color = Color.red;
		Time.timeScale = slowFactor;
		Time.fixedDeltaTime = 0.02f * Time.timeScale;

		float elapsed = 0f;

		while (elapsed < slowLength)
		{
			// 플레이어가 땅에 닿거나 그래플링 훅을 다시 사용하거나 몬스터를 잡을 경우 즉시 종료
			if (player.isGrounded || isAttach || isEnemyAttach)
			{
				Debug.Log("슬로우모션 종료");
				Debug.Log("isGrounded: " + player.isGrounded + ", isAttach: " + isAttach + ", isEnemyAttach: " + isEnemyAttach);
				break;
			}

			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}

		// 복구
		Time.timeScale = 1f;
		Time.fixedDeltaTime = 0.02f;
		sprite.color = Color.white;
	}

}