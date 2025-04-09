using UnityEngine;

public class CameraAspectAdjuster : MonoBehaviour
{
    // Set your desired target aspect ratio, e.g., 16:9
    public float targetAspect = 16f / 9f;

    void Start()
    {
        // Calculate the current screen aspect ratio
        float windowAspect = (float)Screen.width / (float)Screen.height;

        // Calculate the scale height based on the current aspect ratio compared to the target
        float scaleHeight = windowAspect / targetAspect;

        // Get the camera component
        Camera camera = Camera.main;

        if (scaleHeight < 1.0f)
        {
            // If the screen is too tall, add letterboxing (top and bottom black bars)
            Rect rect = camera.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            camera.rect = rect;
        }
        else
        {
            // If the screen is too wide, crop the sides (cut-off width)
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = camera.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            camera.rect = rect;
        }
    }
}
