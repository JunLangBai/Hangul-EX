using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ARObj", menuName = "ARObj/ObjList")]
public class ARObjectList : ScriptableObject
{
    public List<GameObject> objectList = new List<GameObject>();
}
