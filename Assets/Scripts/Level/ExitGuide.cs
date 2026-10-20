using UnityEngine;

// Panah penunjuk jalan di lantai yang berdenyut berurutan menuju pintu keluar.
public class ExitGuide : MonoBehaviour
{
    [SerializeField] private Transform[] chevrons;

    private void Update()
    {
        if (chevrons == null)
            return;
        for (int i = 0; i < chevrons.Length; i++)
        {
            float k = Mathf.Repeat(Time.time * 1.6f - i * 0.25f, 1f);
            float s = 0.75f + Mathf.Sin(k * Mathf.PI) * 0.35f;
            chevrons[i].localScale = new Vector3(s, 1f, s);
        }
    }
}
