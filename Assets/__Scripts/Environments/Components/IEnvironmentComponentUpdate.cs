public interface IEnvironmentComponentUpdate
{
    bool ShouldInclude { get; }
    bool ShouldRefresh { get; }
    void Refresh();
}
