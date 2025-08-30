using Npgsql;
using SqlAdvisor.Domain.Interfaces;

namespace SqlAdvisor.Infrastructure.Postgres
{
    public sealed class PgSettingsProvider : IStatisticsProvider
    {
        private readonly string _connectionString;
        public PgSettingsProvider(string connectionString) => _connectionString = connectionString;

        public async Task<DatabaseSettingsDto> GetDatabaseSettingsAsync(CancellationToken ct = default)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("select name, setting from pg_settings where name in ('work_mem','effective_cache_size')", conn);
            await using var rd = await cmd.ExecuteReaderAsync(ct);


            var dto = new DatabaseSettingsDto();
            while (await rd.ReadAsync(ct))
            {
                var name = rd.GetString(0);
                var value = rd.GetString(1);
                if (name == "work_mem") dto = dto with { WorkMem = value };
                if (name == "effective_cache_size") dto = dto with { EffectiveCacheSize = value };
            }
            return dto;
        }

        public async Task<Dictionary<string, long>> GetTableRowCountsAsync(CancellationToken ct = default)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT relname, reltuples::bigint FROM pg_class WHERE relkind = 'r'", conn);
            await using var rd = await cmd.ExecuteReaderAsync(ct);

            var result = new Dictionary<string, long>();
            while (await rd.ReadAsync(ct))
            {
                result[rd.GetString(0)] = rd.GetInt64(1);
            }
            return result;
        }
    }
}
