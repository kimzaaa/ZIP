using UnityEngine;

public class TestNumberDisplay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    [SerializeField] private float NumberDispay = 1000f;
    void Start()
    {
        GetComponent<NumberDisplay>().DisplayNumber(NumberDispay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
