using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleton<GameManager> // 싱글톤 사용
{
    [Header("Manager 관련 코드")]
    public AudioManager audioManager;
    public PoolManager poolManager;
	public DialogSystem dialogSystem;

    [Header("카메라 관련 코드")]
    public CameraShake cameraShake;

    [Header("컨트롤러")]
    public PlayerController playerController;
    public GrapplingHook grapplingHook;

    [Header("스탯")]
    public PlayerStats playerStats;
    public EnemyStats enemyStats;

    [Header("게임 실행 중 플레이어 스텟값 수정")]
    public PlayerStatsRuntime playerStatsRuntime;
    public EnemyStatsRuntime enemyStatsRuntime;

    [Header("메뉴 관련")]
    public ESCKey escKey;

    protected new void Awake()
    {
        QualitySettings.vSyncCount = 0; // VSync 비활성화 (모니터 주사율 영향 제거)

        Application.targetFrameRate = 120; // 프레임 120 제한
        if (Instance != null && Instance != this) // 중복 GameManager 방지
        {
            Destroy(gameObject);
            return;
        }

        base.Awake(); // MonoSingleton의 Awake 호출

        // 플레이어 초기화
        if (playerStatsRuntime != null)
        {
            playerStatsRuntime = new PlayerStatsRuntime(playerStats);   // 스탯 값 복제
        }
    }
}

//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class GameManager : MonoSingleton<GameManager> // 싱글톤 사용
//{
//    [Header("Manager 관련 코드")]
//    public AudioManager audioManager;
//    public PoolManager poolManager;

//    [Header("카메라 관련 코드")]
//    public CameraShake cameraShake;

//    [Header("컨트롤러")]
//    public PlayerController playerController;

//    [Header("스탯")]
//    public PlayerStats playerStats;
//    public EnemyStats enemyStats;

//    [Header("게임 실행 중 플레이어 스텟값 수정")]
//    public PlayerStatsRuntime playerStatsRuntime;
//    public EnemyStatsRuntime enemyStatsRuntime;

//    [Header("메뉴 관련")]
//    public ESCKey escKey;

//    protected new void Awake()
//    {

//        if (Instance != null && Instance != this) // 중복 GameManager 방지
//        {
//            Destroy(gameObject);
//            return;
//        }

//        base.Awake(); // MonoSingleton의 Awake 호출
//        DontDestroyOnLoad(gameObject);

//        QualitySettings.vSyncCount = 0; // VSync 비활성화 (모니터 주사율 영향 제거)
//        Application.targetFrameRate = 120; // 프레임 120 제한

//        // 씬 로드 이벤트 등록
//        SceneManager.sceneLoaded += OnSceneLoaded;

//        // 최초 씬에서도 자동 할당
//        AutoAssign();
//        InitStats();
//    }

//    void OnDestroy()
//    {
//        SceneManager.sceneLoaded -= OnSceneLoaded;
//    }

//    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//    {
//        AutoAssign();
//    }
//    void AutoAssign()
//    {
//        audioManager ??= Object.FindFirstObjectByType<AudioManager>();
//        poolManager ??= Object.FindFirstObjectByType<PoolManager>();
//        cameraShake ??= Object.FindFirstObjectByType<CameraShake>();
//        playerController ??= Object.FindFirstObjectByType<PlayerController>();
//        escKey ??= Object.FindFirstObjectByType<ESCKey>();

//        // ScriptableObject는 Resources에서
//        if (playerStats == null)
//            playerStats = Resources.Load<PlayerStats>("PlayerStats");

//        if (enemyStats == null)
//            enemyStats = Resources.Load<EnemyStats>("EnemyStats");

//        Debug.Log("GameManager AutoAssign 완료");
//    }

//    void InitStats()
//    {
//        if (playerStats != null && playerStatsRuntime == null)
//            playerStatsRuntime = new PlayerStatsRuntime(playerStats);

//        if (enemyStats != null && enemyStatsRuntime == null)
//            enemyStatsRuntime = new EnemyStatsRuntime(enemyStats);
//    }
//}

