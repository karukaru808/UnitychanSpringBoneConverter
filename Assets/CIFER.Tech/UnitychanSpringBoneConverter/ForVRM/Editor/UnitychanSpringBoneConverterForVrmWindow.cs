using System;
using UnityEditor;
using UnityEngine;
using VRM;

namespace CIFER.Tech.UnitychanSpringBoneConverter.ForVRM
{
    public class UnitychanSpringBoneConverterForVrmWindow : UnitychanSpringBoneConverterWindow
    {
        protected override Type AfterClassType => typeof(VRMMeta);

        [MenuItem("CIFER.Tech/UnitychanSpringBoneConverter/For VRM")]
        public static void Open()
        {
            var window = GetWindow<UnitychanSpringBoneConverterForVrmWindow>("USBC For VRM");
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
                ConvertRoot = (AfterClassObject as VRMMeta)?.transform,
                IsDeleteExistSetting = IsDeleteExistSetting,
            };
            UnitychanSpringBoneConverterForVrm.ToVrmSpringBone(converterData);
        }

        protected override void AdditionalSettings()
        {
            base.AdditionalSettings();
        }
    }
}