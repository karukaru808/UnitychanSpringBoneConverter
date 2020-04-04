using System.Linq;
using UnityEngine;
using UTJ;
using VRM;

namespace CIFER.Tech.UnitychanSpringBoneConverter.ForVRM
{
    public static class UnitychanSpringBoneConverterForVrm
    {
        public static void ToVrmSpringBone(UnitychanSpringBoneConverterData converterData)
        {
            var secondaryTransform = converterData.ConvertRoot.transform.Find("secondary");

            //SpringBone Root
            var springBoneRoots =
                UnitychanSpringBoneConverter.GetAllUnitychanSpringBoneRoot(converterData.SpringManager);

            //ConvertBone Parent
            var convertRootBoneParents = springBoneRoots
                .Select(sbRoot =>
                    UnitychanSpringBoneConverter.FindSameNameTransformInChildren(sbRoot.transform.parent.name,
                        converterData.ConvertRoot))
                .Distinct()
                .ToList();

            //Delete Exist Setting
            if (converterData.IsDeleteExistSetting)
            {
                UnitychanSpringBoneConverter.DeleteExistSetting<VRMSpringBone>(secondaryTransform, false);
                UnitychanSpringBoneConverter.DeleteExistSetting<VRMSpringBoneColliderGroup>(converterData.ConvertRoot,
                    true);
            }

            //VRMSpringBoneColliderGroup Convert
            //SphereCollider Convert
            foreach (var sphereCollider in converterData.SpringManager.GetComponentsInChildren<SpringSphereCollider>())
            {
                var parentName = sphereCollider.transform.parent.name;
                var isExistColliderGroup = UnitychanSpringBoneConverter
                    .FindSameNameTransformInChildren(parentName, converterData.ConvertRoot)
                    .GetComponent<VRMSpringBoneColliderGroup>() != null;

                var colliderGroup =
                    UnitychanSpringBoneConverter.FindOrCreateT<VRMSpringBoneColliderGroup>(parentName,
                        converterData.ConvertRoot);
                if (!isExistColliderGroup)
                    colliderGroup.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[0];

                var colliderList = colliderGroup.Colliders.ToList();
                colliderList.Add(new VRMSpringBoneColliderGroup.SphereCollider
                    {
                        Offset = sphereCollider.transform.localPosition,
                        //Scaleの影響を考える
                        //UnitychanSBはScaleの影響を受けない
                        Radius = sphereCollider.radius
                    }
                );
                colliderGroup.Colliders = colliderList.ToArray();
            }

            //CapsuleCollider Convert
            foreach (var capsuleCollider in converterData.SpringManager.GetComponentsInChildren<SpringCapsuleCollider>()
            )
            {
                var parentName = capsuleCollider.transform.parent.name;
                var isExistColliderGroup = UnitychanSpringBoneConverter
                    .FindSameNameTransformInChildren(parentName, converterData.ConvertRoot)
                    .GetComponent<VRMSpringBoneColliderGroup>();

                var colliderGroup =
                    UnitychanSpringBoneConverter.FindOrCreateT<VRMSpringBoneColliderGroup>(parentName,
                        converterData.ConvertRoot);
                if (!isExistColliderGroup)
                    colliderGroup.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[0];

                var colliderList = colliderGroup.Colliders.ToList();
                var colliderPieces = (int) (capsuleCollider.height / capsuleCollider.radius) + 1;

                var offsetStart = capsuleCollider.transform.localPosition;
                var offsetEnd = capsuleCollider.height * capsuleCollider.transform.TransformVector(Vector3.up) +
                                offsetStart;

                for (var i = 0; i <= colliderPieces; i++)
                {
                    colliderList.Add(new VRMSpringBoneColliderGroup.SphereCollider
                        {
                            Offset = Vector3.Lerp(offsetStart, offsetEnd, (float) i / colliderPieces),
                            Radius = capsuleCollider.radius
                        }
                    );
                }

                colliderGroup.Colliders = colliderList.ToArray();
            }

            //VRMSpringBone Attach
            foreach (var convertParent in convertRootBoneParents)
            {
                var convertParentName = convertParent.name;
                var sameParentSpringBones = springBoneRoots
                    .Where(sbRoot => sbRoot.transform.parent.name == convertParent.name)
                    .ToList();

                //VRMSpringBone init
                var vrmSpringBone = secondaryTransform.gameObject.AddComponent<VRMSpringBone>();
                vrmSpringBone.m_comment = convertParentName;
                vrmSpringBone.ColliderGroups = new VRMSpringBoneColliderGroup[0];

                //VRMSpringBone RootBone Attach
                vrmSpringBone.RootBones.AddRange(sameParentSpringBones
                    .Select(sbRoot =>
                        UnitychanSpringBoneConverter.FindSameNameTransformInChildren(sbRoot.name,
                            converterData.ConvertRoot))
                    .ToList());

                var averageSpringForce = new Vector3(sameParentSpringBones.Average(sb => sb.springForce.x),
                    sameParentSpringBones.Average(sb => sb.springForce.y),
                    sameParentSpringBones.Average(sb => sb.springForce.z));

                vrmSpringBone.m_hitRadius = sameParentSpringBones.Average(sb => sb.radius);
                vrmSpringBone.m_stiffnessForce = sameParentSpringBones.Average(sb => sb.stiffnessForce) * 0.0008f;
                vrmSpringBone.m_dragForce = sameParentSpringBones.Average(sb => sb.dragForce);
                vrmSpringBone.m_gravityPower = 1f;
                vrmSpringBone.m_gravityDir = (averageSpringForce + converterData.SpringManager.gravity) / 10f;

                //VRMSpringBoneColliderGroup Attach
                var scParentName = sameParentSpringBones
                    .SelectMany(sbRoot => sbRoot.sphereColliders)
                    .Select(sc => sc.transform.parent.name)
                    .Distinct();
                //.ToList();
                var ccParentName = sameParentSpringBones
                    .SelectMany(sbRoot => sbRoot.capsuleColliders)
                    .Select(cc => cc.transform.parent.name)
                    .Distinct();
                //.ToList();

                var convertSphereCollider = scParentName
                    .Select(name =>
                        UnitychanSpringBoneConverter.FindSameNameTransformInChildren(name, converterData.ConvertRoot)
                            .GetComponent<VRMSpringBoneColliderGroup>())
                    .ToList();

                var convertCapsuleCollider = ccParentName
                    .Select(name =>
                        UnitychanSpringBoneConverter.FindSameNameTransformInChildren(name, converterData.ConvertRoot)
                            .GetComponent<VRMSpringBoneColliderGroup>())
                    .ToList();

                var colliderGroups = vrmSpringBone.ColliderGroups.ToList();
                colliderGroups.AddRange(convertSphereCollider);
                colliderGroups.AddRange(convertCapsuleCollider);
                vrmSpringBone.ColliderGroups = colliderGroups.ToArray();
            }

            Debug.Log($"{typeof(UnitychanSpringBoneConverterForVrm).ToString().Split('.').Last()} Complete!");
        }
    }
}