using UnityEngine;
using UnityEngine.InputSystem;

// Semua input game lewat satu kelas, supaya keyboard, gamepad, dan autopilot uji
// memakai jalur yang sama.
public static class CapyInput
{
    // Dipakai AutoPilot dan pengujian otomatis.
    public static bool UseOverride;
    public static Vector2 MoveOverride;
    public static bool DashOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        UseOverride = false;
        MoveOverride = Vector2.zero;
        DashOverride = false;
        mouseAim = true;
    }

    public static Vector2 Move()
    {
        if (UseOverride)
            return Vector2.ClampMagnitude(MoveOverride, 1f);

        Vector2 v = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
        }

        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f)
                v += stick;
            v += gp.dpad.ReadValue();
        }

        return Vector2.ClampMagnitude(v, 1f);
    }

    // Arah bidik serangan (gaya HoloCure): kursor mouse di lantai, atau stik kanan gamepad.
    // Perangkat yang terakhir digerakkan yang dipakai. False berarti tidak ada bidikan manual
    // (autopilot, atau gamepad tanpa stik kanan) sehingga serangan mengincar musuh terdekat.
    private static bool mouseAim = true;

    public static bool Aim(Vector3 origin, out Vector3 dir)
    {
        dir = Vector3.zero;
        if (UseOverride)
            return false;

        Gamepad gp = Gamepad.current;
        Vector2 stick = gp != null ? gp.rightStick.ReadValue() : Vector2.zero;
        Mouse mouse = Mouse.current;
        if (stick.sqrMagnitude > 0.09f)
            mouseAim = false;
        else if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0f || mouse.leftButton.isPressed))
            mouseAim = true;

        if (!mouseAim)
        {
            if (stick.sqrMagnitude <= 0.09f)
                return false;
            dir = new Vector3(stick.x, 0f, stick.y).normalized;
            return true;
        }

        Camera cam = Camera.main;
        if (mouse == null || cam == null)
            return false;
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        Plane ground = new Plane(Vector3.up, origin);
        float t;
        if (!ground.Raycast(ray, out t))
            return false;
        dir = ray.GetPoint(t) - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return false;
        dir.Normalize();
        return true;
    }

    public static bool DashPressed()
    {
        if (DashOverride)
        {
            DashOverride = false;
            return true;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame))
            return true;

        Gamepad gp = Gamepad.current;
        return gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.rightTrigger.wasPressedThisFrame);
    }

    public static bool PausePressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
            return true;

        Gamepad gp = Gamepad.current;
        return gp != null && gp.startButton.wasPressedThisFrame;
    }

    public static bool VisionPressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.vKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame))
            return true;

        Gamepad gp = Gamepad.current;
        return gp != null && gp.selectButton.wasPressedThisFrame;
    }

    public static bool ConfirmPressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            return true;

        Gamepad gp = Gamepad.current;
        return gp != null && gp.buttonSouth.wasPressedThisFrame;
    }

    public static bool KeyPressed(Key key)
    {
        Keyboard kb = Keyboard.current;
        return kb != null && kb[key].wasPressedThisFrame;
    }

    // Tombol pilihan kartu upgrade: 1, 2, 3 (atau numpad).
    public static int ChoicePressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return -1;
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return 0;
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return 1;
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return 2;
        return -1;
    }
}
