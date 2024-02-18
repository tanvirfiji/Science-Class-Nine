using System.Collections;
using UnityEngine;

public class PageOneHundredEighteen : MonoBehaviour
{
    [SerializeField] private LayerMask touchable;
    [SerializeField] private Animator anim;
    [SerializeField] private GameObject partOne;
    [SerializeField] private GameObject partTwo;
    [SerializeField] private GameObject partThree;

    private void Start()
    {
        partOne.SetActive(true);
        partTwo.SetActive(false);
        partThree.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, touchable))
            {
                StartCoroutine(NextMove());
                
            }

        }
    }

    IEnumerator NextMove()
    {
        partOne.SetActive(false);
        partTwo.SetActive(true);
        partThree.SetActive(false);

        if (partTwo.activeSelf)
            anim.SetTrigger("Touched");

        yield return new WaitForSeconds(2);

        partOne.SetActive(false);
        partTwo.SetActive(false);
        partThree.SetActive(true);
        GetComponent<BoxCollider>().enabled = false;
    }
}
