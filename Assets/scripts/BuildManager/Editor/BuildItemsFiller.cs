using UnityEngine;
using UnityEditor;
using SimpleBuildingSystem;

namespace SimpleBuildingSystem.EditorTools
{
    // Popula automaticamente o array buildItems do BuildingController com todos os
    // objetos que estão dentro do container "Buildobjects" na cena.
    //
    // Uso: menu Tools -> Build System -> Populate BuildItems from Buildobjects
    public static class BuildItemsFiller
    {
        [MenuItem("Tools/Build System/Populate BuildItems from Buildobjects")]
        public static void Populate()
        {
            GameObject buildObjects = GameObject.Find("Buildobjects");
            if (buildObjects == null)
            {
                Debug.LogError("Não encontrei o GameObject 'Buildobjects' na cena.");
                return;
            }

            BuildingController controller = Object.FindFirstObjectByType<BuildingController>();
            if (controller == null)
            {
                Debug.LogError("Não encontrei nenhum BuildingController na cena.");
                return;
            }

            Undo.RecordObject(controller, "Populate buildItems from Buildobjects");

            var list = new System.Collections.Generic.List<BuildItemCost>();
            foreach (Transform child in buildObjects.transform)
            {
                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) as GameObject;
                if (prefab == null)
                    prefab = child.gameObject;

                var cost = new BuildItemCost();
                cost.prefab = prefab;
                cost.metalCost = 0;
                cost.costItem = FindIronIngot();
                list.Add(cost);
                Debug.Log($"Adicionado buildItem: {prefab.name}");
            }

            controller.buildItems = list.ToArray();
            EditorUtility.SetDirty(controller);
            Debug.Log($"buildItems preenchido com {list.Count} itens. Define os metalCost no inspector conforme quiseres.");
        }

        private static ItemSO FindIronIngot()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemSO IronIngot");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("IronIngot.asset"))
                    return AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            }
            return null;
        }
    }
}
