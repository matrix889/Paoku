using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// When a property is flagged this way, a help box is added after the inspector's field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class HelpAttribute : Attribute, IRuntimeAttribute<HelpAttribute>
    {
        public delegate HelpAttribute HelpDelegate();

        private HelpType type;

        /// <summary>
        /// Help type.
        /// Displays a specific icon.
        /// </summary>
        public HelpType Type
        {
            get { return type; }
            set { type = value; }
        }

        private string message;

        /// <summary>
        /// Help message.
        /// </summary>
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        private HelpPosition position = HelpPosition.After;
        
        /// <summary>
        /// By default, the helpbox is drawn after the field.
        /// If this is false, it is drawn before the field.
        /// </summary>
        public HelpPosition Position
        {
            get { return position; }
            set { position = value; }
        }

        #region IRuntime Implementation
        private string methodName = "";

        public string MethodName
        {
            get { return methodName; }
        }

        public Type Template
        {
            get { return typeof(HelpDelegate); }
        }

        private List<Delegate> delegates = new List<Delegate>();

        public List<Delegate> Delegates
        {
            get { return delegates; }
            set { delegates = value; }
        }

        public HelpAttribute Invoke(int index)
        {
            if (delegates.Count == 0 || index >= delegates.Count)
                return this;

            try
            {
                return delegates[0].DynamicInvoke() as HelpAttribute;
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = ((TargetInvocationException)e).InnerException;

                Debug.LogError(string.Format("Invoking a method failed while trying to retrieve a Help attribute. The exception was \"{0}\".", e.Message));
                return null;
            }
        }
        #endregion

        public HelpAttribute(string methodName)
        {
            this.methodName = methodName;
        }

        public HelpAttribute(HelpType type, string message)
        {
            this.type = type;
            this.message = message;
        }

        public HelpAttribute(HelpType type, HelpPosition position, string message)
        {
            this.type = type;
            this.position = position;
            this.message = message;
        }

        public HelpAttribute(Delegate method)
        {
            this.delegates.Add(method);
        }
    }

    /// <summary>
    /// Because the internal enum for help display is Editor only.
    /// </summary>
    public enum HelpType
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    /// <summary>
    /// The position where the help box should be placed.
    /// </summary>
    public enum HelpPosition
    { 
        After,
        Before
    }
}