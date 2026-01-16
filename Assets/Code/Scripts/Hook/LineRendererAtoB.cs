using UnityEngine;

public class LineRendererAtoB : MonoBehaviour
{
	LineRenderer lineRenderer;

	private void Awake()
	{
		lineRenderer = GetComponent<LineRenderer>();

		lineRenderer.positionCount = 2;     // 그리는 점의 갯수
		lineRenderer.enabled = true;
	}

	// 선 색상 변경함수 (단색)
	public void SetLineColor(Color color)
	{
		lineRenderer.startColor = color;
		lineRenderer.endColor = color;
	}
	// 선 색상 변경함수 (그라데이션)
	public void SetLineColor(Color startColor, Color endColor)
	{
		lineRenderer.startColor = startColor;
		lineRenderer.endColor = endColor;
	}

	// 라인 렌더러 그리기
	public void Play(Vector3 from, Vector3 to)
	{
		lineRenderer.enabled = true;

		lineRenderer.SetPosition(0, from);
		lineRenderer.SetPosition(1, to);
	}

	// 라인 렌더러 숨기기
	public void Stop()
	{
		lineRenderer.enabled = false;
	}
}
