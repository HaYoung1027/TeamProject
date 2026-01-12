using UnityEngine;

// 잡고 던질 수 있는 요소에 대한 인터페이스
public interface IGrabbable
{
	public void Attach(Transform element);	// 잡기
	public void Throw(Vector2 throwDir);	// 던지기
	public void MovePos();	// 위치 이동하기
}
