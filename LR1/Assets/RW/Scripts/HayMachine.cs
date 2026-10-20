using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HayMachine : MonoBehaviour
{
    public float movementSpeed;
    public float horizontalBoundary = 22;

    public GameObject hayBalePrefab; // 1
    public Transform haySpawnpoint; // 2
    public float shootInterval; // 3
    private float shootTimer; // 4
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMovement();
        UpdateShooting();
    }
    private void UpdateMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if (horizontalInput < 0 && transform.position.x > -horizontalBoundary) // 1
        {
            transform.Translate(transform.right * -movementSpeed * Time.deltaTime);
        }
        else if (horizontalInput > 0 && transform.position.x < horizontalBoundary) // 2
        {
            transform.Translate(transform.right * movementSpeed * Time.deltaTime);
        }
    }
    private void UpdateShooting()
    {
        shootTimer -= Time.deltaTime; // 1
        if (shootTimer <= 0 && Input.GetKey(KeyCode.Space)) // 2
        {
            shootTimer = shootInterval; // 3
            ShootHay(); // 4
        }
    }
    private void ShootHay()
    {
        Instantiate(hayBalePrefab, haySpawnpoint.position, Quaternion.identity);
    }

}
