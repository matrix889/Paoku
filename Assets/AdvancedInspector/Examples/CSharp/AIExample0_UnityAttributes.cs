﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// As of version 1.52, Unity attributes are also supported. 
// So you will have to change your code as little as possible.
public class AIExample0_UnityAttributes : MonoBehaviour
{
    [Range(0, 10)]
    public int rangeField;

    [Header("This is a header")]
    public int headerField;

    [Tooltip("This is a tooltip")]
    public int tooltipField;

    [Space(10)]
    public int spaceField;

    [Multiline]
    public string multilineField;

    [TextArea]
    public string textAreaField;
}