using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlAdvisor.Domain.Interfaces;
using SqlAdvisor.Domain.Entities;
using System.Text.Json.Nodes;
using Npgsql;

namespace SqlAdvisor.Infrastructure.Postgres
{
    public sealed class PostgresQueryAnalyzer : IQueryAnalyzer
    {
        private readonly string _connectionString;

        public PostgresQueryAnalyzer(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<QueryPlan> ExplainAsync(string sql, CancellationToken ct = default)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                var wrapped = $"EXPLAIN (FORMAT JSON, ANALYZE FALSE, COSTS TRUE, TIMING FALSE) {sql}";
                await using var cmd = new NpgsqlCommand(wrapped, conn) { CommandTimeout = 30 };
                await using var reader = await cmd.ExecuteReaderAsync(ct);

                var jsonText = new StringBuilder();
                while (await reader.ReadAsync(ct))
                {
                    jsonText.Append(reader.GetString(0));
                }

                var parsed = JsonNode.Parse(jsonText.ToString())!.AsArray()[0]!;
                return new QueryPlan(parsed);
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException($"Invalid SQL for EXPLAIN: {ex.Message}", ex);
            }
        }
    }
}
