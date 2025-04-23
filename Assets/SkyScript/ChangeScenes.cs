using UnityEngine;
using UnityEngine.SceneManagement; // ���������� namespace ������

public class ChangeScenes : MonoBehaviour
{
    [SerializeField]
    private string sceneName; // ��˹����ͩҡ�����ҡ�� Inspector

    // �ѧ��ѹ������¡����͵�ͧ�������¹�ҡ
    public void ChangeScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is empty!");
        }
    }
}
