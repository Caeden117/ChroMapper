using Beatmap.Base;
using NUnit.Framework;

namespace Tests.Editor
{
    public class StateChunksContainerPausedLookupTest
    {
        [Test]
        public void PausedLookupImmediatelyBeforeBoundaryKeepsPreviousState()
        {
            var container = CreateTwoStateContainer(out var previous, out _);

            var retainedCurrentState = container.IsCurrentOrFindState(0.9995f, false);

            Assert.That(retainedCurrentState, Is.True);
            Assert.That(container.CurrentState, Is.SameAs(previous));
        }

        [Test]
        public void PausedLookupAtBoundarySelectsFollowingState()
        {
            var container = CreateTwoStateContainer(out _, out var following);

            var retainedCurrentState = container.IsCurrentOrFindState(1f, false);

            Assert.That(retainedCurrentState, Is.False);
            Assert.That(container.CurrentState, Is.SameAs(following));
        }

        private static BasicEventStateChunksContainer<BasicEventStateData> CreateTwoStateContainer(
            out BasicEventStateData previous,
            out BasicEventStateData following)
        {
            previous = new BasicEventStateData(new BaseEvent())
            {
                StartTime = 0f,
                EndTime = 1f
            };
            following = new BasicEventStateData(new BaseEvent())
            {
                StartTime = 1f,
                EndTime = float.MaxValue
            };

            var container = new BasicEventStateChunksContainer<BasicEventStateData>();
            container.Resize(10f);
            container.AddState(previous);
            container.AddState(following);
            container.SetStateAt(0f);
            return container;
        }
    }
}
