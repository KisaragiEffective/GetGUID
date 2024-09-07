using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using AssemblyDefinitionAsset = UnityEditorInternal.AssemblyDefinitionAsset;

namespace GetGUID.Editor
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
            typeSpecificInformation.Add(CreateDLLInformationUI(target));
            typeSpecificInformation.Add(CreateMaterialSpecificInformationUI(target));

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

        private static VisualElement CreateMaterialSpecificInformationUI(ObjectField target)
        {
            var materialSpecificInformation = new VisualElement
            {
                style = { display = DisplayStyle.None }
            };
            target.RegisterValueChangedCallback(ev =>
            {
                materialSpecificInformation.style.display =
                    ev.newValue is Material ? DisplayStyle.Flex : DisplayStyle.None;
            });

            var parent = new ObjectField("parent") { objectType = typeof(Material) };
            materialSpecificInformation.Add(parent);
            var shader = new ObjectField("shader") { objectType = typeof(Shader) };
            materialSpecificInformation.Add(shader);
            var shaderPath = new TextField("shader path");
            materialSpecificInformation.Add(shaderPath);
            var shaderGuid = new TextField("shader GUID");
            materialSpecificInformation.Add(shaderGuid);

            target.RegisterValueChangedCallback(ev =>
            {
                var newValue = ev.newValue as Material;
                if (newValue == null)
                {
                    parent.value = null;
                    return;
                }

                parent.value = newValue.parent;
                shader.value = newValue.shader;
                shaderPath.value = AssetDatabase.GetAssetPath(newValue.shader);
                shaderGuid.value = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newValue.shader));
            });

            return materialSpecificInformation;
        }

        private static VisualElement CreateDLLInformationUI(ObjectField target)
        {
            var information = new VisualElement()
            {
                style = { display = DisplayStyle.None }
            };

            var isCompatibleForEditor = new Toggle("Editor compatible") { value = false };
            information.Add(isCompatibleForEditor);
            
            target.RegisterValueChangedCallback(ev =>
            {
                var v = ev.newValue;
                var g = GetPluginImporter(v);
                information.style.display = g != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            });
            target.RegisterValueChangedCallback(ev =>
            {
                var g = GetPluginImporter(ev.newValue);
                if (g == null)
                {
                    return;
                }

                isCompatibleForEditor.value = g.GetCompatibleWithEditor();
            });

            return information;

            PluginImporter? GetPluginImporter([CanBeNull] Object asset)
            {
                return asset is DefaultAsset ? 
                    AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(asset)) as PluginImporter : null;
            }
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
