using PoolTable.Core.Balls;
using UnityEngine;

namespace PoolTable.Gameplay.Balls
{
    public sealed class BallIdentity : MonoBehaviour
    {
        [SerializeField, Range(BallId.MinimumNumber, BallId.MaximumNumber)]
        private int ballNumber;

        public BallId Id => new BallId(ballNumber);

        public BallGroup Group => Id.Group;

        public bool IsCueBall => Id.IsCueBall;

        public bool IsEightBall => Id.IsEightBall;
    }
}