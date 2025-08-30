using SqlAdvisor.Domain.Entities;

namespace SqlAdvisor.Domain.Interfaces
{
    public interface IRuleEngine
    {
        IEnumerable<Recommendation> Analyze(QueryPlan plan, Metrics metrics);
        IEnumerable<Warning> AnalyzeWarnings(string sql, QueryPlan plan);
    }
}
