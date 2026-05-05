using UnityEngine;
using System.Collections;

public class WallPanel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool Selected = false;
    public int Index;
    public Wall wall;

    [Header("Spawn Animation")]
    public float animationOffset = 0.1f;
    public float animationDuration = 0.25f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);


    void Start()
    {
        wall = GetComponentInParent<Wall>();
        StartCoroutine(PopAnimation());
    }


    // Update is called once per frame
    void Update()
    {
        if (Selected)
        {
            if (wall == null) return;

            if (wall.panelIndex != Index)
            {
                wall.panelIndex = Index;
            }
        }
    }


    // Pop Animation Coroutine
    IEnumerator PopAnimation()
    {
        Vector3 startPos = transform.position;
        Vector3 upPos = startPos + Vector3.up * animationOffset;
        float time = 0f;

        while (time < animationDuration)
        {
            float t = animationCurve.Evaluate(time / animationDuration);
            transform.position = Vector3.Lerp(startPos, upPos, t);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = upPos;

        time = 0f;
        while (time < animationDuration)
        {
            float t = animationCurve.Evaluate(time / animationDuration);
            transform.position = Vector3.Lerp(upPos, startPos, t);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;
    }


    // Move Right Function
    public void MovePanelRight()
    {
        StartCoroutine(MoveRight());
    }


    // Move Left Function
    public void MovePanelLeft()
    {
        StartCoroutine(MoveLeft());
    }


    // Move Right Coroutine
    IEnumerator MoveRight()
    {
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = startPos + Vector3.right * 1f;

        float time = 0f;

        while (time < animationDuration)
        {
            float t = animationCurve.Evaluate(time / animationDuration);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            time += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = targetPos;
        wall.RefreshPanels();
    }


    // Coroutine to move panel left
    IEnumerator MoveLeft()
    {
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = startPos + Vector3.left * 1f;

        float time = 0f;

        while (time < animationDuration)
        {
            float t = animationCurve.Evaluate(time / animationDuration);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            time += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = targetPos;
        wall.RefreshPanels();
    }
}
