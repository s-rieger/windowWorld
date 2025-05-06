using UnityEngine;

public class OrbManager : MonoBehaviour
{
    public GameObject CollectableOrb;
    public Transform SpawnArea;

    public float maxX;
    public float minX;
    public float minY;
    public float maxY;

    public static OrbManager Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(Instance); }
        else { Instance = this; }
    }

    public void SpawnOrb()
    {
        GameObject newOrb = Instantiate(CollectableOrb, SpawnArea);

        newOrb.transform.localPosition = new Vector3(Random.Range(minX, maxX), 0, Random.Range(minY, maxY));
    }
}
