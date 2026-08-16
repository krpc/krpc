using KRPC.Service;
using KRPC.Utils;
using NUnit.Framework;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class GameStateTest
    {
        [Test]
        public void GenerationMovesOn ()
        {
            var generation = GameState.Generation;
            GameState.Changed ();
            Assert.AreEqual (generation + 1, GameState.Generation);
            GameState.Changed ();
            Assert.AreEqual (generation + 2, GameState.Generation);
        }

        [Test]
        public void AChangeLeavesASweepPending ()
        {
            GameState.Changed ();
            Assert.IsTrue (GameState.SweepPending);
            GameState.Sweep ();
            Assert.IsFalse (GameState.SweepPending);
        }

        [Test]
        public void ARequestedSweepLeavesTheGenerationAlone ()
        {
            var generation = GameState.Generation;
            GameState.RequestSweep ();
            Assert.IsTrue (GameState.SweepPending);
            Assert.AreEqual (generation, GameState.Generation);
            GameState.Sweep ();
            Assert.IsFalse (GameState.SweepPending);
        }

        [Test]
        public void SweepsTheObjectStore ()
        {
            var store = ObjectStore.Instance;
            var alive = new MortalObject ();
            var dead = new MortalObject ();
            var aliveId = store.AddInstance (alive);
            var deadId = store.AddInstance (dead);
            dead.GameObjectState = GameObjectState.Destroyed;

            GameState.Sweep ();

            Assert.AreSame (alive, store.GetInstance (aliveId));
            Assert.Throws<ObjectDestroyedException> (() => store.GetInstance (deadId));
            store.RemoveInstance (alive);
        }
    }
}
