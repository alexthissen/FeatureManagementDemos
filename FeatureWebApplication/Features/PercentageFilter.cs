using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FeatureWebApplication.Features
{
    public class PercentageFilter : IFeatureFilter
    {
        public bool Evaluate(FeatureFilterEvaluationContext context)
        {
            return (new Random(Environment.TickCount).Next(100)) > Int32.Parse(context.Parameters["Value"]);
        }
    }
}
