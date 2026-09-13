using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PoolTable.Core.Balls;

namespace PoolTable.Core.Shots
{
    public sealed class ShotFacts
    {
        private readonly ReadOnlyCollection<BallId> pocketedBalls;
        private readonly ReadOnlyCollection<PocketedBall> pocketedBallEvents;
        private readonly ReadOnlyCollection<BallId> railContactBallsAfterFirstObjectBallContact;

        public ShotFacts(
            BallId? firstObjectBallContact,
            IEnumerable<PocketedBall> pocketedBallEvents,
            IEnumerable<BallId> railContactBallsAfterFirstObjectBallContact)
        {
            if (firstObjectBallContact.HasValue && firstObjectBallContact.Value.IsCueBall)
            {
                throw new ArgumentException("The cue ball cannot be the first object-ball contact.", nameof(firstObjectBallContact));
            }

            FirstObjectBallContact = firstObjectBallContact;
            this.pocketedBallEvents = CopyPocketedBalls(pocketedBallEvents);
            pocketedBalls = CopyPocketedBallIds(this.pocketedBallEvents);
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

        public IReadOnlyList<PocketedBall> PocketedBallEvents => pocketedBallEvents;

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

        public bool WasPocketedIn(BallId ball, PocketId pocket)
        {
            if (!pocket.IsValid)
            {
                throw new ArgumentException("Pocket lookup requires a valid table pocket.", nameof(pocket));
            }

            for (var index = 0; index < pocketedBallEvents.Count; index++)
            {
                var pocketedBall = pocketedBallEvents[index];
                if (pocketedBall.Ball == ball && pocketedBall.Pocket == pocket)
                {
                    return true;
                }
            }

            return false;
        }

        private static ReadOnlyCollection<PocketedBall> CopyPocketedBalls(IEnumerable<PocketedBall> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var result = new List<PocketedBall>();
            var seenNumbers = new HashSet<int>();

            foreach (var pocketedBall in source)
            {
                if (!pocketedBall.IsValid)
                {
                    throw new ArgumentException("Pocketed-ball observations must use valid table pockets.", nameof(source));
                }

                if (!seenNumbers.Add(pocketedBall.Ball.Number))
                {
                    throw new ArgumentException(
                        $"Ball {pocketedBall.Ball.Number} cannot be pocketed more than once in one shot.",
                        nameof(source));
                }

                result.Add(pocketedBall);
            }

            return result.AsReadOnly();
        }

        private static ReadOnlyCollection<BallId> CopyPocketedBallIds(IReadOnlyList<PocketedBall> source)
        {
            var result = new List<BallId>(source.Count);

            for (var index = 0; index < source.Count; index++)
            {
                result.Add(source[index].Ball);
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
