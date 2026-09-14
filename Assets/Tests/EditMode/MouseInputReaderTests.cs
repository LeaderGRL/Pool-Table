using NUnit.Framework;
using PoolTable.Input;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    public sealed class MouseInputReaderTests
    {
        [Test]
        public void Read_ReturnsSnapshotFromInputSource()
        {
            var reader = new MouseInputReader(
                () => new Vector2(12.5f, -7.25f),
                () => true);

            var snapshot = reader.Read();

            Assert.That(snapshot.Delta, Is.EqualTo(new Vector2(12.5f, -7.25f)));
            Assert.That(snapshot.PrimaryButtonIsPressed, Is.True);
        }

        [Test]
        public void Constructor_RejectsMissingInputSource()
        {
            Assert.That(
                () => new MouseInputReader(null, () => false),
                Throws.ArgumentNullException);
            Assert.That(
                () => new MouseInputReader(() => Vector2.zero, null),
                Throws.ArgumentNullException);
        }
    }
}
