using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    public string tagFilter;
    private void OnTriggerEnter(Collider other) // 1
    {
        if (other.CompareTag(tagFilter)) // 2
        {
            Destroy(gameObject); // 3
        }
    }

}
