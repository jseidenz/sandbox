using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif


#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
public class DeveloperMenu : MonoBehaviour
{

    void OnEnable()
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            return;
        }
#endif
        GameObject.Destroy(gameObject);
    }

#if UNITY_EDITOR

    void DrawTopLeftMenu()
    {
        GUI.skin.button.fontSize = 16;

        float x = 40;
        float y = 55;

        if (GUI.Button(new Rect(x, y, 200, 60), "New Game"))
        {
            Game.LaunchGameWithCommandLine(Game.NEW_GAME_COMMAND);
        }
    }

    void OnGUI()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }

        DrawTopLeftMenu();
    }
#endif
}

