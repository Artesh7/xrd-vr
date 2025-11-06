# VR Gun M1911 Interaction Behavior

This project has custom behavior to improve the experience when grabbing the M1911:

- Toggle-to-grab: Press once to grab, press again to release. No need to hold.
- Hide controller visuals while holding the gun, and disable its Rigidbody collisions/forces.
- Gun orientation auto-corrects so it points forward when held.

## Components and scripts

- `Assets/Scripts/Gun/GunController.cs`
  - Subscribes to select entered/exited to hide/show the grabbing controller.
  - Disables controller `Rigidbody` collisions and sets it kinematic while held.
  - Ensures an attach transform exists and rotates it so the muzzle points forward in-hand.
  - Notes: Uses the `muzzle` Transform to infer yaw. Adjust in inspector if needed.

- `Assets/Scripts/XR/XRControllerToggleSelect.cs`
  - On scene load, finds all `XRBaseControllerInteractor` and sets `selectActionTrigger = Toggle`.
  - Auto-instantiates via `RuntimeInitializeOnLoadMethod`, so you don't need to add it into a scene.

## Tweaks

- If controller visuals still appear, ensure those MeshRenderers are under the interactor GameObject in the hierarchy.
- If the gun still feels off-angle, set a manual yaw offset on the attach transform (created at runtime as `GripAttach` under the gun). You can also add a small script to expose this offset if desired.

## Known limitations

- If your controller models are provided by a separate SDK object outside the interactor hierarchy, they may not be hidden; extend `GunController` to include explicit references to those objects and disable them on select.
- If the XR Interaction Toolkit version changes API locations, `XRControllerToggleSelect` will silently skip setting the toggle if the properties/types are not found; update namespaces accordingly.
