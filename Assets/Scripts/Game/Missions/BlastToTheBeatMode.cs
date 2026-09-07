using System.Collections.Generic;
using UnityEngine;

namespace PlatinumQuestScripts
{
    public class BlastToTheBeatMode : ISpecialGameMode
    {
        private readonly GameObject missionRoot;

        private class ColliderProxy
        {
            public Transform beatRoot;
            public GameObject proxyObject;

            // Original collider position relative to the beat root.
            public Vector3 worldOffset;

            // Original collider rotation relative to the beat root.
            public Quaternion relativeRotation;
        }

        private readonly List<ColliderProxy> colliderProxies =
            new List<ColliderProxy>();

        private readonly HashSet<MeshCollider> processedColliders =
            new HashSet<MeshCollider>();

        public BlastToTheBeatMode(GameObject missionRoot)
        {
            this.missionRoot = missionRoot;
        }

        public void OnMissionLoad()
        {
            if (missionRoot == null)
                return;

            CreateBeatColliderProxies();
        }

        public void OnRestart()
        {
            GameManager.instance.PlayBassPunchAudio();
        }

        public void OnRespawn()
        {
        }

        public void Update()
        {
            UpdateColliderProxies();
        }

        public void OnJump()
        {
        }

        public void ProcessMaterialContact(
            Marble marble,
            CollisionInfo contact)
        {
        }

        // ============================================================
        // FIND BEAT OBJECTS
        // ============================================================

        private void CreateBeatColliderProxies()
        {
            colliderProxies.Clear();
            processedColliders.Clear();

            PathMover[] movers =
                missionRoot.GetComponentsInChildren<PathMover>(true);

            foreach (PathMover mover in movers)
            {
                if (mover == null)
                    continue;

                if (!mover.HasPath)
                    continue;

                string pathName = mover.CurrentNode;

                if (string.IsNullOrEmpty(pathName))
                    continue;

                if (!pathName.ToLowerInvariant().Contains("beat"))
                    continue;

                GameObject beatObject = mover.gameObject;

                // ----------------------------------------------------
                // ONLY TREES AND INTERIORINSTANCE.
                //
                // Everything else is left completely untouched.
                // This means TimeTravel, gems, powerups, etc. retain
                // their normal pulsing MeshColliders.
                // ----------------------------------------------------

                if (!IsTreeOrInteriorInstance(beatObject))
                    continue;

                CreateColliderProxiesForBeatObject(
                    beatObject
                );
            }
        }

        // ============================================================
        // OBJECT TYPE FILTER
        // ============================================================

