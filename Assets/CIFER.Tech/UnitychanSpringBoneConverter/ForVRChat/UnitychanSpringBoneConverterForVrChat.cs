using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UTJ;

namespace CIFER.Tech.UnitychanSpringBoneConverter.ForVRChat
{
    public static class UnitychanSpringBoneConverterForVrChat
    {
        public static void ToDynamicBone(UnitychanSpringBoneConverterData converterData)
        {
            //SpringBone Root
            var springBoneRoots =
                UnitychanSpringBoneConverter.GetAllUnitychanSpringBoneRoot(converterData.SpringManager);

            //ConvertBone Root
            var convertRootBones = springBoneRoots
                .Select(springBone =>
                    UnitychanSpringBoneConverter.FindSameNameTransformInChildren(springBone.name,
                        converterData.ConvertRoot))
                .Distinct()
                .ToList();

            //Delete Exist Setting
            if (converterData.IsDeleteExistSetting)
            {
                UnitychanSpringBoneConverter.DeleteExistSetting<DynamicBone>(converterData.ConvertRoot, true);
                UnitychanSpringBoneConverter.DeleteExistSetting<DynamicBoneColliderBase>(converterData.ConvertRoot,
                    true);
            }

            //VRMSpringBoneColliderGroup Convert
            //SphereCollider Convert
            foreach (var sphereCollider in converterData.SpringManager.GetComponentsInChildren<SpringSphereCollider>())
            {
                var sphereParent = sphereCollider.transform.parent;
                var convertColliderParent =
                    UnitychanSpringBoneConverter.FindSameNameTransformInChildren(sphereParent.name,
                        converterData.ConvertRoot);

                var dynamicCollider =
                    UnitychanSpringBoneConverter.FindOrCreateT<DynamicBoneCollider>(sphereCollider.name,
                        converterData.ConvertRoot);

                var convertColliderTransform = dynamicCollider.transform;
                SetParentAndReset(ref convertColliderTransform, convertColliderParent);

                //Scaleの影響を考える
                //UnitychanSBはScaleの影響を受けない
                //DynamicBoneSphereColliderはX軸Scaleだけ影響を受ける
                dynamicCollider.m_Radius = sphereCollider.radius;
            }

            //CapsuleCollider Convert
            foreach (var capsuleCollider in converterData.SpringManager.GetComponentsInChildren<SpringCapsuleCollider>()
            )
            {
                var capsuleTransform = capsuleCollider.transform;
                var convertColliderParent =
                    UnitychanSpringBoneConverter.FindSameNameTransformInChildren(capsuleTransform.parent.name,
                        converterData.ConvertRoot);

                var dynamicCollider =
                    UnitychanSpringBoneConverter.FindOrCreateT<DynamicBoneCollider>(capsuleCollider.name,
                        converterData.ConvertRoot);
                var convertColliderTransform = dynamicCollider.transform;

                SetParentAndReset(ref convertColliderTransform, convertColliderParent);

                var offsetStart = capsuleCollider.transform.localPosition;
                var offsetEnd = capsuleCollider.height * capsuleCollider.transform.TransformVector(Vector3.up) +
                                offsetStart;
                convertColliderTransform.localPosition = Vector3.Lerp(offsetStart, offsetEnd, 0.5f);
                convertColliderTransform.localRotation = capsuleTransform.localRotation;

                dynamicCollider.m_Radius = capsuleCollider.radius;
                dynamicCollider.m_Height = (capsuleCollider.radius * 2 + Vector3.Distance(offsetStart, offsetEnd));
            }

            //PlaneCollider Convert
            //挙動的にScale考慮してないので無視して実装してOK
            foreach (var panelCollider in converterData.SpringManager.GetComponentsInChildren<SpringPanelCollider>())
            {
                var panelTransform = panelCollider.transform;
                var convertColliderParent =
                    UnitychanSpringBoneConverter.FindSameNameTransformInChildren(panelTransform.parent.name,
                        converterData.ConvertRoot);
                var convertColliderGameObject =
                    UnitychanSpringBoneConverter.FindOrCreateT<DynamicBonePlaneCollider>(panelCollider.name,
                        converterData.ConvertRoot);

                var convertColliderTransform = convertColliderGameObject.transform;
                SetParentAndReset(ref convertColliderTransform, convertColliderParent);

                convertColliderTransform.localPosition = panelTransform.localPosition;
                convertColliderTransform.localRotation = panelTransform.localRotation;
            }

            //GroundCollision Convert
            if (converterData.SpringManager.collideWithGround)
            {
                var dynamicCollider = converterData.ConvertRoot.gameObject.AddComponent<DynamicBonePlaneCollider>();
                dynamicCollider.m_Direction = DynamicBoneColliderBase.Direction.Y;
            }

            //DynamicBone Attach
            for (var i = 0; i < convertRootBones.Count; i++)
            {
                var dynamicBone = convertRootBones[i].gameObject.AddComponent<DynamicBone>();
                var springBones = springBoneRoots[i].GetComponentsInChildren<SpringBone>();

                dynamicBone.m_Root = dynamicBone.transform;
                dynamicBone.m_UpdateRate = converterData.SpringManager.simulationFrameRate;
                dynamicBone.m_UpdateMode = (DynamicBone.UpdateMode) converterData.UpdateModeIndex;

                dynamicBone.m_Damping = 1f;
                dynamicBone.m_DampingDistrib = new AnimationCurve(springBones
                    .Select((sb, index) =>
                        new Keyframe(index / (springBones.Length - 1f), sb.dragForce, 0f, 0f))
                    .ToArray());

                //この値UnitychanSBにはない値だよぅ…
                //値が小さい方が綺麗に揺れる
                //1だと完全に動かない
                //Stiffnessと同じでもいいかも？
                dynamicBone.m_Elasticity = 1f;
                dynamicBone.m_ElasticityDistrib = new AnimationCurve(springBones
                    .Select((sb, index) =>
                        new Keyframe(index / (springBones.Length - 1f), sb.stiffnessForce / 5000f, 0f, 0f))
                    .ToArray());

                //1だと完全に動かない
                dynamicBone.m_Stiffness = 1f;
                dynamicBone.m_StiffnessDistrib = new AnimationCurve(springBones
                    .Select((sb, index) =>
                        new Keyframe(index / (springBones.Length - 1f), sb.stiffnessForce / 5000f, 0f, 0f))
                    .ToArray());

                dynamicBone.m_Radius = springBones.Max(sb => sb.radius);
                dynamicBone.m_RadiusDistrib = new AnimationCurve(springBones
                    .Select((sb, index) =>
                        new Keyframe(index / (springBones.Length - 1f), sb.radius / dynamicBone.m_Radius, 0f, 0f))
                    .ToArray());

                //0でマイナス方向に力が働いている？
                dynamicBone.m_Gravity = new Vector3(springBones.Average(sb => sb.springForce.x),
                    springBones.Average(sb => sb.springForce.y),
                    springBones.Average(sb => sb.springForce.z)) / 100f;

                dynamicBone.m_Force = converterData.SpringManager.gravity + (Vector3.up * 10);

                //DynamicBone Collider Attach
                var sbColliderName = new List<string>();
                sbColliderName.AddRange(springBones.SelectMany(sb => sb.sphereColliders)
                    .Select(collider => collider.name));
                sbColliderName.AddRange(springBones.SelectMany(sb => sb.capsuleColliders)
                    .Select(collider => collider.name));
                sbColliderName.AddRange(springBones.SelectMany(sb => sb.panelColliders)
                    .Select(collider => collider.name));

                dynamicBone.m_Colliders = new List<DynamicBoneColliderBase>();
                sbColliderName.Distinct().ToList().ForEach(colliderName =>
                {
                    var collider = UnitychanSpringBoneConverter
                        .FindSameNameTransformInChildren(colliderName, converterData.ConvertRoot)
                        .GetComponent<DynamicBoneCollider>();
                    if (collider != null)
                        dynamicBone.m_Colliders.Add(collider);

                    var planeCollider = UnitychanSpringBoneConverter
                        .FindSameNameTransformInChildren(colliderName, converterData.ConvertRoot)
                        .GetComponent<DynamicBonePlaneCollider>();
                    if (planeCollider != null)
                        dynamicBone.m_Colliders.Add(planeCollider);
                });

                if (converterData.SpringManager.collideWithGround)
                    dynamicBone.m_Colliders.Add(converterData.ConvertRoot.GetComponent<DynamicBonePlaneCollider>());

                //DynamicBone Exclusions Attach
                if (dynamicBone.m_Exclusions == null)
                    dynamicBone.m_Exclusions = new List<Transform>();

                var exclusionsTransforms = springBoneRoots[i]
                    .GetComponentsInChildren<Transform>()
                    .Where(tf => tf.parent.GetComponent<SpringBone>() == null && tf.name != dynamicBone.m_Root.name)
                    .Select(tf =>
                        UnitychanSpringBoneConverter.FindSameNameTransformInChildren(tf.name,
                            converterData.ConvertRoot))
                    .Distinct()
                    .ToList();
                exclusionsTransforms.RemoveAll(tf => tf == null);
                dynamicBone.m_Exclusions.AddRange(exclusionsTransforms);

                //DynamicBone FreezeAxis Attach
                //指定した軸 "のみ" 動かすという設定だった…
                //dynamicBone.m_FreezeAxis = DynamicBone.FreezeAxis.None;

                //DynamicBone DistantDisable Attach
                dynamicBone.m_DistantDisable = converterData.DistantDisable;
            }

            Debug.Log($"{typeof(UnitychanSpringBoneConverterForVrChat).ToString().Split('.').Last()} Complete!");
        }

        /// <summary>
        /// tfの親をparentに設定して、Position, localRotation, localScaleをゼロに設定する
        /// </summary>
        /// <param name="tf"></param>
        /// <param name="parent"></param>
        private static void SetParentAndReset(ref Transform tf, Transform parent)
        {
            tf.SetParent(parent);

            tf.position = Vector3.zero;
            tf.localRotation = Quaternion.identity;
            tf.localScale = new Vector3(1f, 1f, 1f);
        }
    }
}