using UnityEngine;
using UnityEngine.SceneManagement;

public class ToScene : MonoBehaviour
{
    
    public void GoToNext(string name)
    {
        
        SceneManager.LoadScene(name);
    }
}

