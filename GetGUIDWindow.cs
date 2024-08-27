using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using AssemblyDefinitionAsset = UnityEditorInternal.AssemblyDefinitionAsset;

namespace GetGUID
{
    // ReSharper disable once InconsistentNaming
    internal class GetGUIDWindow : EditorWindow
    {
        [MenuItem("Tools/Get GUID")]
        private static void OnMenu()
        {
            GetWindow<GetGUIDWindow>().Show();
        }
        
        private void CreateGUI()
        {
            ConstructUIElementsTo(rootVisualElement);
        }

        private static void ConstructUIElementsTo(VisualElement rootVisualElement)
        {
            var target = new ObjectField("object");
            rootVisualElement.Add(target);
            var typeNameField = new TextField("Type Name");
            rootVisualElement.Add(typeNameField);
            
            var typeSpecificInformation = new Foldout
            {
                text = "Type specific information"
            };
            
            target.RegisterValueChangedCallback(ev =>
                {
                    var v = ev.newValue;
                    typeSpecificInformation.text = v switch
                    {
                        MonoScript => "Script file information",
                        GameObject => "GameObject information",
                        _ => "Type specific information"
                    };
                });
            rootVisualElement.Add(typeSpecificInformation);
            typeSpecificInformation.Add(CreateScriptInformationUI(target));
            typeSpecificInformation.Add(CreateGameObjectInformationUI(target));

            var pathResult = new TextField("Relative path") { isReadOnly = true };
            rootVisualElement.Add(pathResult);
            var guidResult = new TextField("GUID") { isReadOnly = true };
            rootVisualElement.Add(guidResult);

            target.RegisterValueChangedCallback(ev =>
            {
                var newValue = ev.newValue;
                if (newValue == null)
                {
                    typeNameField.value = null;
                    pathResult.value = null;
                    guidResult.value = null;
                    return;
                }
                
                typeNameField.value = newValue.GetType().FullName;
                var pv = AssetDatabase.GetAssetPath(newValue);

                pathResult.value = FoldIrregularValue(pv);
                guidResult.value = FoldIrregularValue(AssetDatabase.AssetPathToGUID(pv));
            });
        }

        private static VisualElement CreateGameObjectInformationUI(ObjectField target)
        {
            var gameObjectSpecificInformation = new VisualElement
            {
                style = { display = DisplayStyle.None }
            };
            target.RegisterValueChangedCallback(ev =>
            {
                gameObjectSpecificInformation.style.display =
                    ev.newValue is GameObject ? DisplayStyle.Flex : DisplayStyle.None;
            });
            var outerPrefabReference = new ObjectField("Enclosing prefab") { objectType = typeof(GameObject) };
            target.RegisterValueChangedCallback(ev =>
            {
                var newValue = ev.newValue as GameObject;
                if (newValue == null)
                {
                    outerPrefabReference.value = null;
                    return;
                }
                    
                outerPrefabReference.value = PrefabUtility.GetNearestPrefabInstanceRoot(newValue);
            });
            gameObjectSpecificInformation.Add(outerPrefabReference);
                
            var sceneOnlyObjectHelpBox = new HelpBox("このオブジェクトはファイルシステムに存在しないため、GUIDを持ちません", HelpBoxMessageType.Info)
            {
                style =
                {
                    display = DisplayStyle.None
                }
            };

            target.RegisterValueChangedCallback(ev =>
            {
                var newValue = ev.newValue;
                var path = AssetDatabase.GetAssetPath(newValue);
                sceneOnlyObjectHelpBox.style.display =
                    string.IsNullOrEmpty(path) ? DisplayStyle.Flex : DisplayStyle.None;
            });
            gameObjectSpecificInformation.Add(sceneOnlyObjectHelpBox);
            return gameObjectSpecificInformation;
        }

        private static string FoldIrregularValue(string x) =>
            x switch
            {
                null => "???",
                "" => "<<<scene object>>>",
                _ => x,
            };

        private static VisualElement CreateScriptInformationUI(ObjectField target)
        {
            var assemblySpecificInformation = new VisualElement
            {
                style = { display = DisplayStyle.None }
            };
            target.RegisterValueChangedCallback(ev =>
            {
                assemblySpecificInformation.style.display =
                    ev.newValue is MonoScript ? DisplayStyle.Flex : DisplayStyle.None;
            });
                
            var correspondingAssemblyInfo = new TextField("Assembly") { isReadOnly = true };
            assemblySpecificInformation.Add(correspondingAssemblyInfo);
            var unityAssemblyDefinitionAssetReference = new ObjectField("asmdef")
                { objectType = typeof(AssemblyDefinitionAsset) };
            assemblySpecificInformation.Add(unityAssemblyDefinitionAssetReference);
            target.RegisterValueChangedCallback(ev =>
            {
                var v = ev.newValue;
                if (v == null)
                {
                    correspondingAssemblyInfo.value = null;
                    return;
                }

                if (v is not MonoScript ms)
                {
                    correspondingAssemblyInfo.value = "N/A";
                    return;
                }

                correspondingAssemblyInfo.value = ms.GetClass().Assembly.ToString();
            });
            target.RegisterValueChangedCallback(ev =>
            {
                var v = ev.newValue;
                if (v == null || v is not MonoScript ms)
                {
                    unityAssemblyDefinitionAssetReference.value = null;
                    return;
                }

                unityAssemblyDefinitionAssetReference.value = UnityAssemblyDefinitionFinder.FindDefinition(ms);
            });

            return assemblySpecificInformation;
        }
    }

    [System.Serializable]
    internal struct PartialAssemblyDefinition
    {
        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Corresponds to <see cref="System.Reflection.AssemblyName"/>.<see cref="System.Reflection.AssemblyName.Name"/>.
        /// </summary>
        public string name;
    }
}
