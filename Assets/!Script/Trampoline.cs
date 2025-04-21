using System;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
   [SerializeField] private float bounceForce;
   private void OnCollisionEnter(Collision collision)
   {
      if (collision.gameObject.CompareTag("Player"))
      {
         Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();

         if (rb != null)
         {
            rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
         }
      }
   }
}
