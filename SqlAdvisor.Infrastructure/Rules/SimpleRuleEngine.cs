using SqlAdvisor.Domain.Entities;
using SqlAdvisor.Domain.Interfaces;
using System.Text.Json.Nodes;

namespace SqlAdvisor.Infrastructure.Rules
{
    public sealed class SimpleRuleEngine : IRuleEngine
    {
        public IEnumerable<Recommendation> Analyze(QueryPlan plan, Metrics metrics)
        {
            var list = new List<Recommendation>();
            bool hasSeqScan = false;
            bool hasNestedLoop = false;
            bool hasSortAgg = false;
            bool hasCte = false;
            bool hasLock = metrics.LockRisk == "elevated";
            
            TraversePlan(plan.PlanJson["Plan"], ref hasSeqScan, ref hasNestedLoop, ref hasSortAgg, ref hasCte);

            if (hasSeqScan && metrics.ScannedRows > 10000)
            {
                list.Add(new Recommendation
                {
                    Id = "qry-seqscan",
                    Title = "Полное сканирование таблицы",
                    Detail = "Запрос выполняется последовательным сканированием на большом объёме строк. Проверьте селективность фильтрации и необходимость ограничения выборки.",
                    Priority = RecommendationPriority.High,
                    EstimatedSpeedupFactor = 2.0,
                    Category = "query"
                });
            }

            if (hasNestedLoop && metrics.ScannedRows > 50000)
            {
                list.Add(new Recommendation
                {
                    Id = "qry-nestedloop",
                    Title = "Nested Loop на большом объёме",
                    Detail = "Обнаружен Nested Loop на большом количестве строк. Проверьте условия соединения, возможна перестройка запроса (например, другой тип JOIN).",
                    Priority = RecommendationPriority.Medium,
                    EstimatedSpeedupFactor = 1.5,
                    Category = "query"
                });
            }

            if (hasSortAgg)
            {
                list.Add(new Recommendation
                {
                    Id = "qry-sortagg",
                    Title = "Тяжёлая сортировка/агрегация",
                    Detail = "В плане присутствует сортировка или агрегация. При большом числе строк это может стать узким местом.",
                    Priority = RecommendationPriority.Medium,
                    EstimatedSpeedupFactor = 1.2,
                    Category = "query"
                });
            }

            if (hasCte)
            {
                list.Add(new Recommendation
                {
                    Id = "qry-cte",
                    Title = "Материализация CTE",
                    Detail = "Используется CTE Scan. При большом числе строк материализация может быть дорогой.",
                    Priority = RecommendationPriority.Low,
                    EstimatedSpeedupFactor = 1.1,
                    Category = "query"
                });
            }

            if (hasLock)
            {
                list.Add(new Recommendation
                {
                    Id = "qry-lock",
                    Title = "Запрос с блокировками",
                    Detail = "Запрос содержит операции блокировки (FOR UPDATE / LOCK TABLE). Возможны блокировки при параллельной работе.",
                    Priority = RecommendationPriority.Medium,
                    EstimatedSpeedupFactor = 0,
                    Category = "concurrency"
                });
            }

            return list;
        }

        private static void TraversePlan(JsonNode? node, ref bool hasSeqScan, ref bool hasNestedLoop, ref bool hasSortAgg, ref bool hasCte)
        {
            if (node == null) return;

            var nodeType = node["Node Type"]?.GetValue<string>();
            if (nodeType == "Seq Scan") hasSeqScan = true;
            if (nodeType == "Nested Loop") hasNestedLoop = true;
            if (nodeType == "Sort" || nodeType == "HashAggregate" || nodeType == "Aggregate") hasSortAgg = true;
            if (nodeType == "CTE Scan") hasCte = true;

            var subPlans = node["Plans"]?.AsArray();
            if (subPlans != null)
            {
                foreach (var sub in subPlans)
                {
                    TraversePlan(sub, ref hasSeqScan, ref hasNestedLoop, ref hasSortAgg, ref hasCte);
                }
            }
        }

        public IEnumerable<Warning> AnalyzeWarnings(string sql, QueryPlan plan)
        {
            var list = new List<Warning>();
            var lowerSql = sql.ToLowerInvariant().TrimStart();

            // DML без WHERE
            if (lowerSql.StartsWith("update") && !lowerSql.Contains("where"))
                list.Add(new Warning { Code = "upd-no-where", Message = "UPDATE без WHERE может затронуть все строки." });

            if (lowerSql.StartsWith("delete") && !lowerSql.Contains("where"))
                list.Add(new Warning { Code = "del-no-where", Message = "DELETE без WHERE может удалить все строки." });

            // SELECT *
            if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
                list.Add(new Warning { Code = "select-star", Message = "Использование SELECT * может приводить к выборке лишних колонок." });

            // LIMIT без ORDER BY
            if (sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase) && !sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
                list.Add(new Warning { Code = "limit-no-order", Message = "LIMIT без ORDER BY даёт непредсказуемый порядок строк." });

            // Функция в WHERE (улучшено: ищем common funcs после WHERE)
            var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
            if (whereIndex >= 0)
            {
                var whereClause = sql.Substring(whereIndex + 5);
                if (whereClause.Contains("(") && (whereClause.Contains("upper(") || whereClause.Contains("lower(") || whereClause.Contains("trim(") || whereClause.Contains("cast(")))
                    list.Add(new Warning { Code = "func-in-where", Message = "Фильтрация по выражению или функции в WHERE может мешать использованию индекса." });
            }

            // UNION вместо UNION ALL
            if (sql.Contains("UNION", StringComparison.OrdinalIgnoreCase) && !sql.Contains("UNION ALL", StringComparison.OrdinalIgnoreCase))
                list.Add(new Warning { Code = "union-distinct", Message = "UNION выполняет уникализацию через сортировку, что может быть дорого. Рассмотрите UNION ALL." });

            return list;
        }
    }
}
