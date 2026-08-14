/////////////////////////////////////////////////////////////////////////////////
//
//	PlacementPreview.cs
//
//	Description:	renders a preview of the object before spawning.
//					
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using static VSController.Grabbing;
using UnityEngine;

namespace VSController
{
    public class PlacementPreview : MonoBehaviour
    {
        private Grabbing grabbing;

        private GameObject previewGO, lastPrefab;
        private readonly List<Renderer> outlineRenderers = new();

        private Material matGreen, matRed;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
        private static readonly int RimBoostId = Shader.PropertyToID("_RimBoost");

        private readonly Collider[] overlapBuf = new Collider[32];

        private void Awake()
        {
            grabbing = GetComponent<Grabbing>();
            if (!grabbing) enabled = false;
        }

        // Called from Grabbing.cs and start the preview process
        public void Tick(Grabbing.CollectedStack sel, RaycastHit hitProbe)
        {
            // Get info about what we place
            if (sel?.objectType == null || sel.prefab == null || !TryCalcPose(sel, hitProbe, out var pos, out var rot, out bool canPlace))
            {
                Kill();
                return;
            }

            SpawnPreview(sel.prefab);
            SetOutline(canPlace);
            previewGO.transform.SetPositionAndRotation(pos, rot);
        }

        // Disable preview
        public void Kill()
        {
            if (previewGO) Destroy(previewGO);
            previewGO = null;
            lastPrefab = null;
            outlineRenderers.Clear();
        }

        // Edit pose of the object preview
        private bool TryCalcPose(CollectedStack s, RaycastHit hitProbe, out Vector3 pos, out Quaternion rot, out bool canPlace)
        {
            pos = default; rot = default; canPlace = true;

            var type = s.objectType;
            var prefab = s.prefab;
            if (type == null || prefab == null) return false;

            // Check by raycast deactivateRange
            var camT = grabbing.playerCamera.transform;
            if (!Physics.Raycast(camT.position, camT.forward, out var hit, type.deactivateRange)) return false;
            if (hit.collider != hitProbe.collider) return false;

            // Collider of obj which we spawn 
            var size = grabbing.GetColliderSizeFunc(prefab);
            if (size == Vector3.zero) return false;
            var n = hit.normal.normalized;

            if (type.spawnBottomToObject)
            {
                // Attach to surface
                Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, n).normalized;

                // If bad forward
                if (forward.sqrMagnitude < 0.001f)
                    forward = Vector3.ProjectOnPlane(Vector3.right, n).normalized;

                // Rotation of different sides
                Quaternion spin = Quaternion.AngleAxis(grabbing.RotationVariant * 90f, n);
                rot = Quaternion.LookRotation(spin * forward, n);
            }
            else
            {
                rot = Quaternion.AngleAxis(grabbing.RotationVariant * 90f, Vector3.up);
            }

            // Shift obj outside
            Vector3 half = size * 0.5f;

            // Rotate half size to the objects orientation
            Vector3 right = rot * Vector3.right * half.x;
            Vector3 up = rot * Vector3.up * half.y;
            Vector3 fwd = rot * Vector3.forward * half.z;

            float off = Mathf.Abs(Vector3.Dot(right, n)) + Mathf.Abs(Vector3.Dot(up, n)) + Mathf.Abs(Vector3.Dot(fwd, n));

            // If free mode show the ray point + shift
            if (!type.useFaceGrid)
            {
                pos = hit.point + n * off;
            }
            else
            {
                var col = hit.collider;
                var b = col.bounds;

                float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);

                // Find face what we need
                Vector3Int faceId = ax >= ay && ax >= az ? new Vector3Int(n.x >= 0 ? 1 : -1, 0, 0) :
                    ay >= ax && ay >= az ? new Vector3Int(0, n.y >= 0 ? 1 : -1, 0) :
                                           new Vector3Int(0, 0, n.z >= 0 ? 1 : -1);

                Vector3 a, d;
                if (faceId.x != 0) { a = Vector3.forward; d = Vector3.up; }
                else if (faceId.y != 0) { a = Vector3.right; d = Vector3.forward; }
                else { a = Vector3.right; d = Vector3.up; }

                // "Face" size
                float faceA = Vector3.Dot(b.size, a), faceB = Vector3.Dot(b.size, d);

                // Obj size
                float objA = Mathf.Abs(Vector3.Dot(size, a)), objB = Mathf.Abs(Vector3.Dot(size, d));
                if (objA < 1e-4f || objB < 1e-4f) return false;

                // Get number if cells 
                int cellsA = Mathf.FloorToInt(faceA / objA), cellsB = Mathf.FloorToInt(faceB / objB);
                if (cellsA <= 0 || cellsB <= 0) return false;

                Vector3 faceCenter = b.center + new Vector3(faceId.x * b.extents.x, faceId.y * b.extents.y, faceId.z * b.extents.z);
                Vector3 origin = faceCenter - a * (faceA * 0.5f) - d * (faceB * 0.5f);
                Vector3 local = (hit.point - origin) + n * 0.001f;

