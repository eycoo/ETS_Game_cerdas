using UnityEngine;
using UnityEngine.EventSystems;

// Tombol membesar sedikit saat disorot mouse/keyboard, seperti game komersial.
public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private float target = 1f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        target = 1.07f;
        AudioManager.Sfx("hover", 0.25f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        target = 1f;
    }

    public void OnSelect(BaseEventData eventData)
    {
        target = 1.07f;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        target = 1f;
    }

    private void OnDisable()
    {
        target = 1f;
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (target <= 1f && transform.localScale.x <= 1.0001f)
            return;
        float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        transform.localScale = new Vector3(s, s, 1f);
    }
}
