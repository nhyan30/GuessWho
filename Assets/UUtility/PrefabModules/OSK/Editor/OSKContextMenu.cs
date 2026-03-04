using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UTool;

namespace UTool.Editor
{
    public class OSKContextMenu
    {
        private const string workingPath = "Assets/UUtility/PrefabModules/OSK";
        private static string multiOSKPath => $"{workingPath}/MultiOSK.prefab";
        private static string enOSKPath => $"{workingPath}/EnOSK.prefab";
        private static string enArOSKPath => $"{workingPath}/EnArOSK.prefab";
        private static string emailOSKPath => $"{workingPath}/EmailOSK.prefab";

        [MenuItem("GameObject/UT/OSK/Multi OSK", false, 200)]
        private static void CreateMultiOSK(MenuCommand command)
        {
            GameObject osk = UTContextMenuUtility.CreateAssetFromPath(command, multiOSKPath, prefabLink: true);
        }

        [MenuItem("GameObject/UT/OSK/English OSK", false, 201)]
        private static void CreateEnOSK(MenuCommand command)
        {
            GameObject osk = UTContextMenuUtility.CreateAssetFromPath(command, enOSKPath, prefabLink: true);
        }

        [MenuItem("GameObject/UT/OSK/Engish Arabic OSK", false, 202)]
        private static void CreateEnArOSK(MenuCommand command)
        {
            GameObject osk = UTContextMenuUtility.CreateAssetFromPath(command, enArOSKPath, prefabLink: true);
        }

        [MenuItem("GameObject/UT/OSK/Email OSK", false, 203)]
        private static void CreateEmailOSK(MenuCommand command)
        {
            GameObject osk = UTContextMenuUtility.CreateAssetFromPath(command, emailOSKPath, prefabLink: true);
        }
    }
}