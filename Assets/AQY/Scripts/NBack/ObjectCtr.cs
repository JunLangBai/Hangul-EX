using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCtr : MonoBehaviour
{
    public GameObject currentCell;
    
    // Update is called once per frame
    void Update()
    {

        transform.position = currentCell.transform.position + currentCell.transform.up * 0.02f;;
        transform.rotation = currentCell.transform.rotation;

    }
}
