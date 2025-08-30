using SqlAdvisor.Domain.Entities;
using SqlAdvisor.Domain.Interfaces;
using System.Text.Json.Nodes;

namespace SqlAdvisor.Application.Services
{
    public static class MetricsCalculator
    {
        public static Metrics Calculate(QueryPlan plan, DatabaseSettingsDto settings, Dictionary<string, long> tableRowCounts)
        {
            try
            {
                var root = plan.PlanJson;
                var planNode = root["Plan"];
                var totalCost = planNode?["Total Cost"]?.GetValue<double>() ?? 0.0;
                var outputRows = planNode?["Plan Rows"]?.GetValue<double>() ?? 0.0;

                long workMemKb = long.TryParse(settings.WorkMem, out var wm) ? wm : 4096;
                long cacheSizeKb = long.TryParse(settings.EffectiveCacheSize, out var cs) ? cs : 409600;

                long scannedRows = 0;
                long ioBytes = 0;
                string lockRisk = "normal";
                double maxScanCost = 0.0;
                TraversePlan(planNode, ref scannedRows, ref ioBytes, ref lockRisk, ref maxScanCost, tableRowCounts);

                double cacheFactor = Math.Min(0.9, cacheSizeKb / (double)(ioBytes / 1024 + 1024));
                ioBytes = (long)(ioBytes * (1 - cacheFactor * 0.5));

                double estMs = maxScanCost * 0.001;
                if (workMemKb < 65536) estMs *= 1.5;

                return new Metrics
                {
                    EstimatedTimeMs = Math.Max(10, (int)(estMs * 1000)),
                    EstimatedIoReadBytes = ioBytes,
                    ScannedRows = scannedRows,
                    LockRisk = lockRisk
                };
            }
            catch
            {
                return new Metrics { EstimatedTimeMs = 1, EstimatedIoReadBytes = 0, ScannedRows = 0, LockRisk = "normal" };
            }
        }

        private static void TraversePlan(JsonNode? node, ref long scannedRows, ref long ioBytes, ref string lockRisk, ref double maxScanCost, Dictionary<string, long> tableRowCounts)
        {
            if (node == null) return;

            var nodeType = node["Node Type"]?.GetValue<string>();
            if (nodeType == "Seq Scan" || nodeType == "Index Scan" || nodeType == "Bitmap Heap Scan")
            {
                var rows = node["Rows"]?.GetValue<double>() ?? node["Plan Rows"]?.GetValue<double>() ?? 0.0;
                var relation = node["Relation Name"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(relation) && tableRowCounts.TryGetValue(relation, out var actualRows))
                {
                    if (nodeType == "Seq Scan") rows = actualRows;
                    else if (nodeType == "Index Scan" || nodeType == "Bitmap Heap Scan") rows = Math.Min(rows, actualRows);
                }
                var width = node["Plan Width"]?.GetValue<double>() ?? 64.0;
                scannedRows += (long)rows;
                ioBytes += (long)(rows * width);
                var cost = node["Total Cost"]?.GetValue<double>() ?? 0.0;
                maxScanCost = Math.Max(maxScanCost, cost);
            }

            if (node.ToString().Contains("For Update") || node["Lock"] != null || node["Relation Lock"] != null)
                lockRisk = "elevated";

            var subPlans = node["Plans"]?.AsArray();
            if (subPlans != null)
            {
                foreach (var sub in subPlans)
                {
                    TraversePlan(sub, ref scannedRows, ref ioBytes, ref lockRisk, ref maxScanCost, tableRowCounts);
                }
            }
        }
    }
}
