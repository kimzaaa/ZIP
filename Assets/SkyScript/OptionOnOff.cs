using UnityEngine;
using UnityEngine.EventSystems;

public class OptionOnOff : MonoBehaviour, IPointerClickHandler
{
    [Header("Canvas ���������ж١�Դ")]
    public GameObject[] allCanvases;  // Canvas ���µ�Ƿ����ҨлԴ

    [Header("Canvas ����ͧ����Դ")]
    public GameObject[] canvasToOpen;  // ��Ƿ���ͧ����Դ

    public void OnPointerClick(PointerEventData eventData)
    {
        
        foreach (GameObject canvas in allCanvases)
        {
            if (canvas != null)
                canvas.SetActive(false);
        }

        foreach (GameObject canvas in canvasToOpen)
        {
            if (canvas != null)
                canvas.SetActive(true);
        }
    }
}
