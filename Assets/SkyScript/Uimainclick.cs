using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Uimainclick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image image;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Vector3 targetOffset = new Vector3(10f, 0f, 0f); // ระยะการเคลื่อนที่

    private Coroutine moveCoroutine;

    [Header("ข้อความเมื่อคลิก (แสดงใน Console)")]

    public string clickMessage = "สวัสดี คุณคลิกปุ่มนี้!";

    void Start()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        SetAlpha(0.7f); // เริ่มต้น
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetAlpha(1f); // ทำให้จาง
        StartMove(originalPosition + targetOffset); // เคลื่อนที่ไปข้างหน้า
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetAlpha(0.7f); // ทำให้ใส
        StartMove(originalPosition); // กลับไปที่เดิม
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickMessage == "Exit")
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        else
        {
            Debug.Log(clickMessage); // แสดงข้อความที่กำหนดใน Inspector
        }
    }

    private void SetAlpha(float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void StartMove(Vector3 targetPosition)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(SmoothMove(targetPosition));
    }

    private IEnumerator SmoothMove(Vector3 targetPosition)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startPos = rectTransform.anchoredPosition;

        while (elapsed < duration)
        {
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }
}