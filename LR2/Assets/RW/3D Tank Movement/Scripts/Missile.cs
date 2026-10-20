/*
 * Copyright (c) 2020 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish, 
 * distribute, sublicense, create a derivative work, and/or sell copies of the 
 * Software in any work that is designed, intended, or marketed for pedagogical or 
 * instructional purposes related to programming, coding, application development, 
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works, 
 * or sale is expressly withheld.
 *    
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using UnityEngine;

public class Missile : MonoBehaviour
{
    public float bulletSpeed = 100f;
    public GameObject explosionPrefab;
    public float blastRadius = 5f;

    private Rigidbody rbInstanceBullet;

    void Start()
    {
        rbInstanceBullet = gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        rbInstanceBullet.velocity = rbInstanceBullet.transform.forward * bulletSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);
        Vector3 contactPosition = contact.point;

        var explosionInstance = Instantiate(explosionPrefab, contactPosition, Quaternion.identity);

        ParticleSystem particlesOfExplosion = explosionInstance.GetComponent<ParticleSystem>();
        particlesOfExplosion.Play();

        if (collision.gameObject.CompareTag("TankRatto"))
        {
            Rigidbody ratto = collision.gameObject.GetComponent<Rigidbody>();
            ratto.AddExplosionForce(1000, transform.position, blastRadius);
        }

        Physics.OverlapSphere(transform.position, blastRadius);

        Destroy(gameObject, particlesOfExplosion.main.duration);
        Destroy(explosionInstance, particlesOfExplosion.main.duration);
    }
}
