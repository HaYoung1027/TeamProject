using UnityEngine;
using UnityEngine.Playables;

public class TimelineController : MonoBehaviour
{
    public static bool isTimelinePlaying;

    PlayableDirector director;

    public GameObject[] objectsToEnable;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
    }

    void OnEnable()
    {
        director.played += OnTimelineStart;
        director.stopped += OnTimelineEnd;
    }

    void OnDisable()
    {
        director.played -= OnTimelineStart;
        director.stopped -= OnTimelineEnd;
    }

    void OnTimelineStart(PlayableDirector d)
    {
        isTimelinePlaying = true;
    }

    void OnTimelineEnd(PlayableDirector d)
    {
        isTimelinePlaying = false;

        if (objectsToEnable != null)
        {
            foreach (GameObject obj in objectsToEnable)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
    }
}
