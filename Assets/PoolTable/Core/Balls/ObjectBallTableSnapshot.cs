using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PoolTable.Core.Balls
{
    public sealed class ObjectBallTableSnapshot
    {
        private readonly ReadOnlyCollection<BallId> balls;
        private readonly HashSet<int> ballNumbers;

        public ObjectBallTableSnapshot(IEnumerable<BallId> ballsOnTable)
        {
            if (ballsOnTable == null)
            {
                throw new ArgumentNullException(nameof(ballsOnTable));
            }

            var copy = new List<BallId>();
            ballNumbers = new HashSet<int>();

            foreach (var ball in ballsOnTable)
            {
                if (ball.IsCueBall)
                {
                    throw new ArgumentException("The object-ball table snapshot cannot contain the cue ball.", nameof(ballsOnTable));
                }

                if (!ballNumbers.Add(ball.Number))
                {
                    throw new ArgumentException($"Ball {ball.Number} cannot appear more than once in the table snapshot.", nameof(ballsOnTable));
                }

                copy.Add(ball);
            }

            balls = copy.AsReadOnly();
        }

        public IReadOnlyList<BallId> Balls => balls;

        public bool Contains(BallId ball) => ballNumbers.Contains(ball.Number);

        public bool HasRemainingBalls(BallGroup group)
        {
            if (group != BallGroup.Solids && group != BallGroup.Stripes)
            {
                throw new ArgumentException("Remaining-ball queries require the solids or stripes group.", nameof(group));
            }

            for (var index = 0; index < balls.Count; index++)
            {
                if (balls[index].Group == group)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
