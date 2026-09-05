using UnityEngine;

public class PlayOnceAnimation : MonoBehaviour
{
    private static bool hasPlayed = false;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (hasPlayed)
        {
            // Animation already played, so don't play it again
            gameObject.SetActive(false);
            return;
        }

        hasPlayed = true;
    }
}
