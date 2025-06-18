using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    private void Awake()
    {
        // Se till att bara en finns
        if (FindObjectsOfType<CursorManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}
