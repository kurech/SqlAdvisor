using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlAdvisor.Domain.Entities
{
    public sealed record Metrics
    {
        public int EstimatedTimeMs { get; init; }
        public long EstimatedIoReadBytes { get; init; }
        public long ScannedRows { get; init; }
        public string LockRisk { get; init; } = "normal";
    }
}
