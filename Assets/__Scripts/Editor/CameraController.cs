using System;
using System.Collections;
using System.Collections.Generic;
using Beatmap.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class CameraController : MonoBehaviour, CMInput.ICameraActions
{
    private static CameraController instance;

    [SerializeField] private Vector3[] presetPositions;
    [SerializeField] private Vector3[] presetRotations;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float mouseSensitivity;
    [SerializeField] private Transform noteGridTransform;

    [SerializeField] private UIMode uiMode;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [SerializeField] private LaneRotationProvider laneRotationProvider;
    [SerializeField] public Camera Camera;

    [Header("Debug")] [SerializeField] private float x;

    [SerializeField] private float y;
    [SerializeField] private float z;

    [SerializeField] private float mouseX;
    [SerializeField] private float mouseY;

    [SerializeField] private bool playerCamera;
    [SerializeField] private ObjectAnimator cameraAnimator;
    [SerializeField] private AudioTimeSyncController atsc;

    private readonly Type[] actionMapsDisabledWhileMoving =
    {
        typeof(CMInput.IPlacementControllersActions),
        typeof(CMInput.ISharedNoteObjectsActions),
        typeof(CMInput.IEventPlacementActions),
        typeof(CMInput.ISavingActions),
        typeof(CMInput.ITimelineActions),
        typeof(CMInput.IPlatformSoloLightGroupActions),
        typeof(CMInput.IPlaybackActions),
        typeof(CMInput.IBeatmapObjectsActions),
        typeof(CMInput.INoteObjectsActions),
        typeof(CMInput.IEventObjectsActions),
        typeof(CMInput.IObstacleObjectsActions),
        typeof(CMInput.ICustomEventsContainerActions),
        typeof(CMInput.IBPMTapperActions),
        typeof(CMInput.IEventUIActions),
        typeof(CMInput.IUIModeActions),
        typeof(CMInput.IArcObjectsActions),
        typeof(CMInput.IArcPlacementActions),
        typeof(CMInput.IChainObjectsActions),
        typeof(CMInput.IChainPlacementActions),
        typeof(CMInput.IScrollPrecisionActions),
        typeof(CMInput.IBoxSelectActions),
        typeof(CMInput.IGLSColorObjectsActions),
        typeof(CMInput.IGLSRotationObjectsActions),
        typeof(CMInput.IGLSTranslationObjectsActions),
        typeof(CMInput.IGLSFloatFXObjectsActions),
        typeof(CMInput.IGLSGroupTabsActions),
        typeof(CMInput.IGLSGroupSelectActions),
        typeof(CMInput.IEasingsSelectionActions),
        typeof(CMInput.IRotationObjectsActions)
    };

    private Vector2 savedMousePos = Vector2.zero;

    // ResumingPlayingDoesNotRestoreEditingCameraMousePosition regression seam: batch mode cannot apply a
    // real OS cursor lock or warp, so SetLockState's native cursor calls route through this adapter. The
    // adapter only mirrors the existing Cursor.lockState/WarpCursorPosition behavior so the test can
    // record the call sequence — the adapter itself owns no logic; lock ownership is tracked separately
    // by cursorLockOwner inside SetLockState.
    internal interface ICursorState
    {
        CursorLockMode LockState { get; set; }
        void Warp(Vector2 position);
    }

    private sealed class NativeCursorState : ICursorState
    {
        public CursorLockMode LockState
        {
            get => Cursor.lockState;
            set => Cursor.lockState = value;
        }
        public void Warp(Vector2 position) => Mouse.current.WarpCursorPosition(position);
    }

    internal static ICursorState CursorState { get; set; } = new NativeCursorState();

    // ResumingPlayingDoesNotRestoreEditingCameraMousePosition: both camera controllers stay enabled,
    // but only the controller that acquired the global cursor lock may restore its saved position.
    private static CameraController cursorLockOwner;

    private bool canMoveCamera;

    private bool lockOntoNoteGrid;

    private bool secondSetOfLocations;
    private bool setLocation;

    private List<float> playerTrackTimes = new();
    private List<TrackAnimator> playerTracks = new();
    private TrackAnimator currentTrack;

    // Pre-bind home for the playing-camera rig. ConnectPlayerTrack reparents cameraAnimator under the
    // track's ObjectParentTransform, so the rig's authored parent/local pose must be captured on the
    // first bind and restored on disconnect — otherwise rewinding before the bind beat (or unloading
    // the map) leaves the camera stuck under a stale track at its last animated pose.
    private bool cameraHomeCaptured;
    private Transform cameraHomeParent;
    private Vector3 cameraHomeLocalPosition;
    private Quaternion cameraHomeLocalRotation;
    private Vector3 cameraHomeLocalScale;


    private bool ignoreInitialMouseMovement = false;
    private int framesAfterRightClick = 0;

    public bool LockedOntoNoteGrid
    {
        get => lockOntoNoteGrid;
        set
        {
            var camTransform = transform;
            camTransform.SetParent(!value ? null : noteGridTransform);
            camTransform.localScale = Vector3.one;
            lockOntoNoteGrid = value;
        }
    }

    public bool MovingCamera => canMoveCamera;

    public void AddPlayerTrack(float time, TrackAnimator track)
    {
        playerTrackTimes.Add(time);
        playerTracks.Add(track);
    }

    public void ClearPlayerTracks()
    {
        // A live binding outlives the lists: TrackAnimator.ResetForMapLoad already emptied the track's
        // Children on map load, so without disconnecting the cameraAnimator stays parented but undriven.
        DisconnectPlayerTrack();
        playerTrackTimes.Clear();
        playerTracks.Clear();
    }

    private void Start()
    {
        Camera.fieldOfView = playerCamera ? Settings.Instance.PlayerCameraFOV : Settings.Instance.CameraFOV;
        UpdateAA(Settings.Instance.CameraAA);
        UpdateRenderScale(Settings.Instance.RenderScale);
        UpdatePlayerCameraOffsetZ(Settings.Instance.PlayerCameraOffsetZ);
        // In-place map swaps and difficulty switches keep this rig alive, and direct field writes to these
        // settings bypass the NotifyBySettingName callbacks; re-apply the same Start-time snapshot on every
        // map load so the camera reads current settings exactly like a fresh scene load would.
        LoadInitialMap.OnLevelLoaded += HandleMapLoaded;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged += HandleMapLoaded;
        Settings.NotifyBySettingName(nameof(Settings.CameraAA), UpdateAA);
        Settings.NotifyBySettingName(nameof(Settings.RenderScale), UpdateRenderScale);
        Settings.NotifyBySettingName(nameof(Settings.PlayerCameraOffsetZ), UpdatePlayerCameraOffsetZ);
        if (!playerCamera)
        {
            OnLocation(0);
            LockedOntoNoteGrid = true;
        }
        else
        {
            laneRotationProvider.OnSmoothedPlaybackChanged += HandleRotationChanged;
            // Salty camera regression (SaltyBeat537HeadCameraParityTest /
            // Beat532CameraStaysHomeBeforeAndAfterHeadTrackVisit): binding only ran from Update, one
            // frame after a stopped seek, so the camera lagged a beat and mode switches/rewinds left
            // it on a stale track. Subscribe to seek and mode events so the bind/detach is
            // synchronous with the time/mode change.
            atsc.OnTimeChanged += SyncPlayerTrack;
            UIMode.OnUIModeSwitched += OnPlayerCameraModeSwitched;
        }
    }

    private void OnDestroy()
    {
        if (playerCamera)
        {
            laneRotationProvider.OnSmoothedPlaybackChanged -= HandleRotationChanged;
            atsc.OnTimeChanged -= SyncPlayerTrack;
            UIMode.OnUIModeSwitched -= OnPlayerCameraModeSwitched;
        }
        LoadInitialMap.OnLevelLoaded -= HandleMapLoaded;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= HandleMapLoaded;
    }

    private void HandleMapLoaded()
    {
        Camera.fieldOfView = playerCamera ? Settings.Instance.PlayerCameraFOV : Settings.Instance.CameraFOV;
        UpdateAA(Settings.Instance.CameraAA);
        UpdateRenderScale(Settings.Instance.RenderScale);
        UpdatePlayerCameraOffsetZ(Settings.Instance.PlayerCameraOffsetZ);
    }

    private void Update()
    {
        if (PauseManager.IsPaused || SceneTransitionManager.IsLoading)
            return; //Dont move camera if we are in pause menu or loading screen

        Camera.fieldOfView = playerCamera ? Settings.Instance.PlayerCameraFOV : Settings.Instance.CameraFOV;

        if (playerCamera)
        {
            SyncPlayerTrack();
        }
        else if (canMoveCamera)
        {
            if (CMInputCallbackInstaller.IsActionMapDisabled(typeof(CMInput.ICameraActions)))
            {
                canMoveCamera = false;
                x = y = z = mouseY = mouseX = 0;
                return;
            }

            HandleCameraHeldMovementKeys();

            SetLockState(true);

            movementSpeed = Settings.Instance.Camera_MovementSpeed;
            mouseSensitivity = Settings.Instance.Camera_MouseSensitivity;

            var movementSpeedInFrame = movementSpeed * Time.deltaTime;

            var sideTranslation = movementSpeedInFrame * new Vector3(x, 0, z);
            transform.Translate(sideTranslation);
            // Y translation should always be in World space
            transform.Translate(movementSpeedInFrame * y * Vector3.up, Space.World);

            // We want to force it to never rotate Z
            var eulerAngles = transform.eulerAngles;
            var ex = eulerAngles.x;
            ex = ex > 180 ? ex - 360 : ex;
            eulerAngles.x = Mathf.Clamp(ex + -mouseY, -89.5f, 89.5f); //pepega code to fix pepega camera :)
            eulerAngles.y += mouseX;
            eulerAngles.z = 0;
            transform.eulerAngles = eulerAngles;
        }
        else
        {
            z = x = 0;
            SetLockState(false);
        }
    }

    // Handles the mode switch after UIMode.SelectedMode has already been updated.
    private void OnPlayerCameraModeSwitched(UIModeType mode) => SyncPlayerTrack();

    // Salty camera regression (SaltyBeat537HeadCameraParityTest /
    // Beat532CameraStaysHomeBeforeAndAfterHeadTrackVisit): the playing camera must bind/detach on the
    // last AssignPlayerToTrack event at/before the current beat while UIMode is Playing — invoked
    // from Update during playback and synchronously from Atsc.OnTimeChanged / UIMode.OnUIModeSwitched
    // for paused seeks and mode switches.
    private void SyncPlayerTrack()
    {
        // Salty mode-switch regression (LeavingPlayingAtBeat541UnbindsHeadCameraWithoutSeek): this
        // check must come before the AnimationMode/track-list early returns — leaving Playing for
        // Normal sets AnimationMode false, and returning early kept the rig bound to asdkm at its
        // animated pose until a rewind forced a re-evaluation. Any non-Playing mode, animations off,
        // or no player tracks detaches synchronously.
        if (UIMode.SelectedMode != UIModeType.Playing || !UIMode.AnimationMode || playerTrackTimes.Count == 0)
        {
            DisconnectPlayerTrack();
            return;
        }

        // 1 after last point, inverted (probably)
        var later = playerTrackTimes.BinarySearch(atsc.CurrentJsonTime);

        var current = (later < 0)
            ? (~later) - 1
            : later;

        if (current < 0)
        {
            DisconnectPlayerTrack();
            return;
        }

        if (playerTracks[current] != currentTrack)
        {
            DisconnectPlayerTrack();
            currentTrack = playerTracks[current];
            // Capture the authored rig home once — on the first bind the transform still sits at its
            // pristine spot, while later track switches re-enter this block already parented under a
            // track. DisconnectPlayerTrack restores this snapshot.
            if (!cameraHomeCaptured)
            {
                cameraHomeCaptured = true;
                var rigTransform = cameraAnimator.transform;
                cameraHomeParent = rigTransform.parent;
                cameraHomeLocalPosition = rigTransform.localPosition;
                cameraHomeLocalRotation = rigTransform.localRotation;
                cameraHomeLocalScale = rigTransform.localScale;
            }
            // Salty b537 Head camera + WorldCaves noodle-note regressions
            // (PlayingModeKeepsNoodleNotesAndEnvironmentConstructsTogetherAtSongStart): the rig is
            // reparented under the track's ObjectParentTransform so the hierarchy still matches the
            // game, but the animator must NOT write to that shared parent — AttachToTrack targets it
            // directly, so a V2 `_position` push translates every noodle note riding the same named
            // track a second time (notes landed ~2x too deep). Camera-only targets keep the track
            // values on the camera: LocalTarget = the AnimationThis child for V2 `_position` (offset,
            // lanes * .6 onto the camera child while the rig keeps authored local y≈-0.6) and
            // WorldTarget = the rig root for V3 `position` (absolute world write, so the captured
            // home local pose is preloaded and held until flush to survive the same-frame
            // LateUpdate). Push + LateUpdate still applies held values synchronously on this beat.
            var isV2Map = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
            var rig = cameraAnimator.transform;
            rig.SetParent(currentTrack.Track.ObjectParentTransform, false);
            rig.localPosition = cameraHomeLocalPosition;
            rig.localRotation = cameraHomeLocalRotation;
            rig.localScale = cameraHomeLocalScale;
            cameraAnimator.ResetData();
            cameraAnimator.LocalTarget = cameraAnimator.AnimationThis.transform;
            cameraAnimator.WorldTarget = rig;
            cameraAnimator.TargetType = ObjectAnimator.TargetTypes.Transform;
            if (!isV2Map)
            {
                cameraAnimator.WorldPosition.Preload(cameraHomeLocalPosition);
                cameraAnimator.WorldPosition.HoldUntilFlush = true;
            }

            cameraAnimator.enabled = true;
            currentTrack.AddChild(cameraAnimator);
            currentTrack.PushToChild(cameraAnimator);
            cameraAnimator.LateUpdate();
        }
    }

    private void UpdateAA(object aaValue)
    {
        // The game has no post-process AA (and ChroMapper's post processing stack
        // is gone), so the option maps straight to camera MSAA sample counts.
        // 0 and 1 (a no-op single sample) disable AA.
        QualitySettings.antiAliasing = Mathf.Clamp((int)aaValue, 0, 4);
    }

    private void UpdateRenderScale(object renderScale)
    {
        // TODO: find way to make it scale above 100%
        var scale = Mathf.Min((int)renderScale / 100f, 1f);
        ScalableBufferManager.ResizeBuffers(scale, scale);
        // QualitySettings.resolutionScalingFixedDPIFactor = scale;
    }

    private void UpdatePlayerCameraOffsetZ(object posZ)
    {
        if (playerCamera)
        {
            var newLocalPosition = transform.localPosition;
            newLocalPosition.z = -(float)posZ;
            transform.localPosition = newLocalPosition;
        }
    }

    // ResumingPlayingDoesNotRestoreEditingCameraMousePosition: both camera controllers stay enabled, so
    // only the controller that acquired the global cursor lock may release it — the idle editing camera's
    // per-frame unlock must not steal the playing camera's lock and warp to its stale right-click spot.
    public void SetLockState(bool lockMouse)
    {
        var mouseLocked = CursorState.LockState == CursorLockMode.Locked;
        if (lockMouse && !mouseLocked)
        {
            // DisabledPlayingCameraDoesNotBreakEditingCameraCursorLock: this is an instance method on the
            // editing controller, so keep the cursor-save state on `this` rather than the global static,
            // which another camera's OnDisable may have already cleared.
            savedMousePos = Mouse.current.position.ReadValue();
            cursorLockOwner = this;

            mouseX = 0;
            mouseY = 0;
            // Locked state automatically hides the cursor, so no need to set visibility
            CursorState.LockState = CursorLockMode.Locked;
        }
        else if (!lockMouse && ReferenceEquals(cursorLockOwner, this))
        {
            // The native lock stays authoritative: if Unity already released it externally, clear this
            // owner's claim without issuing a warp to a stale saved position.
            cursorLockOwner = null;
            if (mouseLocked)
            {
                CursorState.LockState = CursorLockMode.None;
                CursorState.Warp(savedMousePos);
            }
        }
    }

    private bool forwardHeld;
    private bool backwardHeld;
    private bool leftHeld;
    private bool rightHeld;
    private bool elevateHeld;
    private bool lowerHeld;

    private void HandleCameraHeldMovementKeys()
    {
        x = 0f;
        if (leftHeld) x -= 1f;
        if (rightHeld) x += 1f;

        y = 0f;
        if (elevateHeld) y += 1f;
        if (lowerHeld) y -= 1f;

        z = 0f;
        if (forwardHeld) z += 1f;
        if (backwardHeld) z -= 1f;
    }

    //Oh boy new Unity Input System POGCHAMP
    public void OnMoveCamera(CallbackContext context)
    {
        //Take our movement vector and manipulate it to work how we want.
        //Our X component (A and D) should move us left/right (X)
        //Our Y component (W and S) should move us forward/backward (Z)
        var movement = context.ReadValue<Vector2>();
        x = movement.x;
        z = movement.y;
    }

    // God I hate this
    public void OnElevateCamera(CallbackContext context) => elevateHeld = context.performed;
    public void OnLowerCamera(CallbackContext context) => lowerHeld = context.performed;
    public void OnMoveCameraLeft(CallbackContext context) => leftHeld = context.performed;
    public void OnMoveCameraRight(CallbackContext context) => rightHeld = context.performed;
    public void OnMoveCameraForward(CallbackContext context) => forwardHeld = context.performed;
    public void OnMoveCameraBackward(CallbackContext context) => backwardHeld = context.performed;

    public void OnRotateCamera(CallbackContext context)
    {
        if (!canMoveCamera) return;

        if (ignoreInitialMouseMovement)
        {
            framesAfterRightClick++;

            // Ignore the first 3 frames
            if (framesAfterRightClick <= 3)
            {
                mouseX = 0;
                mouseY = 0;
                return;
            }

            // After 8 frames, return to normal mouse handling
            if (framesAfterRightClick > 8)
            {
                ignoreInitialMouseMovement = false;
            }
            else
            {
                // Between frames 4-8, apply limit mouse movement
                var delta = context.ReadValue<Vector2>();
                delta.x = Mathf.Clamp(delta.x, -0.1f, 0.1f);
                delta.y = Mathf.Clamp(delta.y, -0.1f, 0.1f);

                mouseX = delta.x * mouseSensitivity / 10f;
                mouseY = delta.y * mouseSensitivity / 10f;
                return;
            }
        }

        var deltaMouseMovement = context.ReadValue<Vector2>();
        mouseX = deltaMouseMovement.x * mouseSensitivity / 10f;
        mouseY = deltaMouseMovement.y * mouseSensitivity / 10f;
    }

    public void OnHoldtoMoveCamera(CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;

        if (context.performed && !canMoveCamera)
        {
            mouseX = 0;
            mouseY = 0;
            ignoreInitialMouseMovement = true;
            framesAfterRightClick = 0;
        }

        canMoveCamera = context.performed;
        if (canMoveCamera)
            CMInputCallbackInstaller.DisableActionMaps(typeof(CameraController), actionMapsDisabledWhileMoving);
        else if (context.canceled)
            CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(CameraController), actionMapsDisabledWhileMoving);
    }

    public void OnAttachtoNoteGrid(CallbackContext context)
    {
        if (context.performed
            && noteGridTransform.gameObject.activeInHierarchy
            && !playerCamera)
            LockedOntoNoteGrid = !LockedOntoNoteGrid;
    }

    public void OnToggleFullscreen(CallbackContext context)
    {
        if (!Application.isEditor && context.performed) Screen.fullScreen = !Screen.fullScreen;
    }

    public void OnLocation1(CallbackContext context) => OnLocation(0);

    public void OnLocation2(CallbackContext context) => OnLocation(1);

    public void OnLocation3(CallbackContext context) => OnLocation(2);

    public void OnLocation4(CallbackContext context) => OnLocation(3);

    // DisabledPlayingCameraDoesNotBreakEditingCameraCursorLock: the static editing-camera owner must be
    // re-registered on every enable, not only Start, so re-enabling after a disable restores it.
    private void OnEnable()
    {
        if (!playerCamera) instance = this;
    }

    private void OnDisable()
    {
        // DisablingPlayingCursorOwnerReleasesOnlyItsOwnLock: disabled owners stop receiving callbacks,
        // so release their lock here without allowing an unrelated camera to release it.
        SetLockState(false);
        Settings.ClearSettingNotifications(nameof(Settings.CameraAA));
        Settings.ClearSettingNotifications(nameof(Settings.RenderScale));
        Settings.ClearSettingNotifications(nameof(Settings.PlayerCameraOffsetZ));
        // DisabledPlayingCameraDoesNotBreakEditingCameraCursorLock: only the editing controller that owns
        // `instance` may clear it; disabling the playing camera must not orphan the editing camera.
        if (ReferenceEquals(instance, this)) instance = null;
    }

    public void OnSecondSetModifier(CallbackContext context) => secondSetOfLocations = context.performed;

    public void OnOverwriteLocationModifier(CallbackContext context) => setLocation = context.performed;

    public static void ClearCameraMovement()
    {
        if (instance is null) return;
        instance.x = instance.y = instance.z = instance.mouseX = instance.mouseY = 0;
    }

    private void OnLocation(int id)
    {
        if (playerCamera) return;
        // Shift for second set of hotkeys (8 total)
        if (secondSetOfLocations) id += 4;

        if (setLocation)
        {
            Settings.Instance.SavedPositions[id] = new CameraPosition(transform.position, transform.rotation);
        }
        else if (Settings.Instance.SavedPositions[id] != null)
        {
            transform.SetPositionAndRotation(
                Settings.Instance.SavedPositions[id].Position,
                Settings.Instance.SavedPositions[id].Rotation);
        }
    }

    private void HandleRotationChanged(float rotation) =>
        cameraAnimator.LocalTarget.localEulerAngles = new Vector3(0, rotation, 0);

    private void DisconnectPlayerTrack()
    {
        if (currentTrack == null) return;

        currentTrack.Children.Remove(cameraAnimator);
        currentTrack.OnChildrenChanged();
        currentTrack = null;

        cameraAnimator.ResetData();
        cameraAnimator.enabled = false;

        // The rig transform was reparented under the track's ObjectParentTransform on bind and nothing
        // else moves it back, so without this restore it stays under the (possibly already
        // scene-unloaded) track at its last animated pose — SaltyBeat537HeadCameraParityTest.
        if (cameraHomeCaptured)
        {
            var rigTransform = cameraAnimator.transform;
            rigTransform.SetParent(cameraHomeParent);
            rigTransform.localPosition = cameraHomeLocalPosition;
            rigTransform.localRotation = cameraHomeLocalRotation;
            rigTransform.localScale = cameraHomeLocalScale;
        }
    }
}
