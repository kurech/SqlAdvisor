using SqlAdvisor.Domain.Entities;
using SqlAdvisor.Domain.Interfaces;
using System.Text.Json.Nodes;

namespace SqlAdvisor.Application.Services
{
    public sealed class AnalyzeQueryUseCase
    {
        private readonly IQueryAnalyzer _analyzer;
        private readonly IStatisticsProvider _stats;
        private readonly IRuleEngine _rules;

        public AnalyzeQueryUseCase(IQueryAnalyzer analyzer, IStatisticsProvider stats, IRuleEngine rules)
        {
            _analyzer = analyzer;
            _stats = stats;
            _rules = rules;
        }

        public async Task<AnalyzeResult> ExecuteAsync(string sql, CancellationToken ct = default)
        {
            QueryPlan plan = new QueryPlan(JsonNode.Parse("{}")!);
            Metrics metrics = new Metrics { EstimatedTimeMs = 1, EstimatedIoReadBytes = 0, ScannedRows = 0, LockRisk = "normal" };
            IEnumerable<Recommendation> recs = Enumerable.Empty<Recommendation>();
            IEnumerable<Warning> warns;

            try
            {
                plan = await _analyzer.ExplainAsync(sql, ct);
                var dbSettings = await _stats.GetDatabaseSettingsAsync(ct);
                var tableRowCounts = await _stats.GetTableRowCountsAsync(ct);
                metrics = MetricsCalculator.Calculate(plan, dbSettings, tableRowCounts);
                recs = _rules.Analyze(plan, metrics);
                warns = _rules.AnalyzeWarnings(sql, plan);
            }
            catch (Exception ex)
            {
                warns = _rules.AnalyzeWarnings(sql, plan)
                    .Append(new Warning { Code = "analysis-error", Message = $"Error during analysis: {ex.Message}" });
            }

            return new AnalyzeResult(plan, metrics, recs, warns);
        }
    }

    public sealed record AnalyzeResult(QueryPlan Plan, Metrics Metrics, IEnumerable<Recommendation> Recommendations, IEnumerable<Warning> Warnings);
}
