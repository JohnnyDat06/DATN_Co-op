using UnityEngine;
using UnityEngine.Animations;

public class AttackReset : StateMachineBehaviour
{
    [SerializeField] private string triggerName;
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
    {
        animator.ResetTrigger(triggerName);
    }
}
