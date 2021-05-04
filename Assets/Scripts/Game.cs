using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    private void Awake()
    {
        RB.Initialize(new Engine());
    }
}
