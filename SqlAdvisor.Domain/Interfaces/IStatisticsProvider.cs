using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlAdvisor.Domain.Interfaces
{
    public interface IStatisticsProvider
    {
        /// <summary>
        /// Возвращает набор служебных метрик/настроек БД, необходимых для расчёта оценки.
        /// Например: work_mem, effective_cache_size, размер таблицы и т.д.
        /// </summary>
        Task<DatabaseSettingsDto> GetDatabaseSettingsAsync(CancellationToken ct = default);
        Task<Dictionary<string, long>> GetTableRowCountsAsync(CancellationToken ct = default);
    }

    public sealed record DatabaseSettingsDto
    {
        public string WorkMem { get; init; } = "";
        public string EffectiveCacheSize { get; init; } = "";
    }
}
