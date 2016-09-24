// Copyright ?2011, Zero and One Computing Inc. All rights reserved.
// Distributed under terms of the Modified BSD License, see license.txt for details.
//
// Find.MissingScripts.cs - find game objects using scripts that no longer exist
//
// History:
// version 1.0 - January 2011 - Yossarian King

using System;

using UnityEngine;
using UnityEditor;

namespace ZeroAndOne.UnityEditor
{
    public static class FindMissingScriptsMenuItem
    {
        // Search the scene looking for game object referencing script components
        // that no longer exist. Select the objects.
        [MenuItem("Tools/程序/查找/场景中丢失脚本的对象")]
        private static void FindMissingScriptsMenuItemHandler()
        {
            GameObject[] gameObjects = ObjectHelper.FindGameObjectsWithMissingScripts();
            if (gameObjects.Length > 0)
            {
                // Select the identified objects.
                Selection.objects = gameObjects;

                foreach (GameObject gameObject in gameObjects)
                {
                    Debug.LogWarning(String.Format("{0} is missing a script.", gameObject.name), gameObject);
                }
            }
        }
    }
}
