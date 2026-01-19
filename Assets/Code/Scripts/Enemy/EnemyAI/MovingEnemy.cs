using UnityEngine;

public class MovingEnemy : MonoBehaviour
{
    public float speed;
    bool isLeft = true;

	private void Start()
	{
		if(speed == 0) speed = 3;	// ±âº»°ª: 3
	}
	void Update()
    {
        transform.Translate(Vector2.left * speed * Time.deltaTime);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "EndPoint")
        {
            if (isLeft)
            {
                transform.eulerAngles = new Vector3(0, 180, 0);
                isLeft = false;
            }
            else 
            {
                transform.eulerAngles = new Vector3(0, 0, 0);
                isLeft = true;
            }
        }
    }
}
