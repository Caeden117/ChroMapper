using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Regression for bombs visually lagging one scroll behind on alt+shift angle hover scroll.
    // CreateContainerFromPool runs UpdateGridPosition (which applies the cached DirectionTargetEuler
    // to DirectionTarget.localEulerAngles) before UpdateContainerData computes the fresh value, and
    // RefreshSpecialAngles — which re-applies the transform for real notes — early-returns for bombs.
    public class BombAngleScrollTest : TestBase
    {
        [UnityTest]
        public IEnumerator BombAngleScrollAppliesDirectionImmediately()
        {
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var bomb = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Bomb,
                PosX = 1,
                PosY = 1,
                CutDirection = (int)NoteCutDirection.Any
            });

            // First hover scroll: the respawned container's direction must reflect the new angle,
            // not keep the previously-rendered one.
            var edited = NoteCommand.SetAngleOffset(bomb, 45);
            var container = collection.LoadedContainers[edited] as NoteContainer;
            Assert.IsNotNull(container, "Bomb container missing after first angle scroll");
            Assert.AreEqual(
                NoteContainer.Directionalize(edited).z,
                container.DirectionTarget.localEulerAngles.z,
                0.001f,
                "Bomb direction must reflect the first scrolled angle");

            // Second scroll: must reflect the latest angle, not lag at the previous one.
            var edited2 = NoteCommand.SetAngleOffset(edited, 90);
            var container2 = collection.LoadedContainers[edited2] as NoteContainer;
            Assert.IsNotNull(container2, "Bomb container missing after second angle scroll");
            Assert.AreEqual(
                NoteContainer.Directionalize(edited2).z,
                container2.DirectionTarget.localEulerAngles.z,
                0.001f,
                "Bomb direction must reflect the latest scrolled angle, not the previous one");

            yield return null;
        }
    }
}
