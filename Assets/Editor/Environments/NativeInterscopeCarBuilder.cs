using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class NativeInterscopeCarBuilder
{
    public static void Build(CreateContainer container)
    {
        var descriptor = container.Descriptor;
        var jumps = descriptor.GetComponentsInChildren<HydraulicCarJump>(true);
        if (jumps.Length == 0)
            return;

        var templateObject = new GameObject("Car Physics Template");
        templateObject.SetActive(false);
        templateObject.transform.SetParent(descriptor.transform, false);
        var template = templateObject.AddComponent<HydraulicCarPhysicsRig>();
        var bodies = new List<Rigidbody>();
        var sourceBodies = new List<Rigidbody>();
        var joints = new List<SpringJoint>();
        var cars = new List<HydraulicCarTimeline.Car>();
        var startup = container.ComponentInstances.Values.OfType<RigidbodyData>()
            .ToDictionary(body => body.GetComponent(), body => body.StartupState);
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();
        var velocities = new List<Vector3>();
        var angularVelocities = new List<Vector3>();

        foreach (var jump in jumps)
        {
            var suspension = jump.GetComponent<HydraulicCarSuspension>();
            var carRoot = new GameObject(jump.name).transform;
            carRoot.SetParent(template.transform, false);
            carRoot.SetPositionAndRotation(jump.transform.position, jump.transform.rotation);
            carRoot.localScale = jump.transform.lossyScale;
            var transforms = new Dictionary<Transform, Transform> { [jump.transform] = carRoot };
            var carBodies = jump.GetComponentsInChildren<Rigidbody>(true);
            var firstBody = bodies.Count;
            foreach (var source in carBodies)
            {
                var target = CloneTransform(source.transform).gameObject.AddComponent<Rigidbody>();
                EditorUtility.CopySerialized(source, target);
                bodies.Add(target);
                sourceBodies.Add(source);
                var state = startup[source];
                positions.Add(state.Position);
                rotations.Add(new Quaternion(state.Rotation.x, state.Rotation.y, state.Rotation.z, state.Rotation.w));
                velocities.Add(state.LinearVelocity);
                angularVelocities.Add(state.AngularVelocity);
            }

            var sourceJoints = jump.GetComponentsInChildren<SpringJoint>(true);
            var suspensionIndex = -1;
            foreach (var source in sourceJoints)
            {
                var target = transforms[source.transform].gameObject.AddComponent<SpringJoint>();
                EditorUtility.CopySerialized(source, target);
                target.autoConfigureConnectedAnchor = false;
                target.connectedBody = bodies[firstBody + Array.IndexOf(carBodies, source.connectedBody)];
                target.connectedAnchor = source.connectedAnchor;
                if (source == suspension.SpringJoint)
                    suspensionIndex = joints.Count;

                joints.Add(target);
            }

            foreach (var source in jump.GetComponentsInChildren<Collider>(true))
            {
                var target = (Collider)CloneTransform(source.transform).gameObject.AddComponent(source.GetType());
                EditorUtility.CopySerialized(source, target);
                // These native floor and wheel colliders collide with each other. CM's layer names differ.
                target.gameObject.layer = 0;
            }

            cars.Add(new HydraulicCarTimeline.Car
            {
                Body = firstBody + Array.IndexOf(carBodies, jump.Rigidbody),
                Joint = suspensionIndex,
                Jump = jump,
                Suspension = suspension,
                ForcePosition = jump.transform.TransformPoint(jump.Position)
            });

            Transform CloneTransform(Transform source)
            {
                if (transforms.TryGetValue(source, out var existing))
                    return existing;

                var target = new GameObject(source.name).transform;
                target.SetParent(CloneTransform(source.parent), false);
                target.localPosition = source.localPosition;
                target.localRotation = source.localRotation;
                target.localScale = source.localScale;
                transforms.Add(source, target);
                return target;
            }

        }

        template.Bodies = bodies.ToArray();
        template.Joints = joints.ToArray();
        template.StartupPositions = positions.ToArray();
        template.StartupRotations = rotations.ToArray();
        template.StartupVelocities = velocities.ToArray();
        template.StartupAngularVelocities = angularVelocities.ToArray();
        template.Gravity = container.Data.Data.Physics.Gravity;
        template.FixedDeltaTime = container.Data.Data.Physics.FixedDeltaTime;
        var manager = descriptor.BasicEventEffectManager;
        var timeline = manager.gameObject.AddComponent<HydraulicCarTimeline>();
        timeline.Template = template;
        timeline.VisualBodies = sourceBodies.ToArray();
        timeline.Cars = cars.ToArray();
        foreach (var type in cars.SelectMany(car => new[]
                 {
                     car.Jump.EventType, car.Suspension.ContractEventType, car.Suspension.ExpandEventType
                 }).Distinct())
        {
            manager.Register(type, timeline);
        }

        foreach (var body in sourceBodies)
        {
            body.isKinematic = true;
            body.detectCollisions = false;
            body.interpolation = RigidbodyInterpolation.None;
        }

    }

}
