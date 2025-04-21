using UnityEngine;
using UnityEngine.EventSystems;

public class OptionOnOff : MonoBehaviour, IPointerClickHandler
{
    [Header("Canvas ���������ж١�Դ")]
    public GameObject[] allCanvases;  // Canvas ���µ�Ƿ����ҨлԴ

    [Header("Canvas ����ͧ����Դ")]
    public GameObject canvasToOpen;  // ��Ƿ���ͧ����Դ

    public void OnPointerClick(PointerEventData eventData)
    {
        // �Դ�ء Canvas �������� allCanvases
        foreach (GameObject canvas in allCanvases)
        {
            if (canvas != null)
                canvas.SetActive(false);
        }

        // �Դ੾�� Canvas ����ͧ���
        if (canvasToOpen != null)
            canvasToOpen.SetActive(true);
    }
}
