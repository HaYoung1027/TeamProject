using UnityEngine;

public class ObjectController : MonoBehaviour
{
	GrabbableObject obj;

	private void Awake()
	{
		obj = GetComponent<GrabbableObject>();
	}
}
