using UnityEngine;
using System;
using System.Collections;

using AdvancedInspector;

[AdvancedInspector]
public class AIExample19_Restrict : MonoBehaviour 
{
    // Unlike other "dynamic" attributes, the Restrict attribute can only work in "dynamic" mode.
    // The restrict attribute limits - or restrict - the data that can be input in a field.
    // This is quite useful when you want to limit what can be selected. 
    [Inspect, Restrict("ValidFloats")]
    public float[] myFloat;

    private IList ValidFloats()
    {
        return new float[] { 0, 2, 4, 6, 8, 10 };
    }

    // The restrict attribute can display the choices as a drop down list, a collection of button, or a toolbox popup
    // The toolbox popup is quite useful when you have a high number of choices and you want the user to search in them.
    [Inspect, Restrict("ValidStrings", RestrictDisplay.Toolbox)]
    public string myString;

    private IList ValidStrings()
    {
        return new string[] { "A", "AA", "A+", "B", "BB", "B+", "C", "CC", "C+", "D", "DD", "D+", "E", "EE", "E+" };
    }
}
