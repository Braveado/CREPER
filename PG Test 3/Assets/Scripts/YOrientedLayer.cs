using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YOrientedLayer : MonoBehaviour
{
    void Start()
    {
        GetComponent<SpriteRenderer>().sortingOrder = -(int)(transform.position.y * 10);
    }
}