                // Cell selection
                int cA = Mathf.Clamp(Mathf.RoundToInt(Vector3.Dot(local, a) / objA - 0.5f), 0, cellsA - 1);
                int cB = Mathf.Clamp(Mathf.RoundToInt(Vector3.Dot(local, d) / objB - 0.5f), 0, cellsB - 1);

                // Check cell (is free?)
                var key = (surface: col, faceId);
                if (grabbing.occupiedFaceSlots.TryGetValue(key, out var set) && set.Contains(new Vector2Int(cA, cB)))
                    canPlace = false;

                // Callculate final position
                pos = (origin + a * (cA * objA + objA * 0.5f) + d * (cB * objB + objB * 0.5f)) + n * off;
            }

            // Check overlap for free mode
            if (!type.useFaceGrid)
            {
                Vector3 halfExtents = size * 0.5f;
                int cnt = Physics.OverlapBoxNonAlloc(pos, halfExtents, overlapBuf, rot, ~0, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < cnt; i++)
                {
                    var c = overlapBuf[i];

                    if (!c) continue;
                    if (c == hit.collider) continue;
                    if (c.isTrigger) continue;
                    if (previewGO && c.transform.IsChildOf(previewGO.transform)) continue;

                    canPlace = false;
                    break;
                }
            }

            return true;
        }

        private void SpawnPreview(GameObject prefab)
        {
            if (previewGO && lastPrefab != prefab) Kill();
            if (previewGO) return;

            // Instantiate preview obj
            lastPrefab = prefab;
            previewGO = Instantiate(prefab);
            previewGO.name = "PREVIEW " + prefab.name;

            // Disable all physics, scripts components 
            foreach (var c in previewGO.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var rb in previewGO.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
            foreach (var mb in previewGO.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;

            if (matGreen == null || matRed == null)
            {
                // Add shader
                var sh = Shader.Find("PlacmentPreview") ?? Shader.Find("Unlit/Color");
                matGreen = new Material(sh) { renderQueue = 3100 };
                matRed = new Material(sh) { renderQueue = 3100 };

                // Change color depend of situation
                matGreen.SetColor(ColorId, new Color(0f, 1f, 0f, 0.35f));
                matRed.SetColor(ColorId, new Color(1f, 0f, 0f, 0.35f));

                if (matGreen.HasProperty(GlowColorId))
                {
                    matGreen.SetColor(GlowColorId, Color.green);
                    matRed.SetColor(GlowColorId, Color.red);
                }

                if (matGreen.HasProperty(GlowStrengthId))
                {
                    matGreen.SetFloat(GlowStrengthId, 2.5f);
                    matRed.SetFloat(GlowStrengthId, 4f);
                }

                if (matGreen.HasProperty(RimBoostId))
                {
                    matGreen.SetFloat(RimBoostId, 1.6f);
                    matRed.SetFloat(RimBoostId, 2.4f);
                }
            }

            outlineRenderers.Clear();

            // Disable rendering of the original prefab
            foreach (var r in previewGO.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            // Create outline by mesh
            var meshFilters = previewGO.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                var mf = meshFilters[i];
                if (!mf || mf.sharedMesh == null) continue;

                var srcRenderer = mf.GetComponent<MeshRenderer>();
                if (!srcRenderer) continue;

                // Spawn new object with outline
                var go = new GameObject("OUTLINE");
                go.transform.SetParent(mf.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * 1.01f;

                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;

                var mr = go.AddComponent<MeshRenderer>();

                var mats = new Material[mf.sharedMesh.subMeshCount];
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = matGreen;

                mr.sharedMaterials = mats;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                outlineRenderers.Add(mr);
            }

            // Outline with skinned mesh
            var skinnedMeshes = previewGO.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedMeshes.Length; i++)
            {
                var smr = skinnedMeshes[i];
                if (!smr || smr.sharedMesh == null) continue;

                var go = new GameObject("OUTLINE");
                go.transform.SetParent(smr.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * 1.01f;

                go.AddComponent<MeshFilter>().sharedMesh = smr.sharedMesh;

                var mr = go.AddComponent<MeshRenderer>();

                var mats = new Material[smr.sharedMesh.subMeshCount];
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = matGreen;

                mr.sharedMaterials = mats;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                outlineRenderers.Add(mr);
            }
        }

        // Change color of outline
        private void SetOutline(bool canPlace)
        {
            var mat = canPlace ? matGreen : matRed;

            for (int i = 0; i < outlineRenderers.Count; i++)
            {
                var r = outlineRenderers[i];
                if (!r) continue;

                var mats = r.sharedMaterials;
                for (int j = 0; j < mats.Length; j++)
                    mats[j] = mat;

                r.sharedMaterials = mats;
            }
        }
    }
}