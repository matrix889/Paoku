using System;
using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// Changes the color of the background of an expandable item.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
    public class BackgroundAttribute : Attribute
    {
        private Color color = Color.clear;

        /// <summary>
        /// Give this item's background a color.
        /// </summary>
        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public BackgroundAttribute(float r, float g, float b)
            : this(r, g, b, 1) { }

        public BackgroundAttribute(float r, float g, float b, float a)
        {
            this.color = new Color(r, g, b, a);
        }
    }
}