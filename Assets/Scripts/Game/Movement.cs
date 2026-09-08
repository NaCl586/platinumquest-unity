using System;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public static class GravitySystem
{
    public static Vector3 GravityDir = Vector3.down;
    public static float GravityStrength;
    public static Vector3 Gravity => GravityDir * GravityStrength;
}

[System.Serializable]
public class CollisionInfo
{
    public Vector3 point;
    public Vector3 normal;
    public Vector3 velocity;
    public Collider collider;
    public float friction;
    public float restitution;
    public float bounce;
    public float contactDistance;
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Movement : MonoBehaviour
{
    //singleton
    public static Movement instance;

    public void Awake()
    {
        instance = this;
    }

    //constraint
    public bool canSpin = true;
    public bool canMove = true;
    public bool canJump = true;
    public bool freezeMovement = false;
    public bool freezeInput = false;

    [Space]
    public float maxRollVelocity = 15f;
    public float angularAcceleration = 75f;
    public float brakingAcceleration = 30f;
    public float airAcceleration = 5f;
    public float gravity = 20f;
    public float staticFriction = 1.1f;
    public float kineticFriction = 0.7f;
    public float bounceKineticFriction = 0.2f;
    public float maxDotSlide = 0.5f;
    public float jumpImpulse = 7.5f;
    [Tooltip("Minimum time between successful jumps, in seconds.")]
    public float jumpCooldown = 0.1f;
    public float maxForceRadius = 50f;
    public float minBounceVel = 0.1f;
    public float bounceRestitution = 0.5f;
    public float bounce = 0;

    [Space]
    public Vector3 marbleVelocity;
    public Vector3 marbleAngularVelocity;

    public float marbleRadius;

    private int movementTriggerCount;
    private bool movementWasAllowedBeforeTrigger;

    public void EnterMovementTrigger()
    {
        if (movementTriggerCount == 0)
            movementWasAllowedBeforeTrigger = canMove;

        movementTriggerCount++;
    }

    public void ExitMovementTrigger()
    {
        movementTriggerCount--;

        if (movementTriggerCount < 0)
            movementTriggerCount = 0;
    }

    public void ResetMovementTriggerCount()
    {
        movementTriggerCount = 0;
    }

    private Vector2 inputMovement()
    {
        if (GameUIManager.instance != null && GameUIManager.instance.IsChatInputOpen)
        {
            return Vector2.zero;
        }

        if (movementTriggerCount > 0 || freezeInput)
        {
            return Vector2.zero;
        }

        // canMove controls translation, while canSpin controls
        // whether player input can generate rotational input.
        // Keep the input available for spinning during Ready/Set.
        Vector2 movement = fakeInput;

        if (canSpin && !ReplayRecorder.loadReplay)
        {
            if (Input.GetKey(ControlBinding.instance.moveForward))
                movement.y = 1f;

            if (Input.GetKey(ControlBinding.instance.moveBackward))
                movement.y = -1f;

            if (Input.GetKey(ControlBinding.instance.moveRight))
                movement.x = 1f;

            if (Input.GetKey(ControlBinding.instance.moveLeft))
                movement.x = -1f;
        }

        return movement;
    }

    private Vector2 fakeInput = Vector2.zero;

    private bool Jump()
    {
        if (GameUIManager.instance != null && GameUIManager.instance.IsChatInputOpen)
        {
            return false;
        }

        if (!ReplayRecorder.loadReplay)
            return Input.GetKey(ControlBinding.instance.jump);
        else
            return false;
    }

    private Vector3 forwards = Vector3.forward;

    private bool bounceYet;
    private float bounceSpeed;
    private Vector3 bouncePos;
    private Vector3 bounceNormal;

    [HideInInspector]
    public float slipAmount;

    private float contactTime;
    private float rollVolume;

    [HideInInspector]
    public float contactPct;

    private Vector3 surfaceVelocity;

    private List<MeshCollider> colTests;
    private List<CollisionInfo> contacts = new List<CollisionInfo>();

    // Colliders whose special-material contact has already been processed.
    // HashSet prevents repeated calls while the marble remains in contact.
    private readonly HashSet<Collider> specialMaterialContactColliders =
        new HashSet<Collider>();

    class MeshData
    {
        public MeshCollider collider;
        public Mesh mesh;

        public Vector3[] localVertices;
        public int[] triangles;

        // The spatial grid is stored in MESH LOCAL SPACE.
        // This is critical for rotating/moving platforms: the grid does not
        // need to be rebuilt when the platform moves or rotates.
        public Dictionary<Vector3Int, List<int>> triangleGrid;
        public List<int> largeTriangles;
        public int[] triangleQueryStamp;
        public int triangleQueryVersion;

        public Matrix4x4 localToWorld;
        public Matrix4x4 worldToLocal;

        public Vector3 lastPosition;
        public Quaternion lastRotation;
        public Vector3 lastScale;

        // Cached components. These are queried once when mesh data is built
        // instead of repeatedly during high-frequency physics.
        public PathMover pathMover;
        public Rigidbody attachedRigidbody;
        public FrictionComponent frictionComponent;

    }

    private List<MeshData> meshes;

    private Rigidbody rigidBody;
    private SphereCollider sphereCollider;
    private Camera mainCamera;
    private Vector3 lastNormal = Vector3.zero;

    Vector3 position;
    Vector3 oldPos;
    Vector3 newPos;
    Quaternion prevRot;

    private bool wasCanMove = true;
    private float baseStaticFriction;
    private float baseKineticFriction;
    private CollisionInfo bestContact;

    private bool hasPosition = false;

    public int justJumped = 0;

    // Prevents repeated jumps, including repeated jumps within the same
    // Unity FixedUpdate when the custom physics loop performs substeps.
    private float jumpCooldownRemaining = 0f;


    // Triangle broadphase settings. The grid is deliberately independent of
    // Unity's PhysX broadphase because Movement uses its own triangle-level
    // collision system. A 4-unit cell keeps the number of indexed cells low
    // while still reducing the number of triangle tests dramatically in
    // large interior meshes.
    private const float TRIANGLE_GRID_CELL_SIZE = 4f;
    private const int TRIANGLE_GRID_MIN_TRIANGLES = 64;
    private const int TRIANGLE_GRID_MAX_CELLS_PER_TRIANGLE = 64;

    public void SetPosition(Vector3 newPos, bool silent = false)
    {
        hasPosition = true;

        if (!silent)
        {
            marbleVelocity = Vector3.zero;
            marbleAngularVelocity = Vector3.zero;
        }

        position = newPos;
        oldPos = newPos;

        transform.position = newPos;
    }

    public void SetAlignedPosition(Vector3 newPos)
    {
        hasPosition = true;

        position = newPos;
        oldPos = newPos;

        transform.position = newPos;
    }

    public void StopAllMovement()
    {
        marbleVelocity = Vector3.zero;
        marbleAngularVelocity = Vector3.zero;
    }

    public void FinishState()
    {
        canMove = false;
        canSpin = true;
        canJump = false;
        freezeMovement = true;
        freezeInput = true;
    }

    public void StopMoving()
    {
        canMove = false;
        canSpin = false;
        canJump = false;
        freezeInput = true;
    }

    public void StopAllbutJumping()
    {
        canMove = false;
        canSpin = true;
        canJump = true;
        freezeInput = false;
    }

    public void StartMoving()
    {
        canMove = true;
        canSpin = true;
        canJump = true;
        freezeInput = false;
    }

    public void ApplyMissionPhysics()
    {
        maxRollVelocity = MissionInfo.instance.maxRollVelocity;
        angularAcceleration = MissionInfo.instance.angularAcceleration;
        brakingAcceleration = MissionInfo.instance.brakingAcceleration;
        gravity = MissionInfo.instance.gravity;
        jumpImpulse = MissionInfo.instance.jumpImpulse;

        GravitySystem.GravityStrength = gravity;

        if (Marble.instance != null)
            Marble.instance.CapturePhysicsBaseline();
    }

    void Start()
    {
        ApplyMissionPhysics();

        if (Marble.instance != null)
            Marble.instance.CapturePhysicsBaseline();

        baseStaticFriction = staticFriction;
        baseKineticFriction = kineticFriction;

        rigidBody = gameObject.GetComponent<Rigidbody>();
        rigidBody.maxAngularVelocity = Mathf.Infinity;

        sphereCollider = GetComponent<SphereCollider>();
        mainCamera = Camera.main;

        marbleRadius =
            sphereCollider.radius
            * Mathf.Max(
                transform.lossyScale.x,
                transform.lossyScale.y,
                transform.lossyScale.z
            );

        GravitySystem.GravityStrength = gravity;
    }

    public void GenerateMeshData()
    {
        colTests = new List<MeshCollider>();
        meshes = new List<MeshData>();

        foreach (var item in FindObjectsOfType<MeshCollider>())
        {
            if (item == null)
                continue;

            if (!item.isTrigger && item.sharedMesh != null)
                colTests.Add(item);
        }

        foreach (var mesh in colTests)
            GenerateMeshInfo(mesh);
    }

    void GenerateMeshInfo(MeshCollider mc)
    {
        if (mc == null || mc.sharedMesh == null)
            return;

        Mesh m = mc.sharedMesh;

        if (m == null)
            return;

        MeshData data = new MeshData
        {
            collider = mc,
            mesh = m,
            localVertices = m.vertices,
            triangles = m.triangles,
            localToWorld = mc.transform.localToWorldMatrix,
            worldToLocal = mc.transform.worldToLocalMatrix,
            lastPosition = mc.transform.position,
            lastRotation = mc.transform.rotation,
            lastScale = mc.transform.lossyScale,
            pathMover = mc.GetComponentInParent<PathMover>(),
            attachedRigidbody = mc.attachedRigidbody,
            frictionComponent = mc.GetComponent<FrictionComponent>(),
        };

        // Build world-space geometry once. Static level meshes therefore no
        // longer pay Matrix4x4.MultiplyPoint3x4 and triangle-normal costs on
        // every physics substep. Moving meshes are rebuilt only when their
        // transform actually changes.
        RebuildWorldMeshData(data);
        meshes.Add(data);
    }

    private void UpdateFinishRotation()
    {
        Vector3 angularVelocity = marbleAngularVelocity;
        float angularSpeed = angularVelocity.magnitude;

        if (angularSpeed <= 0.0000001f)
            return;

        float physicsTimeScale = 1f;

        if (Marble.instance != null)
            physicsTimeScale =
                Mathf.Max(0f, Marble.instance.PhysicsTimeScale);

        if (physicsTimeScale <= 0f)
            return;

        Quaternion rotation = Quaternion.AngleAxis(
            Time.fixedDeltaTime
                * physicsTimeScale
                * angularSpeed
                * Mathf.Rad2Deg,
            angularVelocity / angularSpeed
        );

        rotation.Normalize();

        transform.rotation = rotation * transform.rotation;
        transform.rotation.Normalize();
    }

    void FixedUpdate()
    {
        justJumped = 0;

        if (GameManager.gameFinish)
        {
            UpdateFinishRotation();
            return;
        }

        // Cooldown is measured in Unity fixed time, not custom physics
        // substeps. This keeps the cooldown at a predictable 0.1 seconds.
        if (jumpCooldownRemaining > 0f)
            jumpCooldownRemaining = Mathf.Max(0f, jumpCooldownRemaining - Time.fixedDeltaTime);

        if (!hasPosition)
            return;

        // Detect canMove turning OFF
        if ((wasCanMove && !canMove) || !GameManager.gameStart)
        {
            // Disable friction
            staticFriction = 0f;
            kineticFriction = 0f;
        }

        // Detect canMove turning ON
        if (!wasCanMove && canMove)
        {
            // Restore friction
            staticFriction = baseStaticFriction;
            kineticFriction = baseKineticFriction;
        }

        wasCanMove = canMove;

        // Apply the PhysMod timeScale only to this marble's custom physics.
        // Do NOT use UnityEngine.Time.timeScale here, because that would
        // affect the entire game.
        float physicsTimeScale = 1f;

        if (Marble.instance != null)
            physicsTimeScale =
                Mathf.Max(0f, Marble.instance.PhysicsTimeScale);

        float timeRemaining =
            Time.fixedDeltaTime * physicsTimeScale;

        // Keep the normal custom physics step, but subdivide further when
        // the marble is moving fast enough to travel a large distance in
        // one step. This prevents high-speed tunneling through geometry.
        const float STEP_SIZE = 0.008f;
        const float MAX_STEP_DISTANCE_MULTIPLIER = 1.0f;
        const int MAX_SUBSTEPS = 6;

        oldPos = position;
        prevRot = transform.rotation;

        var it = 0;

        while (timeRemaining > 0f)
        {
            float timeStep = Mathf.Min(timeRemaining, STEP_SIZE);

            float speed = marbleVelocity.magnitude;
            float maxStepDistance =
                marbleRadius * MAX_STEP_DISTANCE_MULTIPLIER;

            if (speed > 0.0001f && maxStepDistance > 0f)
            {
                float maxTimeForDistance =
                    maxStepDistance / speed;

                timeStep = Mathf.Min(
                    timeStep,
                    maxTimeForDistance
                );
            }

            if (timeStep <= 0.000001f)
                break;

            AdvancePhysics(ref timeStep);

            timeRemaining -= timeStep;

            it++;

            // Safety limit for pathological velocities.
            if (it >= MAX_SUBSTEPS)
                break;
        }

        if (!freezeMovement)
            transform.position = position;

        // Update audio once per Unity FixedUpdate rather than once per
        // internal physics substep. This matters at 200 Hz.
        UpdateRollSound(contactPct, slipAmount);

        Vector3 vector3 = marbleAngularVelocity;
        float num1 = vector3.magnitude;

        if (num1 <= 0.0000001f)
            return;

        Quaternion quaternion = Quaternion.AngleAxis(
            Time.fixedDeltaTime * num1 * Mathf.Rad2Deg,
            vector3 * (1f / num1)
        );

        quaternion.Normalize();

        transform.rotation = quaternion * transform.rotation;
        transform.rotation.Normalize();
    }

    private void AdvancePhysics(ref float _dt)
    {
        Bounds searchBox = new Bounds(
            position,
            Vector3.one * (marbleRadius * 2f)
        );
        searchBox.Expand(0.1f);

        contacts = FindContacts(searchBox);

        // Preserve the special-material contact hook used by the mission
        // modes. This is intentionally called once per physics substep,
        // matching the custom physics flow.
        ProcessSpecialMaterialContacts();

        UpdateMove(ref _dt);
    }

    private void ProcessSpecialMaterialContacts()
    {
        if (contacts == null || contacts.Count == 0)
            return;

        if (GameManager.instance == null)
            return;

        PlatinumQuestScripts.ISpecialGameMode specialMode =
            GameManager.instance.specialGameMode;

        if (specialMode == null)
            return;

        Marble marble = Marble.instance;

        if (marble == null)
            return;

        // Only process the same collider once while the marble remains in
        // contact with it. Remove it below when contact is lost.
        for (int i = 0; i < contacts.Count; i++)
        {
            CollisionInfo contact = contacts[i];

            if (contact == null || contact.collider == null)
                continue;

            if (specialMaterialContactColliders.Contains(contact.collider))
                continue;

            specialMaterialContactColliders.Add(contact.collider);

            specialMode.ProcessMaterialContact(
                marble,
                contact
            );
        }

        specialMaterialContactColliders.RemoveWhere(
            collider =>
            {
                if (collider == null)
                    return true;

                for (int i = 0; i < contacts.Count; i++)
                {
                    if (contacts[i] != null &&
                        contacts[i].collider == collider)
                    {
                        return false;
                    }
                }

                return true;
            }
        );
    }

    List<CollisionInfo> FindContacts(Bounds bounds)
    {
        contacts.Clear();

        float _radius = marbleRadius + 0.0001f;

        if (meshes == null)
            return contacts;

        /*
         * Iterate backwards so stale MeshData entries can safely
         * be removed if their MeshCollider was destroyed.
         */
        for (int _index = meshes.Count - 1; _index >= 0; _index--)
        {
            MeshData _mesh = meshes[_index];

            // Unity destroyed-object protection.
            //
            // A destroyed UnityEngine.Object can still have a non-null
            // C# reference, but Unity treats it as null.
            if (_mesh == null || _mesh.collider == null || _mesh.mesh == null)
            {
                meshes.RemoveAt(_index);
                continue;
            }

            MeshCollider _meshCollider = _mesh.collider;

            if (!_meshCollider.enabled ||
                !_meshCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!_meshCollider.bounds.Intersects(bounds))
                continue;

            // Keep the mesh transform current. The spatial grid is stored in
            // local space, so even a rotating PathMover does NOT rebuild the
            // grid or transform every vertex here.
            UpdateMeshTransform(_mesh);

            TestMeshContacts(_mesh, bounds, _radius);
        }

        return contacts;
    }

    void UpdateMeshTransform(MeshData data)
    {
        if (data == null || data.collider == null)
            return;

        Transform t = data.collider.transform;

        if (data.pathMover != null)
        {
            // PathMover can update a platform through its movement system.
            // Keep the matrices current, but DO NOT rebuild the triangle grid.
            // The grid is stored in local space and remains valid after
            // translation, rotation, or scale changes.
            data.localToWorld = t.localToWorldMatrix;
            data.worldToLocal = t.worldToLocalMatrix;
            data.lastPosition = t.position;
            data.lastRotation = t.rotation;
            data.lastScale = t.lossyScale;
            return;
        }

        if (
            t.position != data.lastPosition
            || t.rotation != data.lastRotation
            || t.lossyScale != data.lastScale
        )
        {
            data.localToWorld = t.localToWorldMatrix;
            data.worldToLocal = t.worldToLocalMatrix;

            data.lastPosition = t.position;
            data.lastRotation = t.rotation;
            data.lastScale = t.lossyScale;
        }
    }

    void RebuildWorldMeshData(MeshData data)
    {
        if (data == null || data.localVertices == null || data.triangles == null)
            return;

        // This method is now only the initialization entry point. The grid is
        // built from local-space geometry and is never rebuilt merely because
        // a PathMover rotates or translates the collider.
        BuildTriangleSpatialGrid(data);
    }

    private Vector3Int LocalToTriangleGridCell(Vector3 localPosition)
    {
        float cellSize = TRIANGLE_GRID_CELL_SIZE;

        return new Vector3Int(
            Mathf.FloorToInt(localPosition.x / cellSize),
            Mathf.FloorToInt(localPosition.y / cellSize),
            Mathf.FloorToInt(localPosition.z / cellSize)
        );
    }

    private void GetLocalBounds(
        MeshData data,
        Bounds worldBounds,
        out Vector3 localMin,
        out Vector3 localMax
    )
    {
        // Transform all eight corners of the marble's world-space AABB into
        // mesh-local space. The resulting local AABB is conservative, so no
        // potentially colliding triangle is lost when the mesh rotates.
        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;

        Vector3 p0 = data.worldToLocal.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z));
        Vector3 p1 = data.worldToLocal.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z));
        Vector3 p2 = data.worldToLocal.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z));
        Vector3 p3 = data.worldToLocal.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z));
        Vector3 p4 = data.worldToLocal.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z));
        Vector3 p5 = data.worldToLocal.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z));
        Vector3 p6 = data.worldToLocal.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z));
        Vector3 p7 = data.worldToLocal.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z));

        localMin = Vector3.Min(p0, p1);
        localMin = Vector3.Min(localMin, p2);
        localMin = Vector3.Min(localMin, p3);
        localMin = Vector3.Min(localMin, p4);
        localMin = Vector3.Min(localMin, p5);
        localMin = Vector3.Min(localMin, p6);
        localMin = Vector3.Min(localMin, p7);

        localMax = Vector3.Max(p0, p1);
        localMax = Vector3.Max(localMax, p2);
        localMax = Vector3.Max(localMax, p3);
        localMax = Vector3.Max(localMax, p4);
        localMax = Vector3.Max(localMax, p5);
        localMax = Vector3.Max(localMax, p6);
        localMax = Vector3.Max(localMax, p7);
    }

    private void BuildTriangleSpatialGrid(MeshData data)
    {
        if (data == null ||
            data.triangles == null ||
            data.localVertices == null)
        {
            return;
        }

        int triangleCount =
            data.triangles.Length / 3;

        if (triangleCount < TRIANGLE_GRID_MIN_TRIANGLES)
        {
            data.triangleGrid = null;
            data.largeTriangles = null;
            data.triangleQueryStamp = null;
            data.triangleQueryVersion = 0;
            return;
        }

        if (data.triangleGrid == null)
        {
            data.triangleGrid =
                new Dictionary<Vector3Int, List<int>>();
        }
        else
        {
            data.triangleGrid.Clear();
        }

        if (data.largeTriangles == null)
            data.largeTriangles = new List<int>();
        else
            data.largeTriangles.Clear();

        if (data.triangleQueryStamp == null ||
            data.triangleQueryStamp.Length != triangleCount)
        {
            data.triangleQueryStamp =
                new int[triangleCount];
        }
        else
        {
            Array.Clear(
                data.triangleQueryStamp,
                0,
                data.triangleQueryStamp.Length
            );
        }

        data.triangleQueryVersion = 0;

        for (int triangleIndex = 0;
             triangleIndex < triangleCount;
             triangleIndex++)
        {
            int offset = triangleIndex * 3;

            Vector3 p0 =
                data.localVertices[data.triangles[offset]];
            Vector3 p1 =
                data.localVertices[data.triangles[offset + 1]];
            Vector3 p2 =
                data.localVertices[data.triangles[offset + 2]];

            Vector3 min =
                Vector3.Min(
                    p0,
                    Vector3.Min(p1, p2)
                );

            Vector3 max =
                Vector3.Max(
                    p0,
                    Vector3.Max(p1, p2)
                );

            Vector3Int minCell =
                LocalToTriangleGridCell(min);
            Vector3Int maxCell =
                LocalToTriangleGridCell(max);

            long spanX =
                (long)maxCell.x - minCell.x + 1L;
            long spanY =
                (long)maxCell.y - minCell.y + 1L;
            long spanZ =
                (long)maxCell.z - minCell.z + 1L;

            long cellCount =
                spanX * spanY * spanZ;

            if (cellCount <= 0 ||
                cellCount > TRIANGLE_GRID_MAX_CELLS_PER_TRIANGLE)
            {
                data.largeTriangles.Add(triangleIndex);
                continue;
            }

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cell =
                            new Vector3Int(x, y, z);

                        if (!data.triangleGrid.TryGetValue(
                                cell,
                                out List<int> triangleList
                            ))
                        {
                            triangleList =
                                new List<int>(4);

                            data.triangleGrid.Add(
                                cell,
                                triangleList
                            );
                        }

                        triangleList.Add(triangleIndex);
                    }
                }
            }
        }
    }

    private void TestMeshContacts(
        MeshData meshData,
        Bounds bounds,
        float radius
    )
    {
        if (meshData == null ||
            meshData.localVertices == null ||
            meshData.triangles == null)
        {
            return;
        }

        int triangleCount =
            meshData.triangles.Length / 3;

        // Small meshes are cheaper to test directly.
        if (triangleCount < TRIANGLE_GRID_MIN_TRIANGLES ||
            meshData.triangleGrid == null)
        {
            TestTriangleRange(
                meshData,
                0,
                triangleCount,
                radius
            );

            return;
        }

        if (meshData.triangleQueryStamp == null ||
            meshData.triangleQueryStamp.Length != triangleCount)
        {
            // This should normally only happen during initialization.
            meshData.triangleQueryStamp =
                new int[triangleCount];
            meshData.triangleQueryVersion = 0;
        }

        meshData.triangleQueryVersion++;

        if (meshData.triangleQueryVersion == int.MaxValue)
        {
            Array.Clear(
                meshData.triangleQueryStamp,
                0,
                meshData.triangleQueryStamp.Length
            );

            meshData.triangleQueryVersion = 1;
        }

        int queryStamp =
            meshData.triangleQueryVersion;

        Vector3 localMin;
        Vector3 localMax;

        GetLocalBounds(
            meshData,
            bounds,
            out localMin,
            out localMax
        );

        Vector3Int minCell =
            LocalToTriangleGridCell(localMin);
        Vector3Int maxCell =
            LocalToTriangleGridCell(localMax);

        if (meshData.largeTriangles != null)
        {
            for (int i = 0; i < meshData.largeTriangles.Count; i++)
            {
                int triangleIndex =
                    meshData.largeTriangles[i];

                if (meshData.triangleQueryStamp[triangleIndex] == queryStamp)
                    continue;

                meshData.triangleQueryStamp[triangleIndex] = queryStamp;

                if (TestTriangle(
                        meshData,
                        triangleIndex,
                        radius
                    ) &&
                    contacts.Count >= 4)
                {
                    return;
                }
            }
        }

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                for (int z = minCell.z; z <= maxCell.z; z++)
                {
                    if (!meshData.triangleGrid.TryGetValue(
                            new Vector3Int(x, y, z),
                            out List<int> triangleIndices
                        ))
                    {
                        continue;
                    }

                    for (int i = 0; i < triangleIndices.Count; i++)
                    {
                        int triangleIndex =
                            triangleIndices[i];

                        if (meshData.triangleQueryStamp[triangleIndex] == queryStamp)
                            continue;

                        meshData.triangleQueryStamp[triangleIndex] = queryStamp;

                        if (TestTriangle(
                                meshData,
                                triangleIndex,
                                radius
                            ) &&
                            contacts.Count >= 4)
                        {
                            return;
                        }
                    }
                }
            }
        }
    }

    private void TestTriangleRange(
        MeshData meshData,
        int startTriangle,
        int triangleCount,
        float radius
    )
    {
        int endTriangle =
            Mathf.Min(
                startTriangle + triangleCount,
                meshData.triangles.Length / 3
            );

        for (int triangleIndex = startTriangle;
             triangleIndex < endTriangle;
             triangleIndex++)
        {
            if (TestTriangle(
                    meshData,
                    triangleIndex,
                    radius
                ) &&
                contacts.Count >= 4)
            {
                return;
            }
        }
    }

    private bool TestTriangle(
        MeshData meshData,
        int triangleIndex,
        float radius
    )
    {
        int triangleOffset =
            triangleIndex * 3;

        Vector3 localP0 =
            meshData.localVertices[
                meshData.triangles[triangleOffset]
            ];

        Vector3 localP1 =
            meshData.localVertices[
                meshData.triangles[triangleOffset + 1]
            ];

        Vector3 localP2 =
            meshData.localVertices[
                meshData.triangles[triangleOffset + 2]
            ];

        // Always transform only the candidate triangle. This is the key
        // difference from the previous implementation: rotating a large
        // platform no longer transforms/recomputes every vertex and every
        // triangle normal just to update the spatial grid.
        Vector3 _p0 =
            meshData.localToWorld.MultiplyPoint3x4(localP0);
        Vector3 _p1 =
            meshData.localToWorld.MultiplyPoint3x4(localP1);
        Vector3 _p2 =
            meshData.localToWorld.MultiplyPoint3x4(localP2);

        Vector3 edge1 = _p1 - _p0;
        Vector3 edge2 = _p2 - _p0;
        Vector3 _normal = Vector3.Cross(edge1, edge2);
        float normalMagnitudeSqr = _normal.sqrMagnitude;

        if (normalMagnitudeSqr <= 1e-12f)
            return false;

        _normal /= Mathf.Sqrt(normalMagnitudeSqr);

        var closest = Vector3.zero;
        var contactNormal = Vector3.zero;

        var res = CollisionHelpers.TriangleSphereIntersection(
            _p0,
            _p1,
            _p2,
            position,
            radius,
            out closest,
            out contactNormal
        );

        if (!res)
            return false;

        var contactDist =
            (closest - position).sqrMagnitude;

        if (contactDist > radius * radius)
            return false;

        if (Vector3.Dot(position - closest, _normal) <= 0)
            return false;

        Vector3 colliderVelocity =
            Vector3.zero;

        PathMover pathMover =
            meshData.pathMover;

        if (pathMover != null &&
            pathMover.pathFollower != null)
        {
            colliderVelocity =
                pathMover.pathFollower.GetPointVelocity(
                    closest
                );
        }
        else if (meshData.attachedRigidbody != null)
        {
            colliderVelocity =
                meshData.attachedRigidbody.GetPointVelocity(
                    closest
                );
        }
        else
        {
            colliderVelocity =
                (meshData.collider.transform.position -
                 meshData.lastPosition) /
                Time.fixedDeltaTime;
        }

        FrictionComponent frictionComponent =
            meshData.frictionComponent;

        CollisionInfo newCollision =
            new CollisionInfo
            {
                point = closest,
                normal = contactNormal.normalized,
                collider = meshData.collider,
                contactDistance = Mathf.Sqrt(contactDist),

                restitution =
                    frictionComponent != null
                        ? frictionComponent.restitution
                        : 1.0f,

                friction =
                    frictionComponent != null
                        ? frictionComponent.friction
                        : 1.0f,

                bounce =
                    frictionComponent != null
                        ? frictionComponent.bounce
                        : 0.0f,

                velocity = colliderVelocity,
            };

        contacts.Add(newCollision);
        lastNormal = newCollision.normal;

        return true;
    }

    void UpdateMove(ref float _dt)
    {
        if (Marble.instance != null && Marble.instance.isFrozen)
        {
            marbleVelocity = Vector3.zero;
            marbleAngularVelocity = Vector3.zero;
            return;
        }

        // Compute player input forces
        bool isMoving = ComputeMoveForces(
            marbleAngularVelocity,
            out var aControl,
            out var desiredOmega
        );

        // No player translation input does not mean the marble is
        // physically stationary. Keep contact physics active while
        // canSpin is enabled during Ready/Set.
        bool physicsMoving =
            isMoving || (!canMove && canSpin);

        // If jump is held and we are touching a valid floor, jump takes
        // priority over bounce. The bounce pass must know this BEFORE it
        // runs, otherwise VelocityCancel() will already have emitted the
        // bounce response/sound/particles.
        bool jumpRequested =
            Jump()
            && canJump
            && jumpCooldownRemaining <= 0f;
        bool jumpSurfaceAvailable = jumpRequested && HasJumpableContact();

        // First pass: cancel velocity. A held jump on a floor suppresses
        // bounce so ApplyContactForces() can perform the jump instead.
        bool stoppedPaths = false;

        VelocityCancel(
            !physicsMoving,
            jumpSurfaceAvailable,
            ref stoppedPaths
        );

        // External forces (gravity, air control)
        Vector3 A = GetExternalForces(_dt);

        // Apply contact forces (friction, jump, bounce)
        ApplyContactForces(
            _dt,
            !physicsMoving,
            aControl,
            desiredOmega,
            ref A,
            out Vector3 a,
            canMove,
            jumpSurfaceAvailable
        );

        // A moving/rotating surface has a velocity at the exact contact
        // point. Enforce the rolling constraint against that velocity
        // before integrating the rest of the frame.
        ApplyMovingSurfaceConstraint(_dt);

        if (canMove)
        {
            marbleVelocity += A * _dt;
        }
        else
        {
            Vector3 gravityDir =
            GravitySystem.GravityDir.sqrMagnitude > 0.0001f
                ? GravitySystem.GravityDir.normalized
                : Vector3.down;

            float gravityVelocity =
                Vector3.Dot(marbleVelocity, gravityDir);

            // Remove all velocity perpendicular to gravity.
            marbleVelocity = gravityDir * gravityVelocity;

            // Apply gravity normally.
            marbleVelocity += gravityDir * gravity * _dt;
        }

        if (canSpin)
            marbleAngularVelocity += a * _dt;
        else
            marbleAngularVelocity = Vector3.zero;

        // Second pass: cancel velocity with bounce disabled
        VelocityCancel(!physicsMoving, true, ref stoppedPaths);

        Vector3 moveVel = marbleVelocity;

        var testDt = _dt;

        TestMove(ref position, moveVel, ref testDt);

        if (testDt != _dt)
        {
            var diff = _dt - testDt;

            marbleVelocity -= A * diff;
            marbleAngularVelocity -= a * diff;

            _dt = testDt;
        }

        var expectedPos = position;

        var newPos = NudgeToContacts(
            marbleVelocity,
            expectedPos
        );


        if (marbleVelocity.sqrMagnitude > 1e-8f)
        {
            var posDiff = newPos - expectedPos;

            if (posDiff.sqrMagnitude > 1e-8)
            {
                var velDiffProj =
                    marbleVelocity
                    * Vector3.Dot(posDiff, marbleVelocity)
                    / marbleVelocity.sqrMagnitude;

                var expectedProjPos = expectedPos + velDiffProj;

                var updatedTimestep =
                    (expectedProjPos - position).magnitude
                    / marbleVelocity.magnitude;

                var tDiff = updatedTimestep - _dt;

                if (tDiff > 0)
                {
                    marbleVelocity -= A * tDiff;
                    marbleAngularVelocity -= a * tDiff;
                    _dt = updatedTimestep;
                }
            }
        }

        position = newPos;

        if (!ReplayRecorder.loadReplay)
            contactPct = contacts.Count > 0 ? 1f : 0f;

    }

    Vector3 NudgeToContacts(Vector3 velocity, Vector3 position)
    {
        var it = 0;
        var prevResolved = 0;

        do
        {
            var resolved = 0;

            foreach (var contact in contacts)
            {
                // Check if we are on wrong side of the triangle
                if (
                    Vector3.Dot(contact.normal, position)
                        - Vector3.Dot(contact.normal, contact.point)
                        < 0
                    || contact.velocity.sqrMagnitude > 0.00001f
                )
                {
                    continue;
                }

                var planeD =
                    -Vector3.Dot(contact.normal, contact.point);

                var t =
                    Vector3.Dot(
                        contact.point - position,
                        contact.normal
                    )
                    / contact.normal.sqrMagnitude;

                var intersect =
                    position + t * contact.normal;

                var planeDistance =
                    (intersect - position).magnitude;

                if (
                    marbleRadius
                    - 0.005f
                    - planeDistance
                    > 0.0001f
                )
                {
                    position += contact.normal *
                        (marbleRadius - 0.005f - planeDistance);

                    resolved += 1;
                }
            }

            if (resolved == 0 && prevResolved == 0)
                break;

            prevResolved = resolved;
            it++;
        }
        while (it < 4);

        return position;
    }

    void TestMove(
        ref Vector3 position,
        Vector3 velocity,
        ref float dt
    )
    {
        float velocitySqrMagnitude = velocity.sqrMagnitude;

        if (velocitySqrMagnitude > 0.000001f)
        {
            float velocityMagnitude = Mathf.Sqrt(velocitySqrMagnitude);

            if (
                Physics.SphereCast(
                    position,
                    marbleRadius,
                    velocity / velocityMagnitude,
                    out var _hit,
                    velocityMagnitude * dt + marbleRadius
                )
            )
            {
                float _travelTime =
                    _hit.distance / velocity.magnitude;

                dt = Mathf.Max(
                    Mathf.Min(dt, _travelTime),
                    0.00001f
                );
            }
        }

        position += velocity * dt;
    }

    private Vector2 GetFilteredMovementInput()
    {
        Vector2 move = inputMovement();

        if (GameManager.instance != null)
        {
            foreach (IGameMode mode in GameManager.instance.GameModes)
            {
                if (mode != null)
                    move = mode.FilterMovementInput(move);
            }
        }

        return move;
    }

    bool ComputeMoveForces(
        Vector3 _angVelocity,
        out Vector3 _torque,
        out Vector3 _targetAngVel
    )
    {
        _torque = Vector3.zero;
        _targetAngVel = Vector3.zero;

        // Relative gravity vector from marble center.
        // Use GravityDir instead of Gravity because Gravity becomes
        // Vector3.zero when gravity strength is 0. The direction remains
        // valid, allowing player-controlled spinning in zero-G.
        Vector3 gravityDir =
            GravitySystem.GravityDir.sqrMagnitude > 0.0001f
                ? GravitySystem.GravityDir.normalized
                : Vector3.down;

        Vector3 _relGravity =
            -gravityDir * marbleRadius;

        // Velocity at the top of the sphere
        Vector3 _topVelocity =
            Vector3.Cross(_angVelocity, _relGravity);

        // Get camera-relative axes
        GetMarbleAxis(
            out var sideDir,
            out var motionDir,
            out Vector3 _
        );

        // Project top velocity onto those axes
        float _topY =
            Vector3.Dot(_topVelocity, motionDir);

        float _topX =
            Vector3.Dot(_topVelocity, sideDir);

        // Input movement
        Vector2 _move = GetFilteredMovementInput();

        float _moveY =
            maxRollVelocity * _move.y;

        float _moveX =
            maxRollVelocity * _move.x;

        // If no input, bail out
        if (
            Math.Abs(_moveY) < 0.001f
            && Math.Abs(_moveX) < 0.001f
        )
            return false;

        // Clamp input so you don’t overshoot
        if (_topY > _moveY && _moveY > 0.0f)
            _moveY = _topY;
        else if (_topY < _moveY && _moveY < 0.0f)
            _moveY = _topY;

        if (_topX > _moveX && _moveX > 0.0f)
            _moveX = _topX;
        else if (_topX < _moveX && _moveX < 0.0f)
            _moveX = _topX;

        // Desired angular velocity based on input
        _targetAngVel =
            Vector3.Cross(
                _relGravity,
                _moveY * motionDir + _moveX * sideDir
            )
            / _relGravity.sqrMagnitude;

        // Torque is difference between desired and current
        _torque =
            _targetAngVel - _angVelocity;

        // Clamp torque to angularAcceleration
        float _targetAngAccel =
            _torque.magnitude;

        if (_targetAngAccel > angularAcceleration)
            _torque *=
                angularAcceleration / _targetAngAccel;

        return true;
    }

    public void GetMarbleAxis(
    out Vector3 sideDir,
    out Vector3 motionDir,
    out Vector3 upDir
)
    {
        // GravityDir is the direction gravity pulls toward.
        Vector3 gravityDir =
            GravitySystem.GravityDir.normalized;

        if (gravityDir.sqrMagnitude < 0.0001f)
            gravityDir = Vector3.down;

        // Marble's actual up direction.
        upDir = -gravityDir;

        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 cameraForward =
            mainCamera != null
                ? mainCamera.transform.forward
                : Vector3.forward;

        Vector3 cameraRight =
            mainCamera != null
                ? mainCamera.transform.right
                : Vector3.right;

        // Find the camera's forward direction on the
        // plane perpendicular to gravity.
        motionDir =
            Vector3.ProjectOnPlane(
                cameraForward,
                gravityDir
            );

        // 2D case: camera may look directly along gravity.
        if (motionDir.sqrMagnitude < 0.0001f)
        {
            motionDir =
                Vector3.ProjectOnPlane(
                    cameraRight,
                    gravityDir
                );
        }

        if (motionDir.sqrMagnitude < 0.0001f)
        {
            motionDir =
                Vector3.ProjectOnPlane(
                    Vector3.forward,
                    gravityDir
                );
        }

        motionDir.Normalize();

        // Right-hand movement axis.
        sideDir =
            Vector3.Cross(
                upDir,
                motionDir
            ).normalized;
    }

    private Vector3 GetExternalForces(float _dt)
    {
        // GravityDir remains valid even when gravity strength is 0.
        Vector3 gravityDir =
            GravitySystem.GravityDir.sqrMagnitude > 0.0001f
                ? GravitySystem.GravityDir.normalized
                : Vector3.down;

        Vector3 _force =
            gravityDir * gravity;

        // Air control is player-controlled, so disable it during Ready/Set.
        // Gravity remains active regardless of canMove.
        if (canMove && contacts.Count == 0)
        {
            GetMarbleAxis(
                out var _sideDir,
                out var _motionDir,
                out Vector3 _
            );

            Vector2 _move = GetFilteredMovementInput();

            _force +=
                (
                    _sideDir * _move.x
                    + _motionDir * _move.y
                )
                * airAcceleration;
        }

        return _force;
    }

    bool VelocityCancel(
        bool _surfaceSlide,
        bool _noBounce,
        ref bool stoppedPaths
    )
    {
        var SurfaceDotThreshold = 0.0001;
        var looped = false;
        var itersIn = 0;
        var done = false;

        do
        {
            done = true;
            itersIn++;

            for (var i = 0; i < contacts.Count; i++)
            {
                var sVel =
                    marbleVelocity - contacts[i].velocity;

                var surfaceDot =
                    Vector3.Dot(
                        contacts[i].normal,
                        sVel
                    );

                if (
                    (!looped && surfaceDot < 0.0)
                    || surfaceDot < -SurfaceDotThreshold
                )
                {
                    var velLen =
                        marbleVelocity.magnitude;

                    var surfaceVel =
                        contacts[i].normal * surfaceDot;

                    if (_noBounce)
                    {
                        marbleVelocity -= surfaceVel;
                    }
                    else
                    {
                        if (
                            contacts[i].velocity.magnitude < 0.0001f
                            && !_surfaceSlide
                            && surfaceDot > -maxDotSlide * velLen
                        )
                        {
                            marbleVelocity -= surfaceVel;
                            marbleVelocity.Normalize();
                            marbleVelocity *= velLen;
                            _surfaceSlide = true;
                        }
                        else if (surfaceDot >= -minBounceVel)
                        {
                            marbleVelocity -= surfaceVel;
                        }
                        else
                        {
                            var restitution =
                                bounceRestitution;

                            if (GameManager.instance.superBounceIsActive)
                                restitution = 0.9f;

                            if (GameManager.instance.shockAbsorberIsActive)
                                restitution = 0.01f;

                            restitution *=
                                contacts[i].restitution;

                            // impact velocity
                            float impactVelocity =
                                -surfaceDot;

                            // MBG volume curve
                            float volume =
                                Mathf.Pow(
                                    impactVelocity / 12f,
                                    1.5f
                                );

                            volume =
                                Mathf.Clamp01(volume);

                            if (impactVelocity > 1f)
                                Marble.instance.PlayBounceSound(
                                    volume
                                );

                            var velocityAdd =
                                surfaceVel
                                * -(1 + restitution);

                            var vAtC =
                                sVel
                                + Vector3.Cross(
                                    marbleAngularVelocity,
                                    contacts[i].normal
                                        * -marbleRadius
                                );

                            var normalVel =
                                -Vector3.Dot(
                                    contacts[i].normal,
                                    sVel
                                );

                            Marble.instance.BounceEmitter(
                                sVel.magnitude
                                    * restitution,
                                contacts[i]
                            );

                            if (
                                ReplayRecorder.Instance != null
                                && ReplayRecorder.Instance.isRecording
                            )
                            {
                                ReplayRecorder.Instance.RecordBounce(
                                    sVel.magnitude
                                        * restitution,
                                    contacts[i].point,
                                    contacts[i].normal
                                );
                            }

                            vAtC -=
                                contacts[i].normal
                                * Vector3.Dot(
                                    contacts[i].normal,
                                    sVel
                                );

                            var vAtCMag =
                                vAtC.magnitude;

                            if (vAtCMag > 0.00001)
                            {
                                var friction =
                                    bounceKineticFriction
                                    * contacts[i].friction;

                                var angVMagnitude =
                                    5
                                    * friction
                                    * normalVel
                                    / (2 * marbleRadius);

                                if (
                                    vAtCMag / marbleRadius
                                    < angVMagnitude
                                )
                                    angVMagnitude =
                                        vAtCMag / marbleRadius;

                                var vAtCDir =
                                    vAtC * (1 / vAtCMag);

                                var deltaOmega =
                                    Vector3.Cross(
                                        contacts[i].normal,
                                        vAtCDir
                                    )
                                    * angVMagnitude;

                                marbleAngularVelocity +=
                                    deltaOmega;

                                marbleVelocity -=
                                    Vector3.Cross(
                                        deltaOmega,
                                        contacts[i].normal
                                            * marbleRadius
                                    );
                            }

                            marbleVelocity +=
                                velocityAdd;

                            if (!ReplayRecorder.loadReplay)
                            {
                                slipAmount =
                                    Mathf.Clamp(
                                        vAtCMag
                                            / maxRollVelocity,
                                        0f,
                                        1.5f
                                    );
                            }
                        }
                    }

                    done = false;
                }
            }

            looped = true;

            if (itersIn > 6 && !stoppedPaths)
            {
                stoppedPaths = true;

                if (_noBounce)
                    done = true;

                foreach (var contact in contacts)
                    contact.velocity = Vector3.zero;
            }
        }
        while (!done && itersIn < 8);

        if (marbleVelocity.magnitude < 625.0)
        {
            var gotOne = false;
            var dir = Vector3.zero;

            for (var j = 0; j < contacts.Count; j++)
            {
                var dir2 =
                    dir + contacts[j].normal;

                if (dir2.sqrMagnitude < 0.01)
                {
                    dir2 =
                        dir2 + contacts[j].normal;
                }

                dir = dir2;
                dir.Normalize();
                gotOne = true;
            }

            if (gotOne)
            {
                dir.Normalize();

                var soFar = 0.0;

                for (var k = 0; k < contacts.Count; k++)
                {
                    var dist =
                        marbleRadius
                        - contacts[k].contactDistance;

                    var timeToSeparate = 0.1;

                    var vel =
                        marbleVelocity
                        - contacts[k].velocity;

                    var outVel =
                        Vector3.Dot(
                            vel + dir * (float)soFar,
                            contacts[k].normal
                        );

                    if (dist > timeToSeparate * outVel)
                    {
                        soFar +=
                            (
                                dist
                                - outVel * timeToSeparate
                            )
                            / timeToSeparate
                            / Vector3.Dot(
                                contacts[k].normal,
                                dir
                            );
                    }
                }

                if (soFar < -25.0)
                    soFar = -25.0;

                if (soFar > 25.0)
                    soFar = 25.0;

                marbleVelocity +=
                    dir * (float)soFar;
            }
        }

        return stoppedPaths;
    }

    private bool HasJumpableContact()
    {
        if (contacts == null || contacts.Count == 0)
            return false;

        Vector3 A = GravitySystem.Gravity;

        int bestSurface = -1;
        float bestNormalForce = 1e-6f;

        for (int i = 0; i < contacts.Count; i++)
        {
            if (contacts[i] == null)
                continue;

            float normalForce =
                -Vector3.Dot(
                    contacts[i].normal,
                    A
                );

            if (normalForce > bestNormalForce)
            {
                bestNormalForce = normalForce;
                bestSurface = i;
            }
        }

        return bestSurface != -1;
    }

    void ApplyContactForces(
        float _dt,
        bool _isCentered,
        Vector3 _aControl,
        Vector3 _desiredOmega,
        ref Vector3 A,
        out Vector3 a,
        bool allowInputMovement,
        bool jumpSurfaceAvailable
    )
    {
        a = Vector3.zero;

        // Use the gravity direction separately from gravity strength.
        // This remains valid in zero-G.
        Vector3 gWorkGravityDir =
            GravitySystem.GravityDir.sqrMagnitude > 0.0001f
                ? GravitySystem.GravityDir.normalized
                : Vector3.down;

        int bestSurface = -1;
        float bestNormalForce = 1e-6f;

        for (int i = 0; i < contacts.Count; i++)
        {
            float normalForce =
                -Vector3.Dot(
                    contacts[i].normal,
                    A
                );

            if (normalForce > bestNormalForce)
            {
                bestNormalForce = normalForce;
                bestSurface = i;
            }
        }

        bestContact =
            (bestSurface != -1)
                ? contacts[bestSurface]
                : null;

        if (bestSurface == -1)
        {
            if (!ReplayRecorder.loadReplay)
                slipAmount = 0f;
        }

        // Bouncy floors normally get their special bounce here.
        // If jump is being held on a valid jump surface, jump has priority
        // and the bouncy-floor response must not run.
        if (
            !jumpSurfaceAvailable
            && contacts.Count > 0
            && contacts[0].bounce > 0
        )
        {
            Vector3 n =
                contacts[0].normal.normalized;

            float normalComponent =
                Vector3.Dot(
                    marbleVelocity,
                    n
                );

            marbleVelocity -=
                normalComponent * n;

            marbleVelocity +=
                n * contacts[0].bounce;

            return;
        }

        bool _canJump =
            bestSurface != -1;

        if (_canJump && jumpSurfaceAvailable)
        {
            Vector3 velDifference =
                marbleVelocity - bestContact.velocity;

            float sv =
                Vector3.Dot(
                    bestContact.normal,
                    velDifference
                );

            if (sv < 0f)
                sv = 0f;

            if (sv < jumpImpulse)
            {
                marbleVelocity +=
                    bestContact.normal *
                    (jumpImpulse - sv);

                justJumped = 1;
                jumpCooldownRemaining = Mathf.Max(0f, jumpCooldown);

                GameManager.instance.NotifySpecialGameModeJump();
                GameManager.instance.PlayJumpAudio();
            }
        }

        for (int j = 0; j < contacts.Count; j++)
        {
            float normalForce2 =
                -Vector3.Dot(
                    contacts[j].normal,
                    A
                );

            if (
                normalForce2 > 0f
                && Vector3.Dot(
                    contacts[j].normal,
                    marbleVelocity
                        - contacts[j].velocity
                ) <= 0.0001f
            )
            {
                A +=
                    contacts[j].normal
                    * normalForce2;
            }
        }

        if (bestSurface != -1)
        {
            Vector3 vAtC =
                marbleVelocity
                + Vector3.Cross(
                    marbleAngularVelocity,
                    -bestContact.normal
                        * marbleRadius
                )
                - bestContact.velocity;

            float rawSlip =
                vAtC.magnitude
                / maxRollVelocity;

            if (!ReplayRecorder.loadReplay)
                slipAmount =
                    Mathf.Max(
                        slipAmount,
                        rawSlip
                    );

            float vAtCMag =
                vAtC.magnitude;

            bool slipping = false;
            Vector3 aFriction = Vector3.zero;
            Vector3 AFriction = Vector3.zero;

            if (vAtCMag != 0f)
            {
                slipping = true;

                float friction = 0.0f;

                friction =
                    kineticFriction
                    * bestContact.friction;

                float angAMagnitude =
                    5
                    * friction
                    * bestNormalForce
                    / (2 * marbleRadius);

                float AMagnitude =
                    bestNormalForce * friction;

                float totalDeltaV =
                    (
                        angAMagnitude * marbleRadius
                        + AMagnitude
                    )
                    * _dt;

                if (totalDeltaV > vAtCMag)
                {
                    float fraction =
                        vAtCMag
                        / totalDeltaV;

                    angAMagnitude *= fraction;
                    AMagnitude *= fraction;

                    slipping = false;
                }

                Vector3 vAtCDir =
                    vAtC / vAtCMag;

                aFriction =
                    Vector3.Cross(
                        -bestContact.normal,
                        -vAtCDir
                    )
                    * angAMagnitude;

                AFriction =
                    -AMagnitude * vAtCDir;
            }

            if (!slipping)
            {
                Vector3 R =
                    -gWorkGravityDir
                    * marbleRadius;

                Vector3 aadd =
                    Vector3.Cross(R, A)
                    / R.sqrMagnitude;

                if (_isCentered)
                {
                    Vector3 nextOmega =
                        marbleAngularVelocity
                        + a * _dt;

                    _aControl =
                        _desiredOmega
                        - nextOmega;

                    float aScalar =
                        _aControl.magnitude;

                    if (aScalar > brakingAcceleration)
                    {
                        _aControl *=
                            brakingAcceleration
                            / aScalar;
                    }
                }

                Vector3 Aadd = Vector3.zero;

                if (allowInputMovement)
                {
                    Aadd =
                        -Vector3.Cross(
                            _aControl,
                            (-bestContact.normal
                             * marbleRadius)
                        );
                }

                float aAtCMag =
                    (
                        Vector3.Cross(
                            aadd,
                            (-bestContact.normal
                             * marbleRadius)
                        )
                        + Aadd
                    ).magnitude;

                var friction2 = 0.0f;

                friction2 =
                    staticFriction
                    * bestContact.friction;

                if (
                    aAtCMag
                    > friction2 * bestNormalForce
                )
                {
                    friction2 = 0.0f;

                    friction2 =
                        kineticFriction
                        * bestContact.friction;

                    Aadd *=
                        friction2
                        * bestNormalForce
                        / aAtCMag;
                }

                A += Aadd;
                a += aadd;
            }

            A += AFriction;
            a += aFriction;
        }

        a += _aControl;

        if (!ReplayRecorder.loadReplay)
            slipAmount =
                Mathf.MoveTowards(
                    slipAmount,
                    0f,
                    _dt * 2.5f
                );
    }

    /// <summary>
    /// Keeps the marble's contact point moving with a moving/rotating
    /// platform when static friction is capable of preventing slip.
    /// </summary>
    void ApplyMovingSurfaceConstraint(float _dt)
    {
        if (
            _dt <= 0f
            || bestContact == null
            || bestContact.velocity.sqrMagnitude < 0.000001f
        )
        {
            return;
        }

        Vector3 normal =
            bestContact.normal.normalized;

        Vector3 radius =
            -normal * marbleRadius;

        Vector3 marbleContactVelocity =
            marbleVelocity
            + Vector3.Cross(
                marbleAngularVelocity,
                radius
            );

        Vector3 relativeVelocity =
            marbleContactVelocity
            - bestContact.velocity;

        Vector3 tangentVelocity =
            Vector3.ProjectOnPlane(
                relativeVelocity,
                normal
            );

        float tangentSpeed =
            tangentVelocity.magnitude;

        if (tangentSpeed < 0.000001f)
            return;

        float requiredFrictionAcceleration =
            tangentSpeed
            / (3.5f * _dt);

        float maxFrictionAcceleration =
            staticFriction
            * bestContact.friction
            * GetBestNormalForce();

        float frictionAcceleration =
            Mathf.Min(
                requiredFrictionAcceleration,
                maxFrictionAcceleration
            );

        if (frictionAcceleration <= 0f)
            return;

        Vector3 frictionDirection =
            -tangentVelocity / tangentSpeed;

        Vector3 frictionAccelerationVector =
            frictionDirection
            * frictionAcceleration;

        marbleVelocity +=
            frictionAccelerationVector * _dt;

        Vector3 angularAcceleration =
            Vector3.Cross(
                radius,
                frictionAccelerationVector
            )
            * (5f / (2f * marbleRadius));

        marbleAngularVelocity +=
            angularAcceleration * _dt;

        surfaceVelocity =
            bestContact.velocity;

        if (!ReplayRecorder.loadReplay)
        {
            float remainingSlip =
                Mathf.Max(
                    0f,
                    tangentSpeed
                    - frictionAcceleration
                        * 3.5f
                        * _dt
                );

            slipAmount =
                Mathf.Max(
                    slipAmount,
                    remainingSlip
                        / maxRollVelocity
                );
        }
    }

    float GetBestNormalForce()
    {
        if (bestContact == null)
            return 0f;

        float normalForce =
            -Vector3.Dot(
                bestContact.normal,
                GravitySystem.Gravity
            );

        return Mathf.Max(
            0f,
            normalForce
        );
    }

    public void ApplySurfaceBoost(float strength = 24.7f)
    {
        // GravityDir remains valid when gravity strength is zero.
        Vector3 gravityDir =
            GravitySystem.GravityDir.sqrMagnitude > 0.0001f
                ? GravitySystem.GravityDir.normalized
                : Vector3.down;

        Vector3 up =
            -gravityDir;

        // Normal SuperSpeed direction.
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector3 defaultDirection =
            Vector3.ProjectOnPlane(
                mainCamera.transform.forward,
                up
            );

        if (defaultDirection.sqrMagnitude < 1e-6f)
            return;

        defaultDirection.Normalize();

        // Let the active game mode modify the direction.
        //
        // Normal/Null mode simply returns defaultDirection.
        // TwoDMode replaces it with its side-axis direction.
        Vector3 movementVector =
            defaultDirection;

        if (GameManager.instance != null)
        {
            movementVector = defaultDirection;

            foreach (IGameMode mode in GameManager.instance.GameModes)
            {
                if (mode != null)
                {
                    movementVector =
                        mode.GetSuperSpeedDirection(
                            movementVector
                        );
                }
            }
        }
        else
        {
            movementVector = defaultDirection;
        }

        movementVector =
            Vector3.ProjectOnPlane(
                movementVector,
                up
            );

        if (movementVector.sqrMagnitude < 1e-6f)
            return;

        movementVector.Normalize();

        // Remove the component going into the surface.
        Vector3 n = lastNormal;

        if (n.sqrMagnitude > 1e-6f)
        {
            n.Normalize();

            float dot =
                Vector3.Dot(
                    movementVector,
                    n
                );

            movementVector -= n * dot;

            if (movementVector.sqrMagnitude < 1e-6f)
                return;

            movementVector.Normalize();
        }

        marbleVelocity +=
            movementVector * strength;
    }

    void UpdateRollSound(
        float _contactPct,
        float _slipAmount
    )
    {
        AudioSource rollSound =
            Marble.instance.rollingSound;

        AudioSource slipSound =
            Marble.instance.slidingSound;

        if (
            rollSound == null
            || slipSound == null
        )
            return;

        if (
            Marble.instance.isFrozen
            || contacts.Count == 0
            || bestContact == null
            || GameManager.gameFinish
        )
        {
            rollSound.volume = 0f;
            slipSound.volume = 0f;
            return;
        }

        Vector3 contactVelocity =
            marbleVelocity
            + Vector3.Cross(
                marbleAngularVelocity,
                -bestContact.normal
                    * marbleRadius
            )
            - bestContact.velocity;

        float contactSpeed =
            contactVelocity.magnitude;

        Vector3 relativeMovement =
            marbleVelocity
            - bestContact.velocity;

        float rollSpeed =
            relativeMovement.magnitude;

        float scale =
            rollSpeed / maxRollVelocity;

        float rollVolume =
            Mathf.Clamp01(scale * 2f);

        if (_contactPct < 0.05f)
            rollVolume *= 0.2f;

        float slipVolume = 0f;

        if (_slipAmount > 0.0001f)
        {
            slipVolume =
                Mathf.Clamp01(
                    _slipAmount / 2.5f
                );

            rollVolume = 0f;
        }

        float soundVolume =
            PlayerPrefs.GetFloat(
                "Audio_SoundVolume",
                0.5f
            );

        rollSound.volume =
            rollVolume * soundVolume;

        slipSound.volume =
            slipVolume * soundVolume;

        float pitch =
            Mathf.Clamp01(
                rollSpeed / 15f
            )
            * 0.75f
            + 0.75f;

        pitch =
            Mathf.Max(
                pitch,
                0.2f
            );

        rollSound.pitch =
            pitch;
    }

    static class CollisionHelpers
    {
        public static bool ClosestPtPointTriangle(
            Vector3 pt,
            float radius,
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 normal,
            out Vector3 closest
        )
        {
            closest = Vector3.zero;

            float num1 =
                Vector3.Dot(pt, normal);

            float num2 =
                Vector3.Dot(p0, normal);

            if (
                Mathf.Abs(num1 - num2)
                > radius * 1.1
            )
                return false;

            closest =
                pt
                + (num2 - num1) * normal;

            if (
                PointInTriangle(
                    closest,
                    p0,
                    p1,
                    p2
                )
            )
                return true;

            float num3 = 10f;

            if (
                IntersectSegmentCapsule(
                    pt,
                    pt,
                    p0,
                    p1,
                    radius,
                    out var tSeg,
                    out var tCap
                )
                && tSeg < num3
            )
            {
                closest =
                    p0
                    + tCap * (p1 - p0);

                num3 = tSeg;
            }

            if (
                IntersectSegmentCapsule(
                    pt,
                    pt,
                    p1,
                    p2,
                    radius,
                    out tSeg,
                    out tCap
                )
                && tSeg < num3
            )
            {
                closest =
                    p1
                    + tCap * (p2 - p1);

                num3 = tSeg;
            }

            if (
                IntersectSegmentCapsule(
                    pt,
                    pt,
                    p2,
                    p0,
                    radius,
                    out tSeg,
                    out tCap
                )
                && tSeg < num3
            )
            {
                closest =
                    p2
                    + tCap * (p0 - p2);

                num3 = tSeg;
            }

            return num3 < 1.0;
        }

        public static bool PointInTriangle(
            Vector3 pnt,
            Vector3 a,
            Vector3 b,
            Vector3 c
        )
        {
            a -= pnt;
            b -= pnt;
            c -= pnt;

            Vector3 bc =
                Vector3.Cross(b, c);

            Vector3 ca =
                Vector3.Cross(c, a);

            if (Vector3.Dot(bc, ca) < 0.0)
                return false;

            Vector3 ab =
                Vector3.Cross(a, b);

            return Vector3.Dot(bc, ab) >= 0.0;
        }

        public static bool IntersectSegmentCapsule(
            Vector3 segStart,
            Vector3 segEnd,
            Vector3 capStart,
            Vector3 capEnd,
            float radius,
            out float seg,
            out float cap
        )
        {
            return ClosestPtSegmentSegment(
                segStart,
                segEnd,
                capStart,
                capEnd,
                out seg,
                out cap,
                out Vector3 _,
                out Vector3 _
            ) < radius * radius;
        }

        public static float ClosestPtSegmentSegment(
            Vector3 p1,
            Vector3 q1,
            Vector3 p2,
            Vector3 q2,
            out float s,
            out float T,
            out Vector3 c1,
            out Vector3 c2
        )
        {
            float num1 = 0.0001f;

            Vector3 vector31 =
                q1 - p1;

            Vector3 vector32 =
                q2 - p2;

            Vector3 vector33 =
                p1 - p2;

            float num2 =
                Vector3.Dot(
                    vector31,
                    vector31
                );

            float num3 =
                Vector3.Dot(
                    vector32,
                    vector32
                );

            float num4 =
                Vector3.Dot(
                    vector32,
                    vector33
                );

            if (
                num2 <= num1
                && num3 <= num1
            )
            {
                s = T = 0.0f;
                c1 = p1;
                c2 = p2;

                return Vector3.Dot(
                    c1 - c2,
                    c1 - c2
                );
            }

            if (num2 <= num1)
            {
                s = 0.0f;

                T =
                    num4 / num3;

                T =
                    Mathf.Clamp(
                        T,
                        0.0f,
                        1f
                    );
            }
            else
            {
                float num5 =
                    Vector3.Dot(
                        vector31,
                        vector33
                    );

                if (num3 <= num1)
                {
                    T = 0.0f;

                    s =
                        Mathf.Clamp(
                            -num5 / num2,
                            0.0f,
                            1f
                        );
                }
                else
                {
                    float num6 =
                        Vector3.Dot(
                            vector31,
                            vector32
                        );

                    float num7 =
                        (float)(
                            num2 * num3
                            - num6 * num6
                        );

                    s =
                        num7 == 0.0
                            ? 0.0f
                            : Mathf.Clamp(
                                (float)(
                                    num6 * num4
                                    - num5 * num3
                                ) / num7,
                                0.0f,
                                1f
                            );

                    T =
                        (num6 * s + num4)
                        / num3;

                    if (T < 0.0)
                    {
                        T = 0.0f;

                        s =
                            Mathf.Clamp(
                                -num5 / num2,
                                0.0f,
                                1f
                            );
                    }
                    else if (T > 1.0)
                    {
                        T = 1f;

                        s =
                            Mathf.Clamp(
                                (num6 - num5) / num2,
                                0.0f,
                                1f
                            );
                    }
                }
            }

            c1 =
                p1
                + vector31 * s;

            c2 =
                p2
                + vector32 * T;

            return Vector3.Dot(
                c1 - c2,
                c1 - c2
            );
        }

        public static void ClosestPtPointTriangle(
            Vector3 p,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            out Vector3 outP
        )
        {
            // Check if P in vertex region outside A
            var ab = b - a;
            var ac = c - a;
            var ap = p - a;

            var d1 =
                Vector3.Dot(ab, ap);

            var d2 =
                Vector3.Dot(ac, ap);

            if (
                d1 <= 0.0
                && d2 <= 0.0
            )
            {
                outP = a;
                return;
            }

            // Check if P in vertex region outside B
            var bp = p - b;

            var d3 =
                Vector3.Dot(ab, bp);

            var d4 =
                Vector3.Dot(ac, bp);

            if (
                d3 >= 0.0
                && d4 <= d3
            )
            {
                outP = b;
                return;
            }

            // Check if P in edge region of AB
            var vc =
                d1 * d4
                - d3 * d2;

            if (
                vc <= 0.0
                && d1 >= 0.0
                && d3 <= 0.0
            )
            {
                var v2 =
                    d1
                    / (d1 - d3);

                outP =
                    a + ab * v2;

                return;
            }

            // Check if P in vertex region outside C
            var cp = p - c;

            var d5 =
                Vector3.Dot(ab, cp);

            var d6 =
                Vector3.Dot(ac, cp);

            if (
                d6 >= 0.0
                && d5 <= d6
            )
            {
                outP = c;
                return;
            }

            // Check if P in edge region of AC
            var vb =
                d5 * d2
                - d1 * d6;

            if (
                vb <= 0.0
                && d2 >= 0.0
                && d6 <= 0.0
            )
            {
                var w2 =
                    d2
                    / (d2 - d6);

                outP =
                    a + ac * w2;

                return;
            }

            // Check if P in edge region of BC
            var va =
                d3 * d6
                - d5 * d4;

            if (
                va <= 0.0
                && (d4 - d3) >= 0.0
                && (d5 - d6) >= 0.0
            )
            {
                var w3 =
                    (d4 - d3)
                    / (
                        (d4 - d3)
                        + (d5 - d6)
                    );

                outP =
                    b + (c - b) * w3;

                return;
            }

            // P inside face region
            var denom =
                1.0f
                / (va + vb + vc);

            var v =
                vb * denom;

            var w =
                vc * denom;

            outP =
                a + ab * v + ac * w;
        }

        public static bool TriangleSphereIntersection(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 P,
            float r,
            out Vector3 point,
            out Vector3 normal
        )
        {
            ClosestPtPointTriangle(
                P,
                v0,
                v1,
                v2,
                out point
            );

            var v =
                point - P;

            if (v.sqrMagnitude <= r * r)
            {
                normal =
                    P - point;

                normal.Normalize();

                return true;
            }
            else
            {
                normal =
                    Vector3.zero;

                return false;
            }
        }
    }
}