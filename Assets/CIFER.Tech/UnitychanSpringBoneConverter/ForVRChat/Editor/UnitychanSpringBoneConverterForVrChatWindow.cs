using System;
using UnityEditor;
using UnityEngine;

namespace CIFER.Tech.UnitychanSpringBoneConverter.ForVRChat
{
    public class UnitychanSpringBoneConverterForVrChatWindow : UnitychanSpringBoneConverterWindow
    {
        protected override Type AfterClassType => typeof(Animator);

        private DynamicBone.UpdateMode _updateMode = DynamicBone.UpdateMode.Normal;
        private bool _distantDisable = true;

        [MenuItem("CIFER.Tech/UnitychanSpringBoneConverter/For VRChat")]
        public static void Open()
        {
            var window = GetWindow<UnitychanSpringBoneConverterForVrChatWindow>("USBC For VRChat");
            window.minSize = MinWindowSize;
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (IsAnyNull1)
                return;

            if (!GUILayout.Button("変換！"))
                return;

            var converterData = new UnitychanSpringBoneConverterData()
            {
                SpringManager = SpringManager,
                ConvertRoot = (AfterClassObject as Animator)?.transform,
                IsDeleteExistSetting = IsDeleteExistSetting,
                UpdateModeIndex = (int) _updateMode,
                DistantDisable = _distantDisable,
            };
            UnitychanSpringBoneConverterForVrChat.ToDynamicBone(converterData);
        }

        protected override void AdditionalSettings()
        {
            base.AdditionalSettings();
            _updateMode = (DynamicBone.UpdateMode) EditorGUILayout.EnumPopup("UpdateMode", _updateMode);
            _distantDisable = EditorGUILayout.Toggle("DistantDisable", _distantDisable);
        }
    }
}