using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector3 startPos;
    public Vector3 firstPos;
    public Vector3 secondPos;
    public float moveSpeed;

    void FixedUpdate()
    {
        transform.position = Vector2.MoveTowards(transform.position, firstPos, Time.deltaTime * moveSpeed);

        if (Vector2.Distance(transform.position, firstPos) <= 0.05f) // 거리가 0.05 이하일 때 목적지 변경
        {
            if (firstPos == secondPos) // firstPos가 secondPos랑 같다면
            {
                firstPos = startPos; // firstPos는 startPos로 변경
            }
            else
            {
                firstPos = secondPos; // firstPos는 secondPos로 변경
            }
        }    
    }
    private void OnCollisionEnter2D(Collision2D collision) // 접촉하는 순간
    {
        if (collision.transform.CompareTag("Player"))
        {
            collision.transform.SetParent(transform); // 자식에 추가
        }
    }
    private void OnCollisionExit2D(Collision2D collision) // 접촉이 끊기는 순간
    {
        if (collision.transform.CompareTag("Player"))
        {
            collision.transform.SetParent(null); // 자식에서 해제
        }
    }
}
