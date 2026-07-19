using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tentacle
{
    [Serializable]
    public class TentacleSettings
    {
        [SerializeField] private Transform[] _bones;
        [SerializeField] private float _grabRadius = 6f;
        [SerializeField] private string _attackStateName = "AttackC_Wall01";
        [SerializeField] private string _idleStateName = "IdleA";
        [SerializeField] private float _attackCrossFadeDuration = 0.18f;
        [SerializeField] private float _attackWindupDuration = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _attackTakeoverNormalizedTime = 0.32f;
        [SerializeField] private float _attackTakeoverTimeout = 0.8f;
        [SerializeField] private float _grabDuration = 0.7f;
        [SerializeField] private float _arcHeight = 1.5f;
        [SerializeField] private float _arcForwardOffset = 2f;
        [SerializeField] private float _arcSideOffset = 0.75f;
        [SerializeField, Range(0.25f, 0.9f)] private float _reachPhasePortion = 0.55f;
        [SerializeField] private float _recoverToIdleDuration = 0.35f;
        [FormerlySerializedAs("_liftLogic")]
        [SerializeField] private TentacleLiftSettings _liftSettings = new TentacleLiftSettings();

        public Transform[] Bones => _bones;
        public float GrabRadius => _grabRadius;
        public string AttackStateName => _attackStateName;
        public string IdleStateName => _idleStateName;
        public float AttackCrossFadeDuration => _attackCrossFadeDuration;
        public float AttackWindupDuration => _attackWindupDuration;
        public float AttackTakeoverNormalizedTime => _attackTakeoverNormalizedTime;
        public float AttackTakeoverTimeout => _attackTakeoverTimeout;
        public float GrabDuration => _grabDuration;
        public float ArcHeight => _arcHeight;
        public float ArcForwardOffset => _arcForwardOffset;
        public float ArcSideOffset => _arcSideOffset;
        public float ReachPhasePortion => _reachPhasePortion;
        public float RecoverToIdleDuration => _recoverToIdleDuration;
        public TentacleLiftSettings Lift => _liftSettings;
    }

    [Serializable]
    public class TentacleLiftSettings
    {
        [SerializeField] private float _liftDuration = 0.9f;
        [SerializeField] private float _liftHeight = 4.25f;
        [SerializeField] private float _liftForwardOffset = 0.35f;
        [SerializeField] private float _liftArcHeight = 1.25f;
        [SerializeField] private float _tipRadius = 0.48f;
        [SerializeField] private float _tipSurfaceOffset = 0.12f;
        [SerializeField] private float _tipMaxGripRadius = 0.68f;
        [SerializeField, Range(0.75f, 1f)] private float _tipGripCompression = 0.88f;
        [SerializeField] private float _tipVerticalInset = 0.16f;
        [SerializeField] private float _tipWrapTurns = 1.65f;
        [SerializeField] private float _tipGripVerticalOffset = 1.05f;
        [SerializeField] private float _tipGripPitch = 0.18f;
        [SerializeField, Range(0.35f, 0.9f)] private float _bodyBonePortion = 0.46f;
        [SerializeField] private float _topShakeDuration = 3f;
        [SerializeField] private float _topShakeFrequency = 1.35f;
        [SerializeField] private float _topShakeSideAmplitude = 0.22f;
        [SerializeField] private float _topShakeForwardAmplitude = 0.1f;
        [SerializeField] private float _topShakeHeightAmplitude = 0.06f;
        [SerializeField] private float _topShakeRotationAngle = 5f;
        [SerializeField] private float _throwWindupDuration = 0.55f;
        [SerializeField] private float _throwReleaseDuration = 0.35f;
        [SerializeField] private float _throwDrawBackDistance = 1.15f;
        [SerializeField] private float _throwExtensionDistance = 0.9f;
        [SerializeField, Range(0f, 0.8f)] private float _throwUnwrapStart = 0.12f;
        [SerializeField] private float _throwUnwrapExpansion = 0.65f;
        [SerializeField] private float _throwUnwrapTrailDistance = 1.35f;
        [SerializeField] private float _throwFollowThroughDuration = 0.22f;
        [SerializeField] private float _throwFollowThroughDistance = 0.75f;
        [SerializeField] private float _throwForwardSpeed = 8.5f;
        [SerializeField] private float _throwUpSpeed = 2.5f;

        public float LiftDuration => _liftDuration;
        public float LiftHeight => _liftHeight;
        public float LiftForwardOffset => _liftForwardOffset;
        public float LiftArcHeight => _liftArcHeight;
        public float TipRadius => _tipRadius;
        public float TipSurfaceOffset => _tipSurfaceOffset;
        public float TipMaxGripRadius => _tipMaxGripRadius;
        public float TipGripCompression => _tipGripCompression;
        public float TipVerticalInset => _tipVerticalInset;
        public float TipWrapTurns => _tipWrapTurns;
        public float TipGripVerticalOffset => _tipGripVerticalOffset;
        public float TipGripPitch => _tipGripPitch;
        public float BodyBonePortion => _bodyBonePortion;
        public float TopShakeDuration => _topShakeDuration;
        public float TopShakeFrequency => _topShakeFrequency;
        public float TopShakeSideAmplitude => _topShakeSideAmplitude;
        public float TopShakeForwardAmplitude => _topShakeForwardAmplitude;
        public float TopShakeHeightAmplitude => _topShakeHeightAmplitude;
        public float TopShakeRotationAngle => _topShakeRotationAngle;
        public float ThrowWindupDuration => _throwWindupDuration;
        public float ThrowReleaseDuration => _throwReleaseDuration;
        public float ThrowDrawBackDistance => _throwDrawBackDistance;
        public float ThrowExtensionDistance => _throwExtensionDistance;
        public float ThrowUnwrapStart => _throwUnwrapStart;
        public float ThrowUnwrapExpansion => _throwUnwrapExpansion;
        public float ThrowUnwrapTrailDistance => _throwUnwrapTrailDistance;
        public float ThrowFollowThroughDuration => _throwFollowThroughDuration;
        public float ThrowFollowThroughDistance => _throwFollowThroughDistance;
        public float ThrowForwardSpeed => _throwForwardSpeed;
        public float ThrowUpSpeed => _throwUpSpeed;
    }
}
