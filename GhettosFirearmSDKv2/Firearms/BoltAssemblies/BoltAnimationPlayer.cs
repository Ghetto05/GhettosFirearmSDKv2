using UnityEngine;

namespace GhettosFirearmSDKv2;

[AddComponentMenu("Firearm SDK v2/Bolt assemblies/Animation player")]
public class BoltAnimationPlayer : MonoBehaviour
{
    public BoltBase bolt;
    public Animator animator;
    public string timeParameterName;

    public void FixedUpdate()
    {
        if (bolt == null || animator == null) return;

        animator.SetFloat(timeParameterName, 0.999f * bolt.cyclePercentage);
    }
}