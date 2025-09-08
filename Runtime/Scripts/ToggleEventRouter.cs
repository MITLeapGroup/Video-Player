using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Routes the state of a Unity UI <see cref="Toggle"/> into two separate UnityEvents.
/// </summary>
/// <remarks>
/// Attach this component to a GameObject with a <see cref="Toggle"/>.
/// It will automatically subscribe to the toggle’s <see cref="Toggle.onValueChanged"/> event
/// and invoke either <see cref="onTrue"/> or <see cref="onFalse"/> depending on the toggle state.
/// 
/// This is useful if you want to assign different actions in the Inspector
/// for when the toggle is checked vs unchecked, without writing custom scripts.
/// </remarks>
[RequireComponent(typeof(Toggle))]
public class ToggleEventRouter : MonoBehaviour
{
    /// <summary>
    /// Reference to the <see cref="Toggle"/> component. 
    /// If left empty, it will be auto-assigned from the same GameObject.
    /// </summary>
    [Header("Toggle Reference (auto-assigned if left empty)")]
    public Toggle toggle;

    /// <summary>
    /// Event invoked when the toggle is switched on (true).
    /// </summary>
    [Header("Events")]
    public UnityEvent onTrue;

    /// <summary>
    /// Event invoked when the toggle is switched off (false).
    /// </summary>
    public UnityEvent onFalse;

    /// <summary>
    /// Called by Unity when the component is reset in the Inspector.
    /// Automatically assigns the <see cref="Toggle"/> reference if available.
    /// </summary>
    private void Reset()
    {
        // Auto-assign Toggle if present on the same GameObject
        toggle = GetComponent<Toggle>();
    }

    /// <summary>
    /// Unity lifecycle method called when the script instance is being loaded.
    /// Ensures the <see cref="Toggle"/> reference is assigned
    /// and subscribes to its value change events.
    /// </summary>
    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        // Subscribe to toggle changes
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    /// <summary>
    /// Unity lifecycle method called when the GameObject is destroyed.
    /// Cleans up the event subscription to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    /// <summary>
    /// Callback for when the toggle value changes.
    /// Invokes <see cref="onTrue"/> if enabled, <see cref="onFalse"/> if disabled.
    /// </summary>
    /// <param name="value">The new toggle state (true if on, false if off).</param>
    private void OnToggleValueChanged(bool value)
    {
        if (value)
        {
            onTrue.Invoke();
        }
        else
        {
            onFalse.Invoke();
        }
    }
}
