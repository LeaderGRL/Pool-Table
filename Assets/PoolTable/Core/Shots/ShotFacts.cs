using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PoolTable.Core.Balls;

namespace PoolTable.Core.Shots
{
    public sealed class ShotFacts
    {
        private readonly ReadOnlyCollection<BallId> pocketedBalls;
        private readonly ReadOnlyCollection<BallId> railContactBallsAfterFirstObjectBallContact;

        public ShotFacts(
            BallId? firstObjectBallContact,
            IEnumerable<BallId> pocketedBalls,
            IEnumerable<BallId> railContactBallsAfterFirstObjectBallContact)
        {
            if (firstObjectBallContact.HasValue && firstObjectBallContact.Value.IsCueBall)
            {
                throw new ArgumentException("The cue ball cannot be the first object-ball contact.", nameof(firstObjectBallContact));
            }

            FirstObjectBallContact = firstObjectBallContact;
            this.pocketedBalls = CopyPocketedBalls(pocketedBalls);
            this.railContactBallsAfterFirstObjectBallContact = CopyDistinctBalls(
                railContactBallsAfterFirstObjectBallContact,
                nameof(railContactBallsAfterFirstObjectBallContact));

            if (!firstObjectBallContact.HasValue && this.railContactBallsAfterFirstObjectBallContact.Count > 0)
            {
                throw new ArgumentException(
                    "Rail contacts after first object-ball contact require a first object-ball contact.",
                    nameof(railContactBallsAfterFirstObjectBallContact));
            }
        }

        public BallId? FirstObjectBallContact { get; }

        public bool HasObjectBallContact => FirstObjectBallContact.HasValue;

        public IReadOnlyList<BallId> PocketedBalls => pocketedBalls;

        public IReadOnlyList<BallId> RailContactBallsAfterFirstObjectBallContact => railContactBallsAfterFirstObjectBallContact;

        public bool CueBallPocketed
        {
            get
            {
                for (var index = 0; index < pocketedBalls.Count; index++)
                {
                    if (pocketedBalls[index].IsCueBall)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static ReadOnlyCollection<BallId> CopyPocketedBalls(IEnumerable<BallId> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var result = new List<BallId>();
            var seenNumbers = new HashSet<int>();

            foreach (var ball in source)
            {
                if (!seenNumbers.Add(ball.Number))
                {
                    throw new ArgumentException($"Ball {ball.Number} cannot be pocketed more than once in one shot.", nameof(source));
                }

                result.Add(ball);
            }

            return result.AsReadOnly();
        }

        private static ReadOnlyCollection<BallId> CopyDistinctBalls(IEnumerable<BallId> source, string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var result = new List<BallId>();
            var seenNumbers = new HashSet<int>();

            foreach (var ball in source)
            {
                if (seenNumbers.Add(ball.Number))
                {
                    result.Add(ball);
                }
            }

            return result.AsReadOnly();
        }
    }
}
