using UnityEngine;

public class ActivateAllAnimations : MonoBehaviour
{
    private Animator[] animators;

    void Start()
    {
        animators = GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            animator.enabled = true;
            animator.Play(0);
        }
    }
}