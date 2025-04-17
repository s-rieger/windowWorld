using UnityEngine;
using UnityEngine.UI;

public class WebcamTextureHandler : MonoBehaviour
{
    public RawImage displayImage; // UI Image to show webcam feed
    private WebCamTexture webcamTexture;

    void Start()
    {
        webcamTexture = new WebCamTexture();
        displayImage.texture = webcamTexture;
        displayImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();
    }

    public Texture2D GetWebcamTexture()
    {
        Texture2D tex = new Texture2D(webcamTexture.width, webcamTexture.height);
        tex.SetPixels(webcamTexture.GetPixels());
        tex.Apply();
        return tex;
    }
}
