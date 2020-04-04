using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UTJ;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace CIFER.Tech.UnitychanSpringBoneConverter
{
    public static class UnitychanSpringBoneConverter
    {
        public static List<SpringBone> GetAllUnitychanSpringBoneRoot(SpringManager springManager)
        {
            return springManager.springBones
                .Where(sb => sb != null && sb.transform.parent.GetComponent<SpringBone>() == null)
                .Distinct()
                .ToList();
        }

        public static Transform FindSameNameTransformInChildren(string name, Transform searchRoot)
        {
            return searchRoot.GetComponentsInChildren<Transform>().FirstOrDefault(tf => tf.name == name);
        }

        public static T FindOrCreateT<T>(string searchName, Transform searchRoot) where T : Component
        {
            var convertGameObject = FindSameNameTransformInChildren(searchName, searchRoot)?.gameObject;
            if (convertGameObject == null)
                convertGameObject = new GameObject(searchName);

            var component = convertGameObject.GetComponent<T>();
            if (component == null)
                component = convertGameObject.AddComponent<T>();

            return component;
        }

        public static void DeleteExistSetting<T>(Transform target, bool isIncludeChildren) where T : Component
        {
            foreach (var targetClass in isIncludeChildren
                ? target.GetComponentsInChildren<T>()
                : target.GetComponents<T>())
                Object.DestroyImmediate(targetClass);
        }
    }

#if UNITY_EDITOR
    public abstract class UnitychanSpringBoneConverterWindow : EditorWindow
    {
        protected static readonly Vector2 MinWindowSize = new Vector2(350f, 200f);
        protected abstract Type AfterClassType { get; }

        protected SpringManager SpringManager { get; private set; }
        protected Component AfterClassObject;
        protected bool IsDeleteExistSetting = true;

        protected bool IsAnyNull1 { get; private set; } = true;

        public virtual void OnGUI()
        {
            SpringManager =
                EditorGUILayout.ObjectField("変換元のSpringManager", SpringManager, typeof(SpringManager), true) as
                    SpringManager;
            AfterClassObject =
                EditorGUILayout.ObjectField($"変換先の{AfterClassType.ToString().Split('.').Last()}", AfterClassObject,
                    AfterClassType, true) as Component;

            EditorGUILayout.Space();

            IsDeleteExistSetting = EditorGUILayout.Toggle("既存の揺れ物設定を削除する。", IsDeleteExistSetting);

            AdditionalSettings();

            GUILayout.FlexibleSpace();
            IsAnyNull1 = SpringManager == null || AfterClassObject == null;
            if (IsAnyNull1)
            {
                EditorGUILayout.HelpBox("変換元／先が選択されていません。", MessageType.Error);
                return;
            }

            EditorGUILayout.Space();
        }

        protected virtual void AdditionalSettings()
        {
            EditorGUILayout.Space();
        }
    }
#endif
}