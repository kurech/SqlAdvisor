using System.Text.Json.Nodes;

namespace SqlAdvisor.Domain.Entities
{
    public sealed record QueryPlan(JsonNode PlanJson);
}
