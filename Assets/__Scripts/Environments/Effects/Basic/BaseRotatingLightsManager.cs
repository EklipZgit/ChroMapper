using System;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public class BaseRotatingLightsManager : BaseRotatingLightsEffect
{
    [SerializeField] public float Multiplier = 20;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float zPositionModifier;

    public bool UseZPositionForAngleOffset;
    private readonly Vector3 rotationVector = Vector3.up;

    private float songSpeed = 1;

    private float speed;
    private Quaternion startRotation;
    private float zPositionOffset;

    private void Start()
    {
        startRotation = transform.localRotation;
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
    }

    private void Update() => transform.Rotate(rotationVector, Time.deltaTime * rotationSpeed * songSpeed, Space.Self);

    private void OnDestroy() => Settings.ClearSettingNotifications("SongSpeed");

    private void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10;
    }

    // If you have any complaints about CM's inaccurate lasers, please look through this and tell me what the hell is wrong.
    public override void UpdateOffset(BaseEvent data, bool mirror, bool isLeftEvent)
    {
        var rotation = UnityEngine.Random.Range(0f, 180f);
        var rotateForwards = UnityEngine.Random.Range(0, 1) == 1;
        if (mirror)
        {
            rotation = -rotation;
            rotateForwards = !rotateForwards;
        }

        this.speed = data.Value;
        var lockRotation = false;
        if (data.CustomData != null) //We have custom data in this event
        {
            //Apply some chroma precision values

            if (data.CustomLockRotation.HasValue) lockRotation = data.CustomLockRotation.Value;

            if (speed > 0)
            {
                if (data.CustomPreciseSpeed.HasValue)
                    this.speed = data.CustomPreciseSpeed.Value;
                else if (data.CustomSpeed.HasValue) this.speed = data.CustomSpeed.Value;
            }

            if (data.CustomDirection.HasValue)
            {
                rotateForwards = mirror
                    ? data.CustomDirection.Value.Equals(0) ^ !isLeftEvent
                    : data.CustomDirection.Value.Equals(0) ^ isLeftEvent;
            }
        }

        if (!lockRotation) //If we are not locking rotation, reset it to its default.
            transform.localRotation = startRotation;
        if (UseZPositionForAngleOffset
            && !lockRotation) //BTS, FitBeat, and Timbaland has laser speeds offset by their Z position
        {
            rotation = Time.frameCount + (transform.position.z * zPositionModifier);
        }

        //Rotate by Rotation variable
        //In most cases, it is randomized, except in certain environments (see above)
        if (!lockRotation && (this.speed > 0 || data.CustomPreciseSpeed.HasValue && data.CustomPreciseSpeed.Value >= 0))
        {
            transform.Rotate(rotationVector, rotation, Space.Self);
        }

        rotationSpeed =
            this.speed * Multiplier * (rotateForwards ? -1 : 1) * Mathf.Sign(Multiplier); //Set rotation speed
    }
}
