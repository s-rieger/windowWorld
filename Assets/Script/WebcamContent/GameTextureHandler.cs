using UnityEngine;
using UnityEngine.UI;

public class GameTextureHandler : MonoBehaviour
{
    public RawImage displayImage; // UI Image to display the game texture
    private Texture2D gameTexture;

    //void Start()
    //{
    //    CaptureGameTexture(); // Initialize game texture
    //}

    public void CaptureGameTexture()
    {
        int width = Screen.width;
        int height = Screen.height;

        gameTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        Rect region = new Rect(0, 0, width, height);
        gameTexture.ReadPixels(region, 0, 0);
        gameTexture.Apply();

        // Display the captured texture on a UI RawImage (if used)
        if (displayImage != null)
        {
            displayImage.texture = gameTexture;
        }
    }

    public void SetGameTexture(Texture2D updatedTexture)
    {
        gameTexture = updatedTexture;
        if (displayImage != null)
        {
            displayImage.texture = gameTexture;
        }
    }

    public Texture2D GetGameTexture()
    {
        return gameTexture;
    }
}

