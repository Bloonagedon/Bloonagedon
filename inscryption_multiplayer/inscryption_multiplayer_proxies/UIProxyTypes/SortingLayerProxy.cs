using UnityEngine;

public class SortingLayerProxy : MonoBehaviour
{
#pragma warning disable 649
    [SerializeField] private string sortingLayerName;
#pragma warning restore 649
    
    public void Awake()
    {
        var sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        var canvas = GetComponent<Canvas>();
        if (canvas is not null)
        {
            canvas.sortingLayerID = sortingLayerID;
            canvas.sortingLayerName = sortingLayerName;
        }
        var renderer = GetComponent<Renderer>();
        if (renderer is not null)
        {
            renderer.sortingLayerID = sortingLayerID;
            renderer.sortingLayerName = sortingLayerName;
        }
    }
}