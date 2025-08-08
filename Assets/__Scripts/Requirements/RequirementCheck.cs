using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Info;

/**
 * The intention here is to create a simple interface that a CM plugin can implement
 * in order to check if a mod requirement or suggestion should be added
 */
public abstract class RequirementCheck
{
    public enum RequirementType
    {
        Requirement, Suggestion, None
    }

    internal static readonly HashSet<RequirementCheck> requirementsAndSuggestions = new HashSet<RequirementCheck>();

    public abstract string Name { get; }

    internal static void Setup()
    {
        requirementsAndSuggestions.Clear();
        RegisterRequirement(new ChromaReq());
        RegisterRequirement(new LegacyChromaReq());
        RegisterRequirement(new MappingExtensionsReq());
        RegisterRequirement(new NoodleExtensionsReq());
        RegisterRequirement(new CinemaReq());
        RegisterRequirement(new SoundExtensionsReq());
        RegisterRequirement(new VivifyReq());
    }

    public static void RegisterRequirement(RequirementCheck req) => requirementsAndSuggestions.Add(req);
    public abstract RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map);
}
