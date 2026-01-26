using UnityEngine;
using System.Collections.Generic;

public class TestHooking : MonoBehaviour
{
	public Vector2 destiny;
	public float speed = 1f;		// TODO: 스크립터블 오브젝트에 있는 speed로 사용하기
	public float distance = 2f;		// TODO: 스크립터블 오브젝트에 있는 값으로 사용하기
	bool isAttach = false;          // 플레이어가 갈고리를 사용했는지

	[Header("노드 프리펩")] public GameObject nodePrefab;   // 노드 프리펩
	[HideInInspector] public GameObject player;				// 플레이어 오브젝트
	[HideInInspector] public GameObject lastNode;           // 마지막에 생성한 노드

	public LineRenderer line;

	int vertexCnt = 2;				// 점 갯수
	public List<GameObject> nodeList = new List<GameObject>();	// 노드 리스트

	private void Start()
	{
		line = GetComponent<LineRenderer>();
		player = GameObject.FindGameObjectWithTag("Player");	// 플레이어 태그로 정보 불러오기
		lastNode = transform.gameObject;    // 마지막 태그를 자기 자신으로 설정
		nodeList.Add(transform.gameObject);	// 첫 번째 노드로 훅 추가
	}

	private void Update()
	{
		transform.position = Vector2.MoveTowards(transform.position, destiny, speed);

		// 훅이 이동중인지 확인
		if ((Vector2)transform.position != destiny)
		{
			// 두 벡터 사이의 거리 확인
			// 위에서 지정한 거리보다 클 경우 노드 생성
			if (Vector2.Distance(player.transform.position, lastNode.transform.position) > distance)
			{
				CreateNode();
			}
		}
		else if (!isAttach) // 갈고리를 사용하지 않을 경우
		{
			isAttach = true;    // 사용 중으로 변경

			HingeJoint2D lastNodeJoint = lastNode.GetComponent<HingeJoint2D>();
			lastNodeJoint.connectedBody = player.GetComponent<Rigidbody2D>();   // 마지막 노드를 플레이어와 연결 (플레이어가 매달리도록)
			lastNodeJoint.autoConfigureConnectedAnchor = false;		// 마지막 노드는 플레이어와 자연스럽게 이어지도록 자동 앵커 해제
			lastNodeJoint.anchor = Vector2.zero;					// 앵커 좌표도 모두 0으로
		}

		RenderLine();
	}

	// 선 그리기
	void RenderLine()
	{
		line.positionCount = vertexCnt;     // 라인렌더러 갯수를 점 갯수만큼 설정

		int i;
		for (i = 0; i < nodeList.Count; i++)
		{
			line.SetPosition(i, nodeList[i].transform.position);
		}

		line.SetPosition(i, player.transform.position);		// 마지막 라인렌더러는 플레이어 위치로 이동
	}

	void CreateNode()
	{
		Vector2 createPos = player.transform.position - lastNode.transform.position;    // 노드 생성 위치
		createPos.Normalize();  // 위치 벡터 정규화
		createPos *= distance;  // 거리 곱하기
		createPos += (Vector2)lastNode.transform.position;

		GameObject tempNode = (GameObject)Instantiate(nodePrefab, createPos, Quaternion.identity);  // 임시 노드 생성

		tempNode.transform.SetParent(transform);

		HingeJoint2D lastNodeJoint = lastNode.GetComponent<HingeJoint2D>();
		lastNodeJoint.autoConfigureConnectedAnchor = true;					// 자동 앵커 조절 활성화
		lastNodeJoint.connectedBody = tempNode.GetComponent<Rigidbody2D>(); // 마지막 노드를 현재 노드 HingeJoint와 연결
		lastNode = tempNode;    // 임시 노드를 마지막 노드로 설정
		nodeList.Add(lastNode); // 노드 리스트에 추가

		++vertexCnt;	// 점 갯수 증가
	}
}
