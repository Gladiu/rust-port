using System.Numerics;
using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private GameObject playerObj = null;
    // Start is called before the first frame update
    void Start()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {

        UnityEngine.Vector3 direction = (playerObj.transform.position - transform.position).normalized;
        transform.position += direction * 0.3f * Time.deltaTime;
    }
}
