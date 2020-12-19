using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class EditorUpdater : MonoBehaviour
{
    static bool _IsRegistered;

#if UNITY_EDITOR
    void OnEnable()
    {
        if (EditorApplication.isPlaying) return;
        if (_IsRegistered) return;

        EditorApplication.update += () =>
        {
            EditorApplication.QueuePlayerLoopUpdate();                
        };
        _IsRegistered = true;
    }
#endif
}
