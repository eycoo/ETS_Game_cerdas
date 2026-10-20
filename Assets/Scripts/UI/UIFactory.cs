using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Pembuat elemen UI lewat kode (canvas, panel, teks, tombol), supaya semua UI konsisten.
public static class UIFactory
{
    public static readonly Color Cream = new Color(1f, 0.96f, 0.88f);
    public static readonly Color PanelDark = new Color(0.12f, 0.09f, 0.18f, 0.92f);
    public static readonly Color Orange = new Color(1f, 0.62f, 0.18f);
    public static readonly Color Green = new Color(0.4f, 0.88f, 0.42f);
    public static readonly Color Blue = new Color(0.38f, 0.78f, 1f);
    public static readonly Color Red = new Color(1f, 0.36f, 0.3f);
    public static readonly Color Outline = new Color(0.08f, 0.05f, 0.12f, 0.85f);

    public static Canvas Canvas(string name, int sortOrder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Object.DontDestroyOnLoad(es);
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(this RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool sliced = true)
    {
        RectTransform rt = Rect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null && sliced && sprite.border.sqrMagnitude > 0f)
            img.type = UnityEngine.UI.Image.Type.Sliced;
        return img;
    }

    public static Text Text(Transform parent, string name, string value, int size, Color color, TextAnchor align, bool title = false, float outline = 2f)
    {
        RectTransform rt = Rect(name, parent);
        Text t = rt.gameObject.AddComponent<Text>();
        GameDatabase db = GameDatabase.I;
        t.font = title ? db.titleFont : db.bodyFont;
        t.text = value;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.supportRichText = true;
        if (outline > 0f)
        {
            Outline o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = Outline;
            o.effectDistance = new Vector2(outline, -outline);
            if (title)
            {
                Shadow s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.45f);
                s.effectDistance = new Vector2(0f, -outline * 2.5f);
            }
        }
        return t;
    }

    public static Button Button(Transform parent, string name, string label, Color color, UnityAction onClick, Vector2 size, int fontSize = 40)
    {
        Image bg = Image(parent, name, GameDatabase.I.rounded, color);
        bg.raycastTarget = true;
        bg.rectTransform.sizeDelta = size;
        Button b = bg.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        cb.selectedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.colorMultiplier = 1.2f;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        b.targetGraphic = bg;
        b.onClick.AddListener(() => AudioManager.Sfx("click", 0.7f));
        if (onClick != null)
            b.onClick.AddListener(onClick);

        Text t = Text(bg.transform, "Label", label, fontSize, Color.white, TextAnchor.MiddleCenter, true, 2.5f);
        t.rectTransform.Stretch();
        bg.gameObject.AddComponent<UIHover>();
        return b;
    }

    public static CanvasGroup Group(GameObject go)
    {
        CanvasGroup g = go.GetComponent<CanvasGroup>();
        if (g == null)
            g = go.AddComponent<CanvasGroup>();
        return g;
    }

    // ---------------------------------------------------------------- tween (unscaled time)

    public static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = to;
    }

    public static IEnumerator PopScale(Transform tr, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = CapyMath.EaseOutBack(Mathf.Clamp01(t / duration));
            tr.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, k);
            yield return null;
        }
        tr.localScale = Vector3.one * to;
    }
}
