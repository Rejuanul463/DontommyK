using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimatorTriggerTester : MonoBehaviour
{
    [Header("Animator Controller")]
    [SerializeField] private Animator animator;

    [Header("Animation Triggers")]
    [SerializeField] private string hiTrigger = "HI";
    [SerializeField] private string watchTrigger = "Watch";
    [SerializeField] private string tslapTrigger = "Tslap";
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private string yesTrigger = "yes";
    [SerializeField] private string noTrigger = "No";

    public void PlayHI()
    {
        TriggerAnimation(hiTrigger);
    }

    public void PlayWatch()
    {
        TriggerAnimation(watchTrigger);
    }

    public void PlayTslap()
    {
        TriggerAnimation(tslapTrigger);
    }

    public void PlayIdle()
    {
        TriggerAnimation(idleTrigger);
    }

    public void PlayYes()
    {
        TriggerAnimation(yesTrigger);
    }

    public void PlayNo()
    {
        TriggerAnimation(noTrigger);
    }

    private void TriggerAnimation(string triggerName)
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator is not assigned.");
            return;
        }

        animator.ResetTrigger(hiTrigger);
        animator.ResetTrigger(watchTrigger);
        animator.ResetTrigger(tslapTrigger);
        animator.ResetTrigger(idleTrigger);
        animator.ResetTrigger(yesTrigger);
        animator.ResetTrigger(noTrigger);

        animator.SetTrigger(triggerName);

        Debug.Log($"Animation Triggered: {triggerName}");
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(AnimatorTriggerTester))]
public class AnimatorTriggerTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AnimatorTriggerTester controller =
            (AnimatorTriggerTester)target;

        // Draw normal inspector fields
        DrawDefaultInspector();

        EditorGUILayout.Space(15);

        EditorGUILayout.LabelField(
            "Animation Controls",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(5);

        // HI
        if (GUILayout.Button("HI", GUILayout.Height(35)))
        {
            controller.PlayHI();
        }

        // Watch
        if (GUILayout.Button("Watch", GUILayout.Height(35)))
        {
            controller.PlayWatch();
        }

        // Tslap
        if (GUILayout.Button("Tslap", GUILayout.Height(35)))
        {
            controller.PlayTslap();
        }

        // Idle
        if (GUILayout.Button("Idle", GUILayout.Height(35)))
        {
            controller.PlayIdle();
        }

        // Yes
        if (GUILayout.Button("Yes", GUILayout.Height(35)))
        {
            controller.PlayYes();
        }

        // No
        if (GUILayout.Button("No", GUILayout.Height(35)))
        {
            controller.PlayNo();
        }
    }
}

#endif