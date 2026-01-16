using UnityEngine;

using tagName = Globals.TagName;

public class Hooking : MonoBehaviour
{
    [Header("이하이면 보정")]
    public float minDistanceLimit;
    [Header("가까울 때 고정되는 거리")]
    public float minClampDistance;

    GrapplingHook grappling;
    public DistanceJoint2D joint2D;

    private Transform hookedEnemy;

    public float minHookLength = 2.0f;   // 최소 그래플 길이

    void Start()
    {
        grappling = GameObject.Find(tagName.player).GetComponent<GrapplingHook>();
        joint2D = GetComponent<DistanceJoint2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(tagName.ground))
        {
            joint2D.enabled = true;

            // 플레이어가 갈고리를 건 위치가 Joint DIstance의 Distance
            float dist = Vector2.Distance(grappling.transform.position, transform.position);
            joint2D.distance = dist;

            if (GameManager.Instance.playerController.isGrounded == true) // 땅에 닿았을 때 줄이기
            {
                joint2D.distance -= joint2D.distance * 0.2f;
            }

            if (joint2D.distance >= 9)
            {
                joint2D.distance = 7;
            }

            if (!GameManager.Instance.playerController.isGrounded && joint2D.distance <= minDistanceLimit) // 짧을 때 늘리기
            {
                joint2D.distance = minClampDistance;
            }

            grappling.ApplyHookImpulse(transform.position); // 힘 주기

            grappling.isAttach = true;
            grappling.isHookActive = false;
            grappling.isLineMax = false;

            // 갈고리가 벽에 박히는 순간 벽과 플레이어 거리가 너무 가깝다면 길이 보정
            if (joint2D.distance < minHookLength)
                joint2D.distance = minHookLength;
        }

        // 몬스터/오브젝트 잡기
        if (collision.CompareTag(tagName.enemy) || collision.CompareTag(tagName.throwingEnemy) || collision.CompareTag(tagName.obj))
        {
            grappling.AttachElement(collision.transform);
		}
	}
}
