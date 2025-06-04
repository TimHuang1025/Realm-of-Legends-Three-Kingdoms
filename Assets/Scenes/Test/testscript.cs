using UnityEngine;
using UnityEngine.SceneManagement;

public class testscript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SceneManager.LoadScene("MainUI", LoadSceneMode.Single);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
