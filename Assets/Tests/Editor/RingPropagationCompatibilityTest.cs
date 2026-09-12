using Beatmap.Base;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;

namespace Tests.Editor
{
    public class RingPropagationCompatibilityTest
    {
        [Test]
        public void OldPropagationDeclarationSuggestsBeatToTheFutureAndPreservesDependency()
        {
            var difficulty = new BaseDifficulty();
            var infoDifficulty = new InfoDifficulty(new InfoDifficultySet());
            var requirement = new BeatToTheFutureReq();

            // The compatibility flag needs BeatToTheFuture at runtime, but automatic metadata must not downgrade an explicit dependency.
            difficulty.CustomData[RingPropagationCompatibility.MappedForOldPropagationKey] = true;
            Assert.AreEqual(
                RequirementCheck.RequirementType.Suggestion,
                requirement.IsRequiredOrSuggested(infoDifficulty, difficulty));

            infoDifficulty.CustomRequirements.Add("BeatToTheFuture");
            Assert.AreEqual(
                RequirementCheck.RequirementType.Requirement,
                requirement.IsRequiredOrSuggested(infoDifficulty, difficulty));
        }

        [Test]
        public void OldPropagationDeclarationAtInfoRootSuggestsBeatToTheFuture()
        {
            var difficulty = new BaseDifficulty
            {
                RuntimeLevelCustomData = new JSONObject
                {
                    [RingPropagationCompatibility.MappedForOldPropagationKey] = true
                }
            };
            var infoDifficulty = new InfoDifficulty(new InfoDifficultySet());

            // BeatToTheFuture reads level custom data in addition to selected-difficulty and beatmap-file metadata, so ChroMapper must advertise that scope too.
            Assert.AreEqual(
                RequirementCheck.RequirementType.Suggestion,
                new BeatToTheFutureReq().IsRequiredOrSuggested(infoDifficulty, difficulty));
        }
    }
}
