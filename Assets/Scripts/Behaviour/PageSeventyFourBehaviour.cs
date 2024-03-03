using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageSeventyFourBehaviour : MonoBehaviour
{
    [SerializeField] private GameObject waterDropParticle;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SpawnWaterDrop());
    }
    IEnumerator SpawnWaterDrop()
    {
        waterDropParticle.SetActive(false);
        yield return new WaitForSeconds(2f);
        waterDropParticle.SetActive(true);
    }
}
