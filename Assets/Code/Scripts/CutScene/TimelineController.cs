using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class TimelineController : MonoBehaviour
{
    public static bool isTimelinePlaying;   // 타임라인이 재생중인지 확인하는 변수

    PlayableDirector director;  // 현재 오브젝트에 붙어있는 PlayableDirector 컴포넌트
    public CinemachineBrain brain;  // 씬의 CinemachineBrain 컴포넌트
    public GameObject[] objectsToEnable;    // 타임라인 종료 후 활성화할 오브젝트들
    public Slider skipSlider;   // UI 슬라이더


    public float skipHoldTime ; // 스킵을 위해 키를 누르고 있어야 하는 시간
    float holdTimer = 0f;       // 키를 누르고 있는 누적 시간
    bool fastForward = false;   // 스킵 상태인지 여부

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
        if (skipSlider != null)
            skipSlider.gameObject.SetActive(false); // 시작할 때 게이지바 숨김
    }

    void Update()
    {
        if (!isTimelinePlaying) return; // 타임라인이 재생 중일 때만 스킵 입력 처리

        HandleSkipInput();  // 키 입력 및 UI 처리

        if (fastForward)
        {
            SkipTimelineInstant();  // 스킵 처리
        }
    }

    // 키 입력 및 UI 처리
    void HandleSkipInput()
    {
        if (Keyboard.current.qKey.isPressed)
        {
            holdTimer += Time.deltaTime;    // 키 누르는 시간 누적

            if (skipSlider != null && !skipSlider.gameObject.activeSelf)
                skipSlider.gameObject.SetActive(true);  // 게이지바 보이기

            if (skipSlider != null)
                skipSlider.value = holdTimer / skipHoldTime;    // 게이지바 채우기

            if (!fastForward && holdTimer >= skipHoldTime)  // 일정시간 누르면
            {
                fastForward = true; // 스킵 상태로 전환
            }
        }
        else
        {
            holdTimer = 0f; // 키 누른 시간 초기화

            if (skipSlider != null)
            {
                skipSlider.gameObject.SetActive(false); // 게이지바 숨기기
                skipSlider.value = 0f;  // 게이지바 초기화
            }
        }
    }

    // 스킵 처리
    void SkipTimelineInstant()
    {
        fastForward = false; // 여러 번 실행 방지

        brain.enabled = false;  // 카메라 고정

        director.time = director.duration; // 바로 타임라인 끝으로 이동
        director.Evaluate();               // 타임라인 내부 오브젝트/이벤트 적용

        brain.enabled = true;  // 카메라 다시 활성화

        director.Stop(); // 타임라인 종료되게
    }


    void OnEnable()
    {
        director.played += OnTimelineStart;
        director.stopped += OnTimelineEnd;
    }

    void OnDisable()
    {
        director.played -= OnTimelineStart;
        director.stopped -= OnTimelineEnd;
    }

    void OnTimelineStart(PlayableDirector d)
    {
        isTimelinePlaying = true;   // 타임라인 재생중으로 변경

        fastForward = false;    // 스킵 상태 초기화
        holdTimer = 0f;         // 키 누른 시간 초기화
    }

    void OnTimelineEnd(PlayableDirector d)
    {
        isTimelinePlaying = false;  // 타임라인 재생중 아님으로 변경

        // 스킵 UI 숨기기, 초기화
        if (skipSlider != null)
        {
            skipSlider.gameObject.SetActive(false);
            skipSlider.value = 0f;
        }

        // 타임라인 끝나면 오브젝트들 활성화
        if (objectsToEnable != null)
        {
            foreach (GameObject obj in objectsToEnable)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
    }
}
