using UnityEngine;

public class OrbManager : MonoBehaviour
{
    public GameObject CollectableOrb;
    public Transform SpawnArea;
    Vector3 targetSpawnLocation;
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

        targetSpawnLocation = new Vector3(Random.Range(minX, maxX), 50, Random.Range(minY, maxY));

        while (Physics.CheckSphere(targetSpawnLocation, 10f))
        {
            targetSpawnLocation = new Vector3(Random.Range(minX, maxX), 50, Random.Range(minY, maxY));
        }


        newOrb.transform.localPosition = targetSpawnLocation;
    }
}
