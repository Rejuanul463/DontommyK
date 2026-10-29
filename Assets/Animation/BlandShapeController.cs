using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls SkinnedMeshRenderer blendshapes (facial expressions) on character models like DOnt_model.
/// Supports control via:
/// 1. Direct Animation Clip curves (keyframing properties in the Animation window)
/// 2. Animator Controller parameters (Float parameters auto-synced each frame)
/// 3. Animation Events & public script API (e.g. SetSmile(100), ResetAllBlendShapes())
/// </summary>
[DisallowMultipleComponent]
public class BlandShapeController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SkinnedMeshRenderer containing blend shapes (e.g. on DOnt_model). If empty, will auto-detect.")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    [Tooltip("The Animator driving animations. If empty, will auto-detect on this object or parents.")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameter Synchronization")]
    [Tooltip("If enabled, automatically reads float parameters from the Animator with matching blendshape names (e.g. 'Smile' or 'Exp.Smile').")]
    [SerializeField] private bool syncWithAnimatorParameters = true;

    [Tooltip("Check this if your Animator float parameters range from 0.0 to 1.0 instead of 0.0 to 100.0.")]
    [SerializeField] private bool animatorParametersAreNormalized = false;

    [Header("Smoothing")]
    [Tooltip("Enable smooth transitions between expression changes.")]
    [SerializeField] private bool enableSmoothing = false;
    [Tooltip("Speed of smoothing interpolation when enabled.")]
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Auto Blink Settings")]
    [Tooltip("Enables natural automatic eye blinking at random intervals.")]
    [SerializeField] private bool autoBlink = true;
    [Tooltip("Minimum time in seconds between blinks.")]
    [SerializeField] private float minBlinkInterval = 2.0f;
    [Tooltip("Maximum time in seconds between blinks.")]
    [SerializeField] private float maxBlinkInterval = 4.5f;
    [Tooltip("Duration of a single blink in seconds.")]
    [SerializeField] private float blinkDuration = 0.15f;

    [Header("Quick Expression Controls (Animatable in Animation Clips)")]
    [Range(0f, 100f)] public float sad = 0f;
    [Range(0f, 100f)] public float blink = 0f;
    [Range(0f, 100f)] public float smile = 0f;
    [Range(0f, 100f)] public float amaze = 0f;
    [Range(0f, 100f)] public float head9 = 0f;
    [Range(0f, 100f)] public float mCloth = 0f;

    // Auto-blink internal state
    private float blinkTimer = 0f;
    private float currentBlinkInterval = 3f;
    private float autoBlinkProgress = -1f; // -1 means not blinking
    private float autoBlinkWeight = 0f;

    // Cache of all blendshapes by normalized name -> blendshape index
    private readonly Dictionary<string, int> nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    // Current and target weights for all blendshapes
    private float[] currentWeights;
    private float[] targetWeights;

    // Direct expression indices (-1 if not found on mesh)
    private int indexSad = -1;
    private int indexBlink = -1;
    private int indexSmile = -1;
    private int indexAmaze = -1;
    private int indexHead9 = -1;
    private int indexMCloth = -1;

    // Animator parameter binding
    private struct AnimatorBinding
    {
        public int paramHash;
        public int blendShapeIndex;
    }
    private readonly List<AnimatorBinding> animatorBindings = new List<AnimatorBinding>();

    // Active crossfade coroutines
    private readonly Dictionary<int, Coroutine> activeFades = new Dictionary<int, Coroutine>();

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void Awake()
    {
        AutoAssignReferences();
        InitializeBlendShapes();
        CacheAnimatorBindings();
    }

    private void OnEnable()
    {
        CacheAnimatorBindings();
    }

    /// <summary>
    /// Finds SkinnedMeshRenderer and Animator if not manually assigned.
    /// </summary>
    [ContextMenu("Auto-Assign References & Refresh")]
    public void AutoAssignReferences()
    {
        if (skinnedMeshRenderer == null)
        {
            var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                if (smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
                {
                    skinnedMeshRenderer = smr;
                    break;
                }
            }

            if (skinnedMeshRenderer == null && smrs.Length > 0)
            {
                skinnedMeshRenderer = smrs[0];
            }
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInParent<Animator>();
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }

    /// <summary>
    /// Scans the mesh and caches all blendshape names and indices.
    /// </summary>
    private void InitializeBlendShapes()
    {
        nameToIndex.Clear();

        if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
        {
            Debug.LogWarning($"[BlandShapeController] No SkinnedMeshRenderer or Mesh found on '{gameObject.name}'", this);
            return;
        }

        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        int count = mesh.blendShapeCount;
        currentWeights = new float[count];
        targetWeights = new float[count];

        for (int i = 0; i < count; i++)
        {
            string rawName = mesh.GetBlendShapeName(i);
            float weight = skinnedMeshRenderer.GetBlendShapeWeight(i);
            currentWeights[i] = weight;
            targetWeights[i] = weight;

            // Register raw name (e.g. "Exp.Smile")
            nameToIndex[rawName] = i;

            // Register normalized name without prefix (e.g. "Smile", "smile")
            string cleanName = NormalizeShapeName(rawName);
            if (!nameToIndex.ContainsKey(cleanName))
            {
                nameToIndex[cleanName] = i;
            }

            // Also register sanitized alpha-numeric name
            string alphaName = cleanName.Replace("_", "").Replace(".", "").Replace(" ", "");
            if (!nameToIndex.ContainsKey(alphaName))
            {
                nameToIndex[alphaName] = i;
            }
        }

        // Cache indices for the known DOnt_model expressions
        indexSad = FindBlendShapeIndex("Sad");
        indexBlink = FindBlendShapeIndex("Blink");
        indexSmile = FindBlendShapeIndex("Smile");
        indexAmaze = FindBlendShapeIndex("amaze");
        indexHead9 = FindBlendShapeIndex("Head9");
        indexMCloth = FindBlendShapeIndex("M_cloth");
    }

    /// <summary>
    /// Matches Animator float parameters to blend shapes.
    /// </summary>
    private void CacheAnimatorBindings()
    {
        animatorBindings.Clear();

        if (animator == null || !syncWithAnimatorParameters || nameToIndex.Count == 0)
            return;

        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Float)
            {
                int index = FindBlendShapeIndex(param.name);
                if (index >= 0)
                {
                    animatorBindings.Add(new AnimatorBinding
                    {
                        paramHash = param.nameHash,
                        blendShapeIndex = index
                    });
                }
            }
        }
    }

    private void Start()
    {
        ResetBlinkTimer();
    }

    private void Update()
    {
        UpdateAutoBlink();
    }

    private void UpdateAutoBlink()
    {
        if (!autoBlink)
        {
            autoBlinkWeight = 0f;
            autoBlinkProgress = -1f;
            return;
        }

        if (autoBlinkProgress >= 0f)
        {
            // Currently executing a blink
            autoBlinkProgress += Time.deltaTime / Mathf.Max(0.01f, blinkDuration);
            if (autoBlinkProgress >= 1f)
            {
                autoBlinkProgress = -1f;
                autoBlinkWeight = 0f;
                ResetBlinkTimer();
            }
            else
            {
                // Parabolic curve: 4 * t * (1 - t) reaches 1.0 at t = 0.5
                float t = autoBlinkProgress;
                float curve = 4f * t * (1f - t);
                autoBlinkWeight = Mathf.Clamp01(curve) * 100f;
            }
        }
        else
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= currentBlinkInterval)
            {
                autoBlinkProgress = 0f;
            }
        }
    }

    private void ResetBlinkTimer()
    {
        blinkTimer = 0f;
        currentBlinkInterval = UnityEngine.Random.Range(minBlinkInterval, maxBlinkInterval);
    }

    private void OnValidate()
    {
        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
        {
            if (nameToIndex.Count == 0)
            {
                InitializeBlendShapes();
            }

            if (indexSad >= 0) skinnedMeshRenderer.SetBlendShapeWeight(indexSad, sad);
            if (indexBlink >= 0) skinnedMeshRenderer.SetBlendShapeWeight(indexBlink, blink);
            if (indexSmile >= 0) skinnedMeshRenderer.SetBlendShapeWeight(indexSmile, smile);
            if (indexAmaze >= 0) skinnedMeshRenderer.SetBlendShapeWeight(indexAmaze, amaze);
            if (indexHead9 >= 0) skinnedMeshRenderer.SetBlendShapeWeight(indexHead9, head9);
            if (indexMCloth >= 0) skinnedMeshRenderer.SetBlendShapeWeight(indexMCloth, mCloth);
        }
    }

    /// <summary>
    /// LateUpdate runs after the Animator updates and Animation clips apply their properties.
    /// </summary>
    private void LateUpdate()
    {
        if (skinnedMeshRenderer == null || currentWeights == null)
            return;

        // 1. Read direct animatable fields (Quick Expression Controls + Auto Blink)
        ApplyDirectProperty(indexSad, sad);
        ApplyDirectProperty(indexBlink, Mathf.Max(blink, autoBlinkWeight));
        ApplyDirectProperty(indexSmile, smile);
        ApplyDirectProperty(indexAmaze, amaze);
        ApplyDirectProperty(indexHead9, head9);
        ApplyDirectProperty(indexMCloth, mCloth);

        // 2. Read Animator parameters if sync is active
        if (syncWithAnimatorParameters && animator != null && animator.isActiveAndEnabled)
        {
            for (int i = 0; i < animatorBindings.Count; i++)
            {
                var binding = animatorBindings[i];
                float val = animator.GetFloat(binding.paramHash);
                if (animatorParametersAreNormalized)
                {
                    val *= 100f;
                }

                // Combine with direct property (Mathf.Max) so Quick Expression Controls are NEVER overwritten by 0!
                float directVal = targetWeights[binding.blendShapeIndex];
                targetWeights[binding.blendShapeIndex] = Mathf.Clamp(Mathf.Max(val, directVal), 0f, 100f);
            }
        }

        // 3. Apply target weights to SkinnedMeshRenderer (with optional smoothing)
        for (int i = 0; i < currentWeights.Length; i++)
        {
            if (enableSmoothing)
            {
                currentWeights[i] = Mathf.MoveTowards(currentWeights[i], targetWeights[i], smoothSpeed * 100f * Time.deltaTime);
            }
            else
            {
                currentWeights[i] = targetWeights[i];
            }

            skinnedMeshRenderer.SetBlendShapeWeight(i, currentWeights[i]);
        }
    }

    private void ApplyDirectProperty(int shapeIndex, float propertyValue)
    {
        if (shapeIndex >= 0)
        {
            // If the property has been set or animated, write to target weight
            targetWeights[shapeIndex] = propertyValue;
        }
    }

    /// <summary>
    /// Helper to find blendshape index by name, ignoring "Exp.", "exp.", or common prefixes.
    /// </summary>
    public int FindBlendShapeIndex(string shapeName)
    {
        if (string.IsNullOrEmpty(shapeName))
            return -1;

        if (nameToIndex.TryGetValue(shapeName, out int index))
            return index;

        string clean = NormalizeShapeName(shapeName);
        if (nameToIndex.TryGetValue(clean, out index))
            return index;

        string alpha = clean.Replace("_", "").Replace(".", "").Replace(" ", "");
        if (nameToIndex.TryGetValue(alpha, out index))
            return index;

        return -1;
    }

    private string NormalizeShapeName(string rawName)
    {
        if (rawName.StartsWith("Exp.", StringComparison.OrdinalIgnoreCase))
        {
            return rawName.Substring(4);
        }
        if (rawName.StartsWith("Exp_", StringComparison.OrdinalIgnoreCase))
        {
            return rawName.Substring(4);
        }
        return rawName;
    }

    #region Public API / Animation Events

    /// <summary>
    /// Sets a blendshape weight by name (0 to 100).
    /// Can be called directly from Unity Animation Events!
    /// </summary>
    public void SetBlendShapeWeight(string shapeName, float weight)
    {
        int index = FindBlendShapeIndex(shapeName);
        if (index >= 0)
        {
            weight = Mathf.Clamp(weight, 0f, 100f);
            targetWeights[index] = weight;
            UpdateFieldForIndex(index, weight);
        }
    }

    /// <summary>
    /// Set Smile weight (0 to 100). Can be called from Animation Events.
    /// </summary>
    public void SetSmile(float weight)
    {
        smile = Mathf.Clamp(weight, 0f, 100f);
        if (indexSmile >= 0) targetWeights[indexSmile] = smile;
    }

    /// <summary>
    /// Set Sad weight (0 to 100). Can be called from Animation Events.
    /// </summary>
    public void SetSad(float weight)
    {
        sad = Mathf.Clamp(weight, 0f, 100f);
        if (indexSad >= 0) targetWeights[indexSad] = sad;
    }

    /// <summary>
    /// Set Blink weight (0 to 100). Can be called from Animation Events.
    /// </summary>
    public void SetBlink(float weight)
    {
        blink = Mathf.Clamp(weight, 0f, 100f);
        if (indexBlink >= 0) targetWeights[indexBlink] = blink;
    }

    /// <summary>
    /// Set Amaze weight (0 to 100). Can be called from Animation Events.
    /// </summary>
    public void SetAmaze(float weight)
    {
        amaze = Mathf.Clamp(weight, 0f, 100f);
        if (indexAmaze >= 0) targetWeights[indexAmaze] = amaze;
    }

    /// <summary>
    /// Set Head9 weight (0 to 100). Can be called from Animation Events.
    /// </summary>
    public void SetHead9(float weight)
    {
        head9 = Mathf.Clamp(weight, 0f, 100f);
        if (indexHead9 >= 0) targetWeights[indexHead9] = head9;
    }

    /// <summary>
    /// Set M_cloth weight (0 to 100). Can be called from Animation Events.
    /// </summary>
    public void SetMCloth(float weight)
    {
        mCloth = Mathf.Clamp(weight, 0f, 100f);
        if (indexMCloth >= 0) targetWeights[indexMCloth] = mCloth;
    }

    /// <summary>
    /// Resets all blend shape weights to 0.
    /// </summary>
    [ContextMenu("Reset All Blend Shapes")]
    public void ResetAllBlendShapes()
    {
        sad = 0f;
        blink = 0f;
        smile = 0f;
        amaze = 0f;
        head9 = 0f;
        mCloth = 0f;
        autoBlinkWeight = 0f;
        autoBlinkProgress = -1f;
        ResetBlinkTimer();

        if (targetWeights != null)
        {
            for (int i = 0; i < targetWeights.Length; i++)
            {
                targetWeights[i] = 0f;
                if (!enableSmoothing && currentWeights != null && skinnedMeshRenderer != null)
                {
                    currentWeights[i] = 0f;
                    skinnedMeshRenderer.SetBlendShapeWeight(i, 0f);
                }
            }
        }
    }

    /// <summary>
    /// Cross-fades a blendshape to target weight over a specified duration in seconds.
    /// </summary>
    public void CrossFadeBlendShape(string shapeName, float targetWeight, float duration)
    {
        int index = FindBlendShapeIndex(shapeName);
        if (index < 0) return;

        if (activeFades.TryGetValue(index, out Coroutine existing))
        {
            if (existing != null) StopCoroutine(existing);
        }

        activeFades[index] = StartCoroutine(CrossFadeRoutine(index, targetWeight, duration));
    }

    private IEnumerator CrossFadeRoutine(int index, float targetWeight, float duration)
    {
        float startWeight = currentWeights[index];
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float w = Mathf.Lerp(startWeight, targetWeight, t);
            targetWeights[index] = w;
            UpdateFieldForIndex(index, w);
            yield return null;
        }

        targetWeights[index] = targetWeight;
        UpdateFieldForIndex(index, targetWeight);
        activeFades.Remove(index);
    }

    private void UpdateFieldForIndex(int index, float weight)
    {
        if (index == indexSad) sad = weight;
        else if (index == indexBlink) blink = weight;
        else if (index == indexSmile) smile = weight;
        else if (index == indexAmaze) amaze = weight;
        else if (index == indexHead9) head9 = weight;
        else if (index == indexMCloth) mCloth = weight;
    }

    #endregion
}
