using UnityEngine;
using UnityEngine.UI;

public class QuickplayTab : MonoBehaviour
{
#pragma warning disable 649
    [SerializeField] private Text searchingText;
#pragma warning restore 649

    private const float UpdateTime = .5f;
    private int numDots = 1;
    private float elapsedTime;

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= UpdateTime)
        {
            elapsedTime = 0;
            if(++numDots > 3)
                numDots = 1;
            searchingText.text =
#if NETWORKING_STEAM
                "Searching"
#else
                "Joining"
#endif
                + new string('.', numDots);
        }
    }
}
