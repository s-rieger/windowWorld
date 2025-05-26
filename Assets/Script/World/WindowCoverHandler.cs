using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class WindowCoverHandler : MonoBehaviour
{
    Animation anim;
    [SerializeField] private Transform leftCover;
    [SerializeField] private Transform rightCover;
    [SerializeField] private float turnDuration = 1f;

    private void Awake()
    {
        anim = GetComponent<Animation>();
    }

    public void OpenWindows(int index)
    {
        if(ScreenDetector.Instance.isDebug == true) { return; }
        StartCoroutine(OpenWindowCoro(index));

    }

    public void CloseWindows(int index) 
    {
        if (ScreenDetector.Instance.isDebug == true) { return; }
        StartCoroutine(CloseWindowCoro(index));
    }


    IEnumerator OpenWindowCoro(int index)
    {
        // Comment section is for perspective camera
        //float targetLeftRot = 130 - index*4f;
        //float targetRightRot = 120 - index*15;

        float targetLeftRot = 120;
        float targetRightRot = 60;

        Quaternion startRotationLeft = leftCover.rotation;
        Quaternion startRotationRight = rightCover.rotation;
        Quaternion endRotationLeft = Quaternion.Euler(-90f, targetLeftRot, 0f);
        Quaternion endRotationRight = Quaternion.Euler(-90f, targetRightRot, 0f);

        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            float t = elapsed / turnDuration;
            leftCover.rotation = Quaternion.Slerp(startRotationLeft, endRotationLeft, t);
            rightCover.rotation = Quaternion.Slerp(startRotationRight, endRotationRight, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Make sure it finishes exactly at the target
        leftCover.rotation = endRotationLeft;
        rightCover.rotation = endRotationRight;
    }

    IEnumerator CloseWindowCoro(int index)
    {
        for (int i = 0; i < ScreenDetector.Instance.PlayerHandlers.Count; i++)
        {
            if (ScreenDetector.Instance.PlayerHandlers[i] == null)
            {
                ArduinoSetup.instance.SetLedColorForPlayer(i + 1, "OFF");
            }
        }

        Quaternion startRotationLeft = leftCover.rotation;
        Quaternion startRotationRight = rightCover.rotation;
        Quaternion endRotationLeft = Quaternion.Euler(-90f, 180, 180);
        Quaternion endRotationRight = Quaternion.Euler(-90f, 180, 0);

        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            float t = elapsed / turnDuration;
            leftCover.rotation = Quaternion.Slerp(startRotationLeft, endRotationLeft, t);
            rightCover.rotation = Quaternion.Slerp(startRotationRight, endRotationRight, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Make sure it finishes exactly at the target
        leftCover.rotation = endRotationLeft;
        rightCover.rotation = endRotationRight;
    }
}



