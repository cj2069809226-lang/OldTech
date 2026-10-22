using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CheckPhoto : MonoBehaviour
{
    public Image Photo;
    private bool wasActive;
    private bool hasLoaded;

    void Start()
    {
        if (Photo != null)
            wasActive = Photo.gameObject.activeInHierarchy;
    }

    void Update()
    {
        if (hasLoaded || Photo == null) return;

        bool nowActive = Photo.gameObject.activeInHierarchy;

        if (wasActive && !nowActive)
        {
            hasLoaded = true;
            SceneManager.LoadScene("beforeChildHood");
        }

        wasActive = nowActive;
    }
}
