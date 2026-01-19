using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{

    private static T _instance; // 싱글톤 인스턴스 (게임 전체에 단 하나만 존재)
    private static object _lock = new object(); // 멀티 스레드 환경에서 동시에 Instance 접근 시 충돌 방지용

    public static T Instance
    {
        get
        {
            if (_instance == null) // 인스턴스가 아직 만들어지지 않았다면
            {
                lock (_lock) // 동시에 여러 곳에서 접근하는 것을 막기 위해 lock 사용
                {
                    _instance = (T)FindAnyObjectByType(typeof(T)); // 현재 씬에 이미 존재하는 객체를 탐색
                    
                    if (_instance == null) // 씬에도 없다면 완전히 새로 생성
                    {
                        GameObject singletonGO = new GameObject(typeof(T).Name); // 타입 이름을 가진 빈 GameObject 생성
                        _instance = singletonGO.AddComponent<T>(); // 해당 타입(T) 컴포넌트 추가
                        DontDestroyOnLoad(singletonGO); // 씬 전환 시 삭제되지 않도록 설정
                    }
                }
            }
            return _instance; // 항상 하나뿐인 인스턴스를 반환
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null) // 아직 싱글톤 인스턴스가 없다면
        {
            _instance = (T)this; // 자기 자신을 싱글톤 인스턴스로 등록
            DontDestroyOnLoad(this.gameObject); // 씬이 바뀌어도 파괴되지 않게 설정
        }
        else if (_instance != this) Destroy(gameObject);// 이미 다른 인스턴스가 존재한다면 자신은 필요 없으므로 제거
    }
}