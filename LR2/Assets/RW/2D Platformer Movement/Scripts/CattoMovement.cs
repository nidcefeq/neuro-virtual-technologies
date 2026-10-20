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

public class CattoMovement : MonoBehaviour
{
    // Variables for components
    // 1
    private Rigidbody2D cattoRigidbody2D;
    private Animator cattoAnimator;

    // Bools to check state
    // 2
    private bool cattoIsFacingRight = true;
    private bool cattoIsJumping = false;
    private bool cattoIsGrounded = false;


    // Most common variables
    // 3
    public Transform groundCheck;
    public float groundCheckRadius;
    public LayerMask ground;
    public float moveInput;
    public float cattoSpeed;
    public float cattoJumpForce;


    private void FlipCatto()
    {
        cattoIsFacingRight = !cattoIsFacingRight;
        Vector3 cattoScale = transform.localScale;
        cattoScale.x *= -1;
        transform.localScale = cattoScale;
    }

    void Start()
    {
        cattoRigidbody2D = GetComponent<Rigidbody2D>();
        cattoAnimator = GetComponent<Animator>();

    }

    void Update()
    {
        // Сначала получаем данные
        cattoIsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, ground);
        moveInput = Input.GetAxis("Horizontal");

        if (cattoIsGrounded)
        {
            cattoAnimator.SetFloat("Velocity", Mathf.Abs(moveInput));
        }

        if (Input.GetButtonDown("Jump") && cattoIsGrounded)
        {
            cattoIsJumping = true;
            cattoAnimator.SetTrigger("Jump");
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && cattoIsGrounded)
        {
            cattoAnimator.SetBool("Crouch", true);
        }
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            cattoAnimator.SetBool("Crouch", false);
        }
    }

    private void FixedUpdate()
    {
        cattoRigidbody2D.velocity = new Vector2(moveInput * cattoSpeed, cattoRigidbody2D.velocity.y);

        if (cattoIsFacingRight == false && moveInput > 0)
        {
            FlipCatto();
        }
        else if (cattoIsFacingRight == true && moveInput < 0)
        {
            FlipCatto();
        }

        if (cattoIsJumping)
        {
            cattoRigidbody2D.AddForce(new Vector2(0f, cattoJumpForce));
            cattoIsJumping = false;
        }
    }

}
