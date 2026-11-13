using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class SandboxPlayer : MonoBehaviour
{
    [SerializeField, Min(0f)] private float speed = 5f;
    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || FadeTransition.IsBusy)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        var right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        var left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        var up = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        var down = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        var direction = new Vector2((right ? 1 : 0) - (left ? 1 : 0), (up ? 1 : 0) - (down ? 1 : 0));
        body.linearVelocity = direction.normalized * speed;
    }
}
