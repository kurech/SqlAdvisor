using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlAdvisor.Domain.Entities
{
    public sealed record Warning
    {
        public string Code { get; init; } = "";
        public string Message { get; init; } = "";
    }
}
