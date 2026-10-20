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
