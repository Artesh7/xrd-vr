using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerInput : MonoBehaviour
{
    [Header("References")]
    public Transform cam;       // Assign your Main Camera
    public Transform muzzle;    // Assign Muzzle point
    public GameObject projectilePrefab;

    [Header("Settings")]
    public float moveSpeed = 6f;
    public float sensitivity = 200f;
    public float fireRate = 6f;
    public float projectileSpeed = 30f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float yaw;
    private float pitch;
    private float nextFire;
    private bool fireHeld;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Movement
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;
        controller.SimpleMove(move);

        // Look
        float mx = lookInput.x * sensitivity * Time.deltaTime;
        float my = lookInput.y * sensitivity * Time.deltaTime;
        yaw += mx;
        pitch = Mathf.Clamp(pitch - my, -80f, 80f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cam) cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Fire
        if (fireHeld && Time.time >= nextFire)
        {
            nextFire = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (!muzzle || !projectilePrefab) return;

        GameObject proj = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        if (proj.TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = muzzle.forward * projectileSpeed;
    }

    // Input System callbacks
    public void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    public void OnFire(InputAction.CallbackContext ctx)
    {
        if (ctx.started) fireHeld = true;
        if (ctx.canceled) fireHeld = false;
        if (ctx.performed && Time.time >= nextFire)
        {
            nextFire = Time.time + 1f / fireRate;
            Shoot();
        }
    }
}
