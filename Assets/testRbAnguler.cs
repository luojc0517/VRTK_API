using UnityEngine;
using System.Collections;

public class testRbAnguler : MonoBehaviour
{
    private Rigidbody rb;

    // Use this for initialization
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        rb.angularVelocity = new Vector3(0, 40, 0);
        rb.velocity = new Vector3(0.1f, 0, 0.1f);
    }
}
