using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class move : MonoBehaviour
{
    public Vector2 speed;
    // Update is called once per frame
    void Update()
    {
        transform.position += new Vector3(speed.x, 0, speed.y) * Time.deltaTime;
    }
}