        private bool IsTreeOrInteriorInstance(
            GameObject beatObject)
        {
            if (beatObject == null)
                return false;

            string objectName =
                beatObject.name.ToLowerInvariant();

            // --------------------------------------------------------
            // Tree
            // --------------------------------------------------------

            if (objectName == "tree" ||
                objectName.StartsWith("tree") ||
                objectName.Contains("tree"))
            {
                return true;
            }

            // --------------------------------------------------------
            // InteriorInstance
            // --------------------------------------------------------

            if (objectName == "interiorinstance" ||
                objectName.StartsWith("interiorinstance") ||
                objectName.Contains("interiorinstance"))
            {
                return true;
            }

            // --------------------------------------------------------
            // Also check the actual component name/type if the object
            // was imported with a component representing the object
            // type.
            // --------------------------------------------------------

            Component[] components =
                beatObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                string typeName =
                    component.GetType().Name;

                if (typeName.Equals(
                        "Tree",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (typeName.Equals(
                        "InteriorInstance",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // ============================================================
        // CREATE COLLIDER PROXIES
        // ============================================================

        private void CreateColliderProxiesForBeatObject(
            GameObject beatObject)
        {
            if (beatObject == null)
                return;

            MeshCollider[] colliders =
                beatObject.GetComponentsInChildren<MeshCollider>(true);

            foreach (MeshCollider sourceCollider in colliders)
            {
                if (sourceCollider == null)
                    continue;

                if (processedColliders.Contains(sourceCollider))
                    continue;

                if (sourceCollider.sharedMesh == null)
                    continue;

                processedColliders.Add(sourceCollider);

                CreateColliderProxy(
                    beatObject.transform,
                    sourceCollider
                );
            }
        }

        private void CreateColliderProxy(
            Transform beatRoot,
            MeshCollider sourceCollider)
        {
            Transform sourceTransform =
                sourceCollider.transform;

            // --------------------------------------------------------
            // Capture the collider's original WORLD position relative
            // to the beat root.
            //
            // We must do this before the beat path changes its scale.
            // --------------------------------------------------------

            Vector3 worldOffset =
                Quaternion.Inverse(beatRoot.rotation) *
                (sourceTransform.position -
                 beatRoot.position);

            // --------------------------------------------------------
            // Capture original relative rotation.
            // --------------------------------------------------------

            Quaternion relativeRotation =
                Quaternion.Inverse(beatRoot.rotation) *
                sourceTransform.rotation;

            // --------------------------------------------------------
            // Capture original world scale.
            //
            // This scale is NEVER changed afterward.
            // --------------------------------------------------------

            Vector3 originalWorldScale =
                sourceTransform.lossyScale;

            // --------------------------------------------------------
            // Create proxy outside the beat hierarchy.
            // --------------------------------------------------------

            GameObject proxyObject =
                new GameObject(
                    sourceTransform.name +
                    "_BeatStaticCollider"
                );

            proxyObject.transform.SetParent(
                null,
                true
            );

            // Same physics layer.
            proxyObject.layer =
                sourceCollider.gameObject.layer;

            // Same tag.
            try
            {
                proxyObject.tag =
                    sourceCollider.gameObject.tag;
            }
            catch
            {
                // Ignore invalid tag.
            }

            // --------------------------------------------------------
            // Initial transform.
            // --------------------------------------------------------

            proxyObject.transform.position =
                sourceTransform.position;

            proxyObject.transform.rotation =
                sourceTransform.rotation;

            proxyObject.transform.localScale =
                originalWorldScale;

            // --------------------------------------------------------
            // Create replacement MeshCollider.
            // --------------------------------------------------------

            MeshCollider proxyCollider =
                proxyObject.AddComponent<MeshCollider>();

            proxyCollider.sharedMesh =
                sourceCollider.sharedMesh;

            proxyCollider.convex =
                sourceCollider.convex;

            proxyCollider.isTrigger =
                sourceCollider.isTrigger;

            proxyCollider.sharedMaterial =
                sourceCollider.sharedMaterial;

            proxyCollider.contactOffset =
                sourceCollider.contactOffset;

#if UNITY_2022_3_OR_NEWER
            proxyCollider.cookingOptions =
                sourceCollider.cookingOptions;
#endif

            // --------------------------------------------------------
            // Disable the original collider.
            //
            // The renderer and the path animation remain untouched.
            // --------------------------------------------------------

            sourceCollider.enabled = false;

            // --------------------------------------------------------
            // Store proxy information.
            // --------------------------------------------------------

            ColliderProxy proxy =
                new ColliderProxy
                {
                    beatRoot = beatRoot,
                    proxyObject = proxyObject,
                    worldOffset = worldOffset,
                    relativeRotation = relativeRotation
                };

            colliderProxies.Add(proxy);
        }

        // ============================================================
        // UPDATE PROXIES
        // ============================================================

        private void UpdateColliderProxies()
        {
            for (int i = 0;
                 i < colliderProxies.Count;
                 i++)
            {
                ColliderProxy proxy =
                    colliderProxies[i];

                if (proxy == null)
                    continue;

                if (proxy.beatRoot == null)
                    continue;

                if (proxy.proxyObject == null)
                    continue;

                Transform beatTransform =
                    proxy.beatRoot.transform;

                Transform proxyTransform =
                    proxy.proxyObject.transform;

                // ----------------------------------------------------
                // Follow beat object's position.
                //
                // The collider's original offset is preserved.
                // ----------------------------------------------------

                proxyTransform.position =
                    beatTransform.position +
                    beatTransform.rotation *
                    proxy.worldOffset;

                // ----------------------------------------------------
                // Follow beat object's rotation.
                // ----------------------------------------------------

                proxyTransform.rotation =
                    beatTransform.rotation *
                    proxy.relativeRotation;

                // ----------------------------------------------------
                // IMPORTANT:
                //
                // Never modify proxyTransform.localScale.
                //
                // The proxy therefore never receives the beat
                // object's pulsing scale.
                // ----------------------------------------------------
            }
        }
    }
}