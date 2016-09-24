using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// Forces a field to display a FieldEditor related to its current runtime type instead of the field type.
    /// The Runtime version supply the type itself. Useful when the field value is null or for an unknown object picker.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RuntimeResolveAttribute : Attribute, IListAttribute, IRuntimeAttribute<Type>
    {
        public delegate Type RuntimeResolveDelegate();

        #region IRuntime Implementation
        private string methodName = "";

        public string MethodName
        {
            get { return methodName; }
        }

        public Type Template
        {
            get { return typeof(RuntimeResolveDelegate); }
        }

        private List<Delegate> delegates = new List<Delegate>();

        public List<Delegate> Delegates
        {
            get { return delegates; }
            set { delegates = value; }
        }

        public Type Invoke(int index)
        {
            if (delegates.Count == 0 || index >= delegates.Count)
                return null;

            try
            {
                return delegates[index].DynamicInvoke() as Type;
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = ((TargetInvocationException)e).InnerException;

                Debug.LogError(string.Format("Invoking a method from a RuntimeResolve attribute failed. The exception was \"{0}\".", e.Message));
                return null;
            }
        }
        #endregion

        public RuntimeResolveAttribute() { }

        public RuntimeResolveAttribute(string methodName)
        {
            this.methodName = methodName;
        }

        public RuntimeResolveAttribute(Delegate method)
        {
            this.delegates.Add(method);
        }
    }
}