using UnityEngine;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// A UI progress bar with interactive controls for scrubbing (dragging) and hover animations.
/// </summary>
/// <remarks>
/// - Designed for video or audio players, but can be reused for any normalized progress visualization.
/// - Handles pointer hover (enlarges bar), drag (updates progress), and click interactions.
/// - Invokes <see cref="pointerDownEvent"/>, <see cref="pointerUpEvent"/>, and <see cref="pointerDragEvent"/> 
/// with a normalized float (0–1) representing the pointer position along the bar.
/// 
/// Required setup in Unity:
/// 1. Assign <see cref="progressBarRectTransform"/> to the full bar background.
/// 2. Assign <see cref="filledProgressRectTransform"/> to the filled portion of the bar.
/// 3. Assign <see cref="playhead"/> to the draggable playhead object.
/// </remarks>
public class ProgressBar : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, 
    IPointerDownHandler, IPointerUpHandler, 
    IDragHandler
{
    /// <summary>
    /// The RectTransform representing the filled portion of the progress bar.
    /// </summary>
    public RectTransform filledProgressRectTransform;

    /// <summary>
    /// The playhead (draggable indicator) object.
    /// </summary>
    public GameObject playhead;

    /// <summary>
    /// The full progress bar background RectTransform.
    /// </summary>
    public RectTransform progressBarRectTransform;

    /// <summary>
    /// Event invoked when the user presses down on the bar.
    /// Passes a normalized 0–1 value representing the click position.
    /// </summary>
    [SerializeField] private UnityEvent<float> pointerDownEvent;

    /// <summary>
    /// Event invoked when the user releases the pointer.
    /// Passes a normalized 0–1 value representing the release position.
    /// </summary>
    [SerializeField] private UnityEvent<float> pointerUpEvent;

    /// <summary>
    /// Event invoked continuously while dragging along the bar.
    /// Passes a normalized 0–1 value representing the drag position.
    /// </summary>
    [SerializeField] private UnityEvent<float> pointerDragEvent;

    private bool dragging = false;
    private bool hovering = false;
    private bool pausedBeforeDrag = true;

    private float width;

    // Animation states
    private bool enlarging = false;
    private bool shrinking = false;

    /// <summary>
    /// Speed of hover enlarge/shrink animations (lower = slower).
    /// </summary>
    private float hoverAnimSpeed = 0.05f;

    /// <summary>
    /// Maximum Y scale when hovered.
    /// </summary>
    public float hoverSize = 3;

    private void Start()
    {
        // Cache the width in world units (adjusted for lossy scale).
        width = progressBarRectTransform.rect.width * transform.lossyScale.x;
    }

    private void Update()
    {
        if (enlarging) Enlarge();
        if (shrinking) Shrink();
    }

    /// <summary>
    /// Sets the progress bar's filled state and playhead position.
    /// </summary>
    /// <param name="normalizedState">
    /// Normalized progress (0 = far left, 1 = far right).
    /// </param>
    public void SetBarProgress(float normalizedState)
    {
        filledProgressRectTransform.offsetMax = new Vector2(
            -(1 - normalizedState) * width,
            filledProgressRectTransform.offsetMax.y);
    }

    /// <summary>
    /// Enlarges the progress bar and playhead (called continuously until target size is reached).
    /// </summary>
    public void Enlarge()
    {
        shrinking = false;
        if (!enlarging) enlarging = true;

        transform.localScale = Vector3.Lerp(
            transform.localScale, new Vector3(1, hoverSize, 1), hoverAnimSpeed);

        playhead.transform.localScale = Vector3.Lerp(
            playhead.transform.localScale, new Vector3(hoverSize / 2, 1, 1), hoverAnimSpeed);

        if (Vector3.Distance(transform.localScale, new Vector3(1, hoverSize, 1)) < 0.001f)
        {
            enlarging = false;
            transform.localScale = new Vector3(1, hoverSize, 1);
            playhead.transform.localScale = new Vector3(hoverSize / 2, 1, 1);
        }
    }

    /// <summary>
    /// Shrinks the progress bar and playhead back to default size (called continuously until reset size is reached).
    /// </summary>
    public void Shrink()
    {
        enlarging = false;
        if (!shrinking) shrinking = true;

        transform.localScale = Vector3.Lerp(
            transform.localScale, new Vector3(1, 1, 1), hoverAnimSpeed);

        playhead.transform.localScale = Vector3.Lerp(
            playhead.transform.localScale, new Vector3(1, 1, 1), hoverAnimSpeed);

        if (Vector3.Distance(transform.localScale, new Vector3(1, 1, 1)) < 0.001f)
        {
            shrinking = false;
            transform.localScale = Vector3.one;
            playhead.transform.localScale = Vector3.one;
        }
    }

    /// <inheritdoc/>
    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        Enlarge();
    }

    /// <inheritdoc/>
    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        if (!dragging) Shrink();
    }

    /// <inheritdoc/>
    public void OnDrag(PointerEventData eventData)
    {
        float normalizedTime = GetValueFromPointer(eventData);
        SetBarProgress(normalizedTime);
        pointerDragEvent.Invoke(normalizedTime);
    }

    /// <inheritdoc/>
    public void OnPointerDown(PointerEventData eventData)
    {
        dragging = true;
        float normalizedTime = GetValueFromPointer(eventData);
        SetBarProgress(normalizedTime);
        pointerDownEvent.Invoke(normalizedTime);
    }

    /// <inheritdoc/>
    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
        if (!hovering) Shrink();

        float normalizedTime = GetValueFromPointer(eventData);
        SetBarProgress(normalizedTime);
        pointerUpEvent.Invoke(normalizedTime);
    }

    /// <summary>
    /// Converts a pointer position into a normalized value along the progress bar.
    /// </summary>
    /// <param name="eventData">Pointer event data (screen position, camera, etc.).</param>
    /// <returns>
    /// Normalized progress (0 = far left, 1 = far right).  
    /// Returns 0 if the pointer position cannot be converted.
    /// </returns>
    public float GetValueFromPointer(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                progressBarRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
        {
            return Mathf.InverseLerp(0, width, localPoint.x);
        }
        return 0;
    }
}
