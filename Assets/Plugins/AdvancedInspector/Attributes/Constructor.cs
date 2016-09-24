﻿using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// Some object cannot be created with an empty constructor.
    /// This runtime attribute lets you create the object by yourself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConstructorAttribute : Attribute, IRuntimeAttribute<object>
    {
        public delegate object ConstructorDelegate();

        #region IRuntime Implementation
        private string methodName = "";

        public string MethodName
        {
            get { return methodName; }
        }

        public Type Template
        {
            get { return typeof(ConstructorDelegate); }
        }

        private List<Delegate> delegates = new List<Delegate>();

        public List<Delegate> Delegates
        {
            get { return delegates; }
            set { delegates = value; }
        }

        public object Invoke(int index)
        {
            if (delegates.Count == 0 || index >= delegates.Count)
                return null;

            try
            {
                return delegates[index].DynamicInvoke();
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = ((TargetInvocationException)e).InnerException;

                Debug.LogError(string.Format("Invoking a method from a constructor failed. The exception was \"{0}\".", e.Message));
                return null;
            }
        }
        #endregion
        public ConstructorAttribute(string methodName)
        {
            this.methodName = methodName;
        }

        public ConstructorAttribute(Delegate method)
        {
            this.delegates.Add(method);
        }
    }
}