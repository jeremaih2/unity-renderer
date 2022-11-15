using DCL;
using DCL.Configuration;
using DCL.Helpers;
using UnityEngine;
using Cinemachine;

public class DCLCharacterController : MonoBehaviour
{
    public static DCLCharacterController i { get; private set; }

    private const float CONTROLLER_DRIFT_OFFSET = 0.15f;//偏移差值

    [Header("Movement")]
    public float minimumYPosition = 1f;//最低Y的位置

    public float groundCheckExtraDistance = 0.1f;//检查地面最大距离
    public float gravity = -55f;//重力
    public float jumpForce = 12f;//跳跃力度
    public float movementSpeed = 8f;//移动速度
    public float runningSpeedMultiplier = 2f;//奔跑速度乘数
    //public int jumpLimit = 3;//跳跃限制

    public DCLCharacterPosition characterPosition;//角色位置

    [Header("Collisions")]
    public LayerMask groundLayers;//碰撞层
    
    [Header("Additional Camera Layers")]
    public LayerMask cameraLayers;//相机层

    [System.NonSerialized]
    public bool initialPositionAlreadySet = false;//初始化位置是否已经设置

    [System.NonSerialized]
    public bool characterAlwaysEnabled = true;//人物是否一直激活

    [System.NonSerialized]
    public CharacterController characterController;//角色控制

    FreeMovementController freeMovementController;//自由移动控制

    new Collider collider;

    float lastUngroundedTime = 0f;//最后不在地面时间
    float lastJumpButtonPressedTime = 0f;//最后跳跃按钮时间
    float lastMovementReportTime;//最后移动报告时间
    float originalGravity;//起始重力
    Vector3 lastLocalGroundPosition;//最后本地位置

    Vector3 lastCharacterRotation;//最后角色旋转
    Vector3 lastGlobalCharacterRotation;//最后全局角色旋转

    Vector3 velocity = Vector3.zero;

    //public Plane p;
    //public Light l;
    //public Transform t;
    public bool isWalking { get; private set; } = false;
    public bool isMovingByUserInput { get; private set; } = false;
    public bool isJumping { get; private set; } = false;
    public bool isGrounded { get; private set; }
    public bool isOnMovingPlatform { get; private set; }

    internal Transform groundTransform;

    Vector3 lastPosition;
    Vector3 groundLastPosition;
    Quaternion groundLastRotation;
    bool jumpButtonPressed = false;//是否按下跳跃按钮

    [Header("InputActions")]
    public InputAction_Hold jumpAction;//跳跃事件

    public InputAction_Hold sprintAction;//冲刺事件

    public Vector3 moveVelocity;//移动速度

    private InputAction_Hold.Started jumpStartedDelegate;//开始跳的委托
    private InputAction_Hold.Finished jumpFinishedDelegate;//完成跳的委托
    private InputAction_Hold.Started walkStartedDelegate;//开始走的委托
    private InputAction_Hold.Finished walkFinishedDelegate;//完成走的委托

    private Vector3NullableVariable characterForward => CommonScriptableObjects.characterForward;

    public static System.Action<DCLCharacterPosition> OnCharacterMoved;//角色移动
    public static System.Action<DCLCharacterPosition> OnPositionSet;//设置位置
    public event System.Action<float> OnUpdateFinish;//完成更新

    public GameObject avatarGameObject;//角色gameobject
    public GameObject firstPersonCameraGameObject;//第一人称视角的gameobject

    [SerializeField]
    private InputAction_Measurable characterYAxis;//角色Y轴

    [SerializeField]
    private InputAction_Measurable characterXAxis;//角色X轴

    private Vector3Variable cameraForward => CommonScriptableObjects.cameraForward;
    private Vector3Variable cameraRight => CommonScriptableObjects.cameraRight;

    private readonly DataStore_Player dataStorePlayer = DataStore.i.player;

    [System.NonSerialized]
    public float movingPlatformSpeed;//地面移动速度
    private CollisionFlags lastCharacterControllerCollision;//最后一次角色控制器的碰撞器

    public event System.Action OnJump;//在跳跃
    public event System.Action OnHitGround;//隐藏地面
    public event System.Action<float> OnMoved;//在移动
    
    void Awake()
    {
        if (i != null)
        {
            Destroy(gameObject);
            return;
        }

        i = this;
        originalGravity = gravity;

        SubscribeToInput();
        CommonScriptableObjects.playerUnityPosition.Set(Vector3.zero);
        dataStorePlayer.playerWorldPosition.Set(Vector3.zero);
        CommonScriptableObjects.playerCoords.Set(Vector2Int.zero);
        dataStorePlayer.playerGridPosition.Set(Vector2Int.zero);
        CommonScriptableObjects.playerUnityEulerAngles.Set(Vector3.zero);

        characterPosition = new DCLCharacterPosition();
        characterController = GetComponent<CharacterController>();
        freeMovementController = GetComponent<FreeMovementController>();
        collider = GetComponent<Collider>();

        CommonScriptableObjects.worldOffset.OnChange += OnWorldReposition;

        lastPosition = transform.position;
        transform.parent = null;

        CommonScriptableObjects.rendererState.OnChange += OnRenderingStateChanged;
        OnRenderingStateChanged(CommonScriptableObjects.rendererState.Get(), false);

        if (avatarGameObject == null || firstPersonCameraGameObject == null)
        {
            throw new System.Exception("Both the avatar and first person camera game objects must be set.");
        }

        var worldData = DataStore.i.Get<DataStore_World>();
        worldData.avatarTransform.Set(avatarGameObject.transform);
        worldData.fpsTransform.Set(firstPersonCameraGameObject.transform);
    }

    private void SubscribeToInput()//订阅输入
    {//跳跃，行走，开始，完成的委托
        jumpStartedDelegate = (action) =>
        {
            lastJumpButtonPressedTime = Time.time;
            jumpButtonPressed = true;
        };
        jumpFinishedDelegate = (action) => jumpButtonPressed = false;
        jumpAction.OnStarted += jumpStartedDelegate;
        jumpAction.OnFinished += jumpFinishedDelegate;

        walkStartedDelegate = (action) => isWalking = true;
        walkFinishedDelegate = (action) => isWalking = false;
        sprintAction.OnStarted += walkStartedDelegate;
        sprintAction.OnFinished += walkFinishedDelegate;
    }

    void OnDestroy()
    {
        CommonScriptableObjects.worldOffset.OnChange -= OnWorldReposition;
        jumpAction.OnStarted -= jumpStartedDelegate;
        jumpAction.OnFinished -= jumpFinishedDelegate;
        sprintAction.OnStarted -= walkStartedDelegate;
        sprintAction.OnFinished -= walkFinishedDelegate;
        CommonScriptableObjects.rendererState.OnChange -= OnRenderingStateChanged;
        i = null;
    }

    void OnWorldReposition(Vector3 current, Vector3 previous)
    {//在世界重新定位
        Vector3 oldPos = this.transform.position;
        this.transform.position = characterPosition.unityPosition; //CommonScriptableObjects.playerUnityPosition;

        if (CinemachineCore.Instance.BrainCount > 0)
        {
            CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera?.OnTargetObjectWarped(transform, transform.position - oldPos);
        }
    }

    public void SetPosition(Vector3 newPosition)
    {
        // failsafe in case something teleports the player below ground collisions
        //以防有东西将玩家传送到地面碰撞
        if (newPosition.y < minimumYPosition)
        {
            newPosition.y = minimumYPosition + 2f;
        }

        lastPosition = characterPosition.worldPosition;
        characterPosition.worldPosition = newPosition;
        transform.position = characterPosition.unityPosition;
        Environment.i.platform.physicsSyncController?.MarkDirty();

        CommonScriptableObjects.playerUnityPosition.Set(characterPosition.unityPosition);
        dataStorePlayer.playerWorldPosition.Set(characterPosition.worldPosition);
        Vector2Int playerPosition = Utils.WorldToGridPosition(characterPosition.worldPosition);
        CommonScriptableObjects.playerCoords.Set(playerPosition);
        dataStorePlayer.playerGridPosition.Set(playerPosition);
        dataStorePlayer.playerUnityPosition.Set(characterPosition.unityPosition);

        if (Moved(lastPosition))
        {
            if (Moved(lastPosition, useThreshold: true))
                ReportMovement();

            OnCharacterMoved?.Invoke(characterPosition);

            float distance = Vector3.Distance(characterPosition.worldPosition, lastPosition) - movingPlatformSpeed;

            if (distance > 0f && isGrounded)
                OnMoved?.Invoke(distance);
        }

        lastPosition = transform.position;
    }

    public void Teleport(string teleportPayload)
    {//传送
        ResetGround();//重置地板

        var payload = Utils.FromJsonWithNulls<Vector3>(teleportPayload);

        var newPosition = new Vector3(payload.x, payload.y, payload.z);
        SetPosition(newPosition);

        if (OnPositionSet != null)
        {
            OnPositionSet.Invoke(characterPosition);
        }

        DataStore.i.player.lastTeleportPosition.Set(newPosition, true);

        if (!initialPositionAlreadySet)
        {
            initialPositionAlreadySet = true;
        }
    }

    [System.Obsolete("SetPosition is deprecated, please use Teleport instead.", true)]
    public void SetPosition(string positionVector) { Teleport(positionVector); }

    public void SetEnabled(bool enabled) { this.enabled = enabled; }

    bool Moved(Vector3 previousPosition, bool useThreshold = false)
    {
        if (useThreshold)//使用阈值
            return Vector3.Distance(characterPosition.worldPosition, previousPosition) > 0.001f;
        else
            return characterPosition.worldPosition != previousPosition;//上一个位置
    }

    internal void LateUpdate()
    {
        if(!DataStore.i.player.canPlayerMove.Get())
            return;

        if (transform.position.y < minimumYPosition)
        {//当前y轴的位置小于最小y的位置
            SetPosition(characterPosition.worldPosition);//设置角色的位置，角色世界位置
            return;
        }

        if (freeMovementController.IsActive())
        {//自由移动控制是否激活
            velocity = freeMovementController.CalculateMovement();//速度=自由移动的计算移动
            Debug.Log("自由移动控制的状态是："+freeMovementController.IsActive());
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
            velocity.y += gravity * Time.deltaTime;

            bool previouslyGrounded = isGrounded;

            if (!isJumping || velocity.y <= 0f)
                CheckGround();

            if (isGrounded)
            {
                isJumping = false;
                velocity.y = gravity * Time.deltaTime; // to avoid accumulating gravity in velocity.y while grounded 避免重力在速度上积累。y而停飞
            }
            else if (previouslyGrounded && !isJumping)
            {
                lastUngroundedTime = Time.time;
            }

            if (characterForward.HasValue())
            {
                // Horizontal movement
                var speed = movementSpeed * (isWalking ? runningSpeedMultiplier : 1f);

                transform.forward = characterForward.Get().Value;

                var xzPlaneForward = Vector3.Scale(cameraForward.Get(), new Vector3(1, 0, 1));
                var xzPlaneRight = Vector3.Scale(cameraRight.Get(), new Vector3(1, 0, 1));

                Vector3 forwardTarget = Vector3.zero;

                if (characterYAxis.GetValue() > CONTROLLER_DRIFT_OFFSET)
                    forwardTarget += xzPlaneForward;
                if (characterYAxis.GetValue() < -CONTROLLER_DRIFT_OFFSET)
                    forwardTarget -= xzPlaneForward;

                if (characterXAxis.GetValue() > CONTROLLER_DRIFT_OFFSET)
                    forwardTarget += xzPlaneRight;
                if (characterXAxis.GetValue() < -CONTROLLER_DRIFT_OFFSET)
                    forwardTarget -= xzPlaneRight;

                if (forwardTarget.Equals(Vector3.zero))
                    isMovingByUserInput = false;
                else
                    isMovingByUserInput = true;


                forwardTarget.Normalize();
                velocity += forwardTarget * speed;
                CommonScriptableObjects.playerUnityEulerAngles.Set(transform.eulerAngles);
            }

            bool jumpButtonPressedWithGraceTime = jumpButtonPressed && (Time.time - lastJumpButtonPressedTime < 0.15f);//是否跳跃按钮宽限时间

            if (jumpButtonPressedWithGraceTime) // almost-grounded jump button press allowed time几乎接地跳按钮按允许的时间
            {
                bool justLeftGround = (Time.time - lastUngroundedTime) < 0.1f;

                if (isGrounded || justLeftGround) // just-left-ground jump allowed time 左跳是有时间的
                {
                        Jump();
                }
            }

            //NOTE(Mordi): Detecting when the character hits the ground (for landing-SFX)检测角色何时触地(用于着陆- sfx)
            if (isGrounded && !previouslyGrounded && (Time.time - lastUngroundedTime) > 0.4f)
            {
                OnHitGround?.Invoke();
            }
        }

        if (characterController.enabled)
        {
            //NOTE(Brian): Transform has to be in sync before the Move call, otherwise this call
            //             will reset the character controller to its previous position.
            //             Transform必须在Move调用之前同步，否则这个调用将重置角色控制器到它之前的位置。
            Environment.i.platform.physicsSyncController?.Sync();
            lastCharacterControllerCollision = characterController.Move(velocity * Time.deltaTime);
        }

        SetPosition(PositionUtils.UnityToWorldPosition(transform.position));

        if ((DCLTime.realtimeSinceStartup - lastMovementReportTime) > PlayerSettings.POSITION_REPORTING_DELAY)
        {
            ReportMovement();
        }

        if (isOnMovingPlatform)
        {
            SaveLateUpdateGroundTransforms();
        }
        OnUpdateFinish?.Invoke(Time.deltaTime);
    }

    private void SaveLateUpdateGroundTransforms()
    {
        lastLocalGroundPosition = groundTransform.InverseTransformPoint(transform.position);

        if (CommonScriptableObjects.characterForward.HasValue())
        {
            lastCharacterRotation = groundTransform.InverseTransformDirection(CommonScriptableObjects.characterForward.Get().Value);
            lastGlobalCharacterRotation = CommonScriptableObjects.characterForward.Get().Value;
        }
    }

    void Jump()
    {
        if (isJumping)
            return;

        //if (isGrounded) jumpLimit = 3;

        isJumping = true;
        isGrounded = false;

        ResetGround();

        //if (jumpLimit>0)
        //{
        //    velocity.y = jumpForce;
        //    jumpLimit-- ;
        //}
        velocity.y = jumpForce;
        //cameraTargetProbe.damping.y = dampingOnAir;

        OnJump?.Invoke();
    }

    public void ResetGround()
    {
        if (isOnMovingPlatform)
            CommonScriptableObjects.playerIsOnMovingPlatform.Set(false);

        isOnMovingPlatform = false;
        groundTransform = null;
        movingPlatformSpeed = 0;
    }

    void CheckGround()
    {
        if (groundTransform == null)
            ResetGround();

        if (isOnMovingPlatform)
        {
            Physics.SyncTransforms();
            //NOTE(Brian): This should move the character with the moving platform
            Vector3 newGroundWorldPos = groundTransform.TransformPoint(lastLocalGroundPosition);
            movingPlatformSpeed = Vector3.Distance(newGroundWorldPos, transform.position);
            transform.position = newGroundWorldPos;

            Vector3 newCharacterForward = groundTransform.TransformDirection(lastCharacterRotation);
            Vector3 lastFrameDifference = Vector3.zero;
            if (CommonScriptableObjects.characterForward.HasValue())
            {
                lastFrameDifference = CommonScriptableObjects.characterForward.Get().Value - lastGlobalCharacterRotation;
            }

            //NOTE(Kinerius) CameraStateTPS rotates the character between frames so we add the difference.
            //               if we dont do this, the character wont rotate when moving, only when the platform rotates
            CommonScriptableObjects.characterForward.Set(newCharacterForward + lastFrameDifference);
        }

        Transform transformHit = CastGroundCheckingRays();

        if (transformHit != null)
        {
            if (groundTransform == transformHit)
            {
                bool groundHasMoved = (transformHit.position != groundLastPosition || transformHit.rotation != groundLastRotation);

                if (!characterPosition.RepositionedWorldLastFrame()
                    && groundHasMoved)
                {
                    isOnMovingPlatform = true;
                    CommonScriptableObjects.playerIsOnMovingPlatform.Set(true);
                    Physics.SyncTransforms();
                    SaveLateUpdateGroundTransforms();

                    Quaternion deltaRotation = groundTransform.rotation * Quaternion.Inverse(groundLastRotation);
                    CommonScriptableObjects.movingPlatformRotationDelta.Set(deltaRotation);
                }
            }
            else
            {
                groundTransform = transformHit;
                CommonScriptableObjects.movingPlatformRotationDelta.Set(Quaternion.identity);
            }
        }
        else
        {
            ResetGround();
        }

        if (groundTransform != null)
        {
            groundLastPosition = groundTransform.position;
            groundLastRotation = groundTransform.rotation;
        }

        isGrounded = IsLastCollisionGround() || groundTransform != null && groundTransform.gameObject.activeInHierarchy;
    }

    public Transform CastGroundCheckingRays()
    {
        RaycastHit hitInfo;

        var result = CastGroundCheckingRays(transform, collider, groundCheckExtraDistance, 0.9f, groundLayers, out hitInfo);

        if ( result )
        {
            return hitInfo.transform;
        }

        return null;
    }

    public bool CastGroundCheckingRays(float extraDistance, float scale, out RaycastHit hitInfo)
    {
        if (CastGroundCheckingRays(transform, collider, extraDistance, scale, groundLayers | cameraLayers , out hitInfo))
            return true;

        return IsLastCollisionGround();
    }

    public bool CastGroundCheckingRay(float extraDistance, out RaycastHit hitInfo)
    {
        Bounds bounds = collider.bounds;
        float rayMagnitude = (bounds.extents.y + extraDistance);
        bool test = CastGroundCheckingRay(transform.position, out hitInfo, rayMagnitude, groundLayers);
        return IsLastCollisionGround() || test;
    }

    // We secuentially cast rays in 4 directions (only if the previous one didn't hit anything)
    public static bool CastGroundCheckingRays(Transform transform, Collider collider, float extraDistance, float scale, int groundLayers, out RaycastHit hitInfo)
    {
        Bounds bounds = collider.bounds;

        float rayMagnitude = (bounds.extents.y + extraDistance);
        float originScale = scale * bounds.extents.x;

        if (!CastGroundCheckingRay(transform.position, out hitInfo, rayMagnitude, groundLayers) // center
            && !CastGroundCheckingRay( transform.position + transform.forward * originScale, out hitInfo, rayMagnitude, groundLayers) // forward
            && !CastGroundCheckingRay( transform.position + transform.right * originScale, out hitInfo, rayMagnitude, groundLayers) // right
            && !CastGroundCheckingRay( transform.position + -transform.forward * originScale, out hitInfo, rayMagnitude, groundLayers) // back
            && !CastGroundCheckingRay( transform.position + -transform.right * originScale, out hitInfo, rayMagnitude, groundLayers)) // left
        {
            return false;
        }

        // At this point there is a guaranteed hit, so this is not null
        return true;
    }

    public static bool CastGroundCheckingRay(Vector3 origin, out RaycastHit hitInfo, float rayMagnitude, int groundLayers)
    {
        var ray = new Ray();
        ray.origin = origin;
        ray.direction = Vector3.down * rayMagnitude;

        var result = Physics.Raycast(ray, out hitInfo, rayMagnitude, groundLayers);

#if UNITY_EDITOR
        if ( result )
            Debug.DrawLine(ray.origin, hitInfo.point, Color.green);
        else
            Debug.DrawRay(ray.origin, ray.direction, Color.red);
#endif

        return result;
    }

    void ReportMovement()
    {
        float height = 0.875f;

        var reportPosition = characterPosition.worldPosition + (Vector3.up * height);
        var compositeRotation = Quaternion.LookRotation(characterForward.HasValue() ? characterForward.Get().Value : cameraForward.Get());
        var playerHeight = height + (characterController.height / 2);
        var cameraRotation = Quaternion.LookRotation(cameraForward.Get());

        //NOTE(Brian): We have to wait for a Teleport before sending the ReportPosition, because if not ReportPosition events will be sent
        //             When the spawn point is being selected / scenes being prepared to be sent and the Kernel gets crazy.

        //             The race conditions that can arise from not having this flag can result in:
        //                  - Scenes not being sent for loading, making ActivateRenderer never being sent, only in WSS mode.
        //                  - Random teleports to 0,0 or other positions that shouldn't happen.
        if (initialPositionAlreadySet)
            DCL.Interface.WebInterface.ReportPosition(reportPosition, compositeRotation, playerHeight, cameraRotation);

        lastMovementReportTime = DCLTime.realtimeSinceStartup;
    }

    public void PauseGravity()
    {
        gravity = 0f;
        velocity.y = 0f;
    }

    public void ResumeGravity() { gravity = originalGravity; }

    void OnRenderingStateChanged(bool isEnable, bool prevState) { SetEnabled(isEnable); }

    bool IsLastCollisionGround()
    {
        return (lastCharacterControllerCollision & CollisionFlags.Below) != 0;
    }
}
