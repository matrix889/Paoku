using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace AdvancedInspector
{
    public class AnimationCurveEditor : FieldEditor
    {
        public override Type[] EditedTypes
        {
            get { return new Type[] { typeof(AnimationCurve) }; }
        }

        public override void Draw(InspectorField field, GUIStyle style)
        {
            object value = GetValue(field);
            if (value == null)
                return;

            AnimationCurve curve = (AnimationCurve)value;

            EditorGUI.BeginChangeCheck();

            AnimationCurve result;
            if (field.Descriptor != null)
                result = EditorGUILayout.CurveField(curve, field.Descriptor.Color, new Rect(0, 0, 0, 0));
            else
                result = EditorGUILayout.CurveField(curve, Color.red, new Rect(0, 0, 0, 0));

            if (EditorGUI.EndChangeCheck())
                field.SetValue(result);
        }
    }
}
