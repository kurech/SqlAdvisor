using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlAdvisor.Domain.Entities
{
    public sealed record Recommendation
    {
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string Detail { get; init; } = "";
        public RecommendationPriority Priority { get; init; } = RecommendationPriority.Medium;
        public double EstimatedSpeedupFactor { get; init; }
        public string Category { get; init; } = "query";
    }

    public enum RecommendationPriority { Low, Medium, High }
}
