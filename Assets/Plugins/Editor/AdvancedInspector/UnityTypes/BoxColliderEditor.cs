using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BoxCollider), true)]
    public class BoxColliderEditor : ColliderEditor
    {
        private static readonly int hash = "BoxColliderEditor".GetHashCode();
        private object boxEditor;
        private MethodInfo edit;

        protected override void OnEnable()
        {
            base.OnEnable();

            Type box = TypeUtility.GetTypeByName("BoxEditor");
            boxEditor = Activator.CreateInstance(box, true, hash);
            edit = box.GetMethod("OnSceneGUI", new Type[] { typeof(Transform), typeof(Color), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType() });
        }

        protected override void RefreshFields()
        {
            Type type = typeof(BoxCollider);

            base.RefreshFields();

            fields.Add(new InspectorField(type, Instances, type.GetProperty("center"),
                new DescriptorAttribute("Center", "The center of the box, measured in the object's local space.", "http://docs.unity3d.com/ScriptReference/BoxCollider-center.html")));
            fields.Add(new InspectorField(type, Instances, type.GetProperty("size"),
                new DescriptorAttribute("Size", "The size of the box, measured in the object's local space.", "http://docs.unity3d.com/ScriptReference/BoxCollider-size.html")));
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Used)
                return;

            BoxCollider target = (BoxCollider)Instances[0];
            Vector3 center = target.center;
            Vector3 size = target.size;
            Color color = ColliderHandleColor;

            if (!target.enabled)
            {
                color = ColliderHandleColorDisabled;
            }

            object[] arguments = new object[] { target.transform, color, center, size };

            if ((bool)edit.Invoke(boxEditor, arguments))
            {
                center = (Vector3)arguments[2];
                size = (Vector3)arguments[3];

                Undo.RecordObject(target, "Modified Box Collider");
                target.center = center;
                target.size = size;
            }
        }
    }
}