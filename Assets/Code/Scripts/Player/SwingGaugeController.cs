using UnityEngine;
using UnityEngine.UI;

// 플레이어가 갈고리에 매달려 회전할 때 게이지를 조절하는 스크립트
public class SwingGaugeController : MonoBehaviour
{
    public Slider swingGauge;               // 회전 에너지 게이지 UI

    private GrapplingHook grappling;        // GrapplingHook 스크립트 참조
    public Transform hook;                  // 갈고리의 위치(회전 중심)

    public int maxTurns = 1;                // 저장가능한 최대 회전 수(에너지 제한)
    public float decreaseSpeed = 400f;      // 회전 중이지 않을 때 게이지 감소 속도
    public float increaseMultiplier = 1.0f; // 회전할 때 회전량 증가 배율
    public float turnMinDelta = 0.3f;       // 회전으로 인정할 최소 각도 변화

    private float accumulatedAngle = 0f;    // 누적 회전량(게이지 수치)
    private float maxAngle;                 // maxTurns 회전 시 최대 각도(= 360 * maxTurns)

    private float previousAngle;            // 이전 프레임의 각도
    private bool angleInitialized = false;  // 첫 프레임 각도 초기화 여부

    private int storedDirection = 0;        // 저장된 회전 방향(1=시계, -1=반시계, 0=없음)

    PlayerController player;

    void Awake()
    {
        grappling = GetComponent<GrapplingHook>();
        player = GetComponent<PlayerController>();
        maxAngle = maxTurns * 360f;
    }

    void Update()
    {
        // 갈고리에 붙어 있을 때만 게이지 처리
        if (grappling.isAttach)
        {
            bool noInput = player.inputVec == Vector2.zero;

            if(swingGauge.value > 0.25f)
                swingGauge.gameObject.SetActive(true);  // 게이지 UI 활성화

            Vector2 hookPos = hook.position;        // 갈고리(회전 중심) 좌표
            Vector2 playerPos = transform.position; // 플레이어 좌표

            Vector2 dir = (playerPos - hookPos).normalized; // 갈고리 -> 플레이어 방향 벡터

            float angleNow = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // 현재 각도(0~360°)

            // 첫 프레임에서는 이전 각도가 없으므로 초기화
            if (!angleInitialized)
            {
                previousAngle = angleNow;
                angleInitialized = true;
            }

            // 프레임 간 각도 변화 계산 (360° 넘어가도 정확하게 처리)
            float delta = Mathf.DeltaAngle(previousAngle, angleNow);
            previousAngle = angleNow; // 현재 각도를 다음 프레임을 위해 저장

            // delta가 충분히 크면 "회전 중"으로 처리
            if (Mathf.Abs(delta) > turnMinDelta)
            {
                int deltaDir = delta > 0 ? 1 : -1; // 회전 방향 판단

                // 회전 방향이 처음 결정되는 순간
                if (storedDirection == 0)
                    storedDirection = deltaDir;

                if (deltaDir == storedDirection)
                {
                    if (noInput)    // 입력 없을 때
                    {
                        accumulatedAngle += decreaseSpeed * Time.deltaTime * 0.05f;
                    }
                    else
                    {
                        // 같은 방향으로 돌면 -> 게이지 증가
                        accumulatedAngle += Mathf.Abs(delta) * increaseMultiplier;
                    }
                }
                else
                {
                    // 반대 방향으로 돌면 -> 게이지 감소
                    accumulatedAngle -= Mathf.Abs(delta) * increaseMultiplier * 1.5f;

                    // 감소하다가 0 이하가 되면 방향 초기화
                    if (accumulatedAngle <= 0f)
                    {
                        accumulatedAngle = 0;
                        storedDirection = 0;
                    }
                }
            }
            else
            {
                // 회전량 거의 없음 -> 자연 감소
                accumulatedAngle -= decreaseSpeed * Time.deltaTime;

                // 감소하다 0 이하로 떨어지면 초기화
                if (accumulatedAngle <= 0f)
                {
                    accumulatedAngle = 0;
                    storedDirection = 0;
                }
            }

            // 누적 회전량을 0 ~ maxAngle 범위에 유지
            accumulatedAngle = Mathf.Clamp(accumulatedAngle, 0, maxAngle);

            // 게이지 UI에 비율(0~1)로 표시
            swingGauge.value = accumulatedAngle / maxAngle;
        }
        else
        {
            // 갈고리에서 떨어지면 모든 값 초기화
            swingGauge.gameObject.SetActive(false); // 게이지 숨기기
            accumulatedAngle = 0f;                  // 회전량 초기화
            swingGauge.value = 0f;                  // UI 리셋
            angleInitialized = false;               // 다음 회전 때 새 초기화 필요
            storedDirection = 0;                    // 방향 초기화
        }
    }

    public float GetGaugePercent()
    {
        return accumulatedAngle / maxAngle; // 0~1
    }
}
