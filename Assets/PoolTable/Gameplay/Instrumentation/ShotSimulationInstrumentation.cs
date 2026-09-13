using System;
using System.Linq;
using PoolTable.Gameplay.Balls;
using PoolTable.Physics.Instrumentation;
using UnityEngine;

namespace PoolTable.Gameplay.Instrumentation
{
    [DisallowMultipleComponent]
    public sealed class ShotSimulationInstrumentation : MonoBehaviour
    {
        private ShotSimulationRecorder _recorder;

        public bool IsRecording => _recorder != null && _recorder.IsRecording;

        public int RegisteredBallCount => _recorder?.RegisteredBallCount ?? 0;

        public ShotSimulationReport LastReport { get; private set; }

        private void Awake()
        {
            _recorder = new ShotSimulationRecorder();
            var identities = GetComponentsInChildren<BallIdentity>(true)
                .OrderBy(identity => identity.Id.Number)
                .ToArray();

            if (identities.Length == 0)
            {
                throw new InvalidOperationException("Shot simulation instrumentation requires typed billiard balls below its scene root.");
            }

            foreach (var identity in identities)
            {
                if (!identity.TryGetComponent<RigidbodySimulationProbe>(out var probe))
                {
                    throw new InvalidOperationException($"Ball {identity.Id} requires a RigidbodySimulationProbe for shot instrumentation.");
                }

                _recorder.Register(identity.Id, probe);
            }
        }

        private void OnDisable()
        {
            if (_recorder != null && _recorder.IsRecording)
            {
                _recorder.Cancel(Time.fixedTimeAsDouble);
            }
        }

        public void BeginShot()
        {
            if (_recorder == null)
            {
                throw new InvalidOperationException("Shot simulation instrumentation has not been initialized.");
            }

            LastReport = null;
            _recorder.Begin(Time.fixedTimeAsDouble);
        }

        public ShotSimulationReport CompleteShot()
        {
            if (_recorder == null)
            {
                throw new InvalidOperationException("Shot simulation instrumentation has not been initialized.");
            }

            LastReport = _recorder.Complete(Time.fixedTimeAsDouble);
            return LastReport;
        }
    }
}
