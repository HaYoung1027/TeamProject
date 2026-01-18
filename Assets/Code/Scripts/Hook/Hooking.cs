using UnityEngine;

using tagName = Globals.TagName;

public class Hooking : MonoBehaviour
{
    [Header("갈고리 사이 거리가 이하이면 보정")]
    public float minDistanceLimit;
    [Header("가까울 때 고정되는 거리")]
    public float minClampDistance;

    public DistanceJoint2D joint2D; // 플레이어와 갈고리를 물리적으로 연결하는 DistanceJoint2D

    private Transform hookedEnemy;
    [Header("훅 최소 길이")]
    public float minHookLength = 2.0f;

    void Start()
    {
        joint2D = GetComponent<DistanceJoint2D>();                                 // 현재 오브젝트에 붙어있는 DistanceJoint2D 가져오기
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(tagName.ground)) // 갈고리가 특정 태그에 닿았을 때
        {
            joint2D.enabled = true; // 줄 활성화

            // 플레이어가 갈고리를 건 위치가 Joint DIstance의 Distance
            float dist = Vector2.Distance(GameManager.Instance.grapplingHook.transform.position, transform.position);    // 플레이어와 갈고리 사이 거리 계산
            joint2D.distance = dist;                                                            // 계산된 거리를 Joint의 거리로 설정

            if (GameManager.Instance.playerController.isGrounded == true)                       // 플레이어가 땅에 붙어 있을 경우
            {
                joint2D.distance -= joint2D.distance * 0.2f;                                    // 줄이 너무 팽팽해지지 않도록 살짝 줄여줌
            }

            if (joint2D.distance >= 9)    // 줄 길이가 너무 길 경우 제한
            {
                joint2D.distance = 7;
            }

            if (!GameManager.Instance.playerController.isGrounded && joint2D.distance <= minDistanceLimit) // 짧을 때 늘리기
            {
                joint2D.distance = minClampDistance;
            }

            GameManager.Instance.grapplingHook.ApplyHookImpulse(transform.position);    // 힘 주기
            GameManager.Instance.grapplingHook.isAttach = true;                         // 훅이 연결된 상태
            GameManager.Instance.grapplingHook.isHookActive = false;                    // 훅 발사 상태 종료
            GameManager.Instance.grapplingHook.isLineMax = false;                       // 줄 최대 길이 상태 해제

            // 갈고리가 벽에 박히는 순간 벽과 플레이어 거리가 너무 가깝다면 길이 보정
            if (joint2D.distance < minHookLength)
                joint2D.distance = minHookLength;
        }

        // 몬스터/오브젝트 잡기
        if (collision.CompareTag(tagName.enemy) || collision.CompareTag(tagName.throwingEnemy) || collision.CompareTag(tagName.obj))
        {
            GameManager.Instance.grapplingHook.AttachElement(collision.transform);
		}
	}
}
