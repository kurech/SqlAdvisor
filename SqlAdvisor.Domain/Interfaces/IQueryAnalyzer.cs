using SqlAdvisor.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlAdvisor.Domain.Interfaces
{
    public interface IQueryAnalyzer
    {
        /// <summary>
        /// Возвращает план EXPLAIN (FORMAT JSON) как объект QueryPlan.
        /// Метод не выполняет сам SQL — только вызывает EXPLAIN.
        /// </summary>
        Task<QueryPlan> ExplainAsync(string sql, CancellationToken ct = default);
    }
}
