using UnityEngine;
using System.Collections;

public class Menu : MonoBehaviour {

    public string startLevel;

    public void NewGame()
    {
        Application.LoadLevel(startLevel);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
