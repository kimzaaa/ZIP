using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Uimainclick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image image;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Vector3 targetOffset = new Vector3(10f, 0f, 0f); // ����͹���

    private Coroutine moveCoroutine;

    [Header("��ͤ�����ԡ (���� Console)")]
    public string clickMessage = "�س��ԡ����ٻ����!";

    void Start()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        SetAlpha(0.7f); // ��������
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetAlpha(1f); // �ֺ
        StartMove(originalPosition + targetOffset); // ����͹���
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetAlpha(0.7f); // ��Ѻ���
        StartMove(originalPosition); // ��Ѻ���˹����
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(clickMessage); // �ʴ���ͤ������������ Inspector
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
