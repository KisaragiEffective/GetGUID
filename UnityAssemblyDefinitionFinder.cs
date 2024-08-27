using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using AssemblyDefinitionAsset = UnityEditorInternal.AssemblyDefinitionAsset;

namespace GetGUID
{
    internal static class UnityAssemblyDefinitionFinder
    {
        /// <summary>
        /// 与えられた <see cref="UnityEditor.MonoScript"/> が所属する <see cref="UnityEditorInternal.AssemblyDefinitionAsset"/> を
        /// 取得する。
        /// <br />
        /// もし<c>Assembly-CSharp</c>などの黙示的な <see cref="System.Reflection.Assembly"/> に所属する場合は、<c>null</c>を返す。
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        [CanBeNull]
        internal static AssemblyDefinitionAsset FindDefinition(MonoScript ms)
        {
            var assembly = ms.GetClass().Assembly;
            var query = $"t:{typeof(AssemblyDefinitionAsset).FullName}";

            var candidates = AssetDatabase
                .FindAssets(query)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AuxContainer.Create(
                    AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path),
                    path
                ))
                .Select(d => d.Map(a => a.text))
                .Select(d => d.Map(JsonUtility.FromJson<PartialAssemblyDefinition>))
                // ReSharper disable once PossibleInvalidOperationException
                .Where(d => d.main.name == assembly.GetName().Name)
                .ToList();

            var assemblyDefinitionPath = candidates.SingleOrDefault()?.aux;
            return assemblyDefinitionPath != null
                ? AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assemblyDefinitionPath)
                : null;
        }
    }
}
