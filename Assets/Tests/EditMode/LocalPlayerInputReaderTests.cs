using NUnit.Framework;
using PoolTable.Input;
using UnityEngine;

namespace PoolTable.Tests.EditMode
{
    [Category("Input")]
    public sealed class LocalPlayerInputReaderTests
    {
        [Test]
        public void Read_ReturnsSnapshotFromInputSource()
        {
            var reader = new LocalPlayerInputReader(
                () => new Vector2(12.5f, -7.25f),
                () => true,
                () => true,
                () => Vector2.zero,
                () => Vector2.zero,
                () => false,
                () => false);

            var snapshot = reader.Read();

            Assert.That(snapshot.PointerDelta, Is.EqualTo(new Vector2(12.5f, -7.25f)));
            Assert.That(snapshot.PrimaryActionIsPressed, Is.True);
            Assert.That(snapshot.SecondaryActionIsPressed, Is.True);
        }

        [Test]
        public void Constructor_RejectsMissingInputSource()
        {
            Assert.That(
                () => new LocalPlayerInputReader(null, () => false, () => false, () => Vector2.zero, () => Vector2.zero, () => false, () => false),
                Throws.ArgumentNullException);
            Assert.That(
                () => new LocalPlayerInputReader(() => Vector2.zero, null, () => false, () => Vector2.zero, () => Vector2.zero, () => false, () => false),
                Throws.ArgumentNullException);
            Assert.That(
                () => new LocalPlayerInputReader(() => Vector2.zero, () => false, null, () => Vector2.zero, () => Vector2.zero, () => false, () => false),
                Throws.ArgumentNullException);
        }
    }
}
