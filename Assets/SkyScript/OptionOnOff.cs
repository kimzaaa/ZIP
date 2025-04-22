using UnityEngine;
using UnityEngine.EventSystems;

public class OptionOnOff : MonoBehaviour, IPointerClickHandler
{
    [Header("Canvas ทั้งหมดที่จะถูกปิด")]
    public GameObject[] allCanvases;  // Canvas หลายตัวที่เราจะปิด

    [Header("Canvas ที่ต้องการเปิด")]
    public GameObject canvasToOpen;  // ตัวที่ต้องการเปิด

    public void OnPointerClick(PointerEventData eventData)
    {
        // ปิดทุก Canvas ที่อยู่ใน allCanvases
        foreach (GameObject canvas in allCanvases)
        {
            if (canvas != null)
                canvas.SetActive(false);
        }

        // เปิดเฉพาะ Canvas ที่ต้องการ
        if (canvasToOpen != null)
            canvasToOpen.SetActive(true);
    }
}
