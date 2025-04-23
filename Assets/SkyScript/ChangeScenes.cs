using UnityEngine;
using UnityEngine.SceneManagement; // อย่าลืมใส่ namespace นี้ด้วย

public class ChangeScenes : MonoBehaviour
{
    [SerializeField]
    private string sceneName; // กำหนดชื่อฉากที่อยากไปใน Inspector

    // ฟังก์ชันนี้เรียกเมื่อต้องการเปลี่ยนฉาก
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
