namespace CodeReadabilityLib.Analysis.RunAgreement
{
    /// <summary>
    /// The agreement statistics every run reports, each with a bootstrap confidence interval.
    /// </summary>
    public static class StatisticCatalog
    {
        public static readonly IReadOnlyList<StatisticDefinition> All =
        [
            new StatisticDefinition { Key = "spearman", Index = 0, HigherIsBetter = true, Read = s => s.Spearman },
            new StatisticDefinition { Key = "pearson", Index = 1, HigherIsBetter = true, Read = s => s.Pearson },
            new StatisticDefinition { Key = "kendallTau", Index = 2, HigherIsBetter = true, Read = s => s.KendallTau },
            new StatisticDefinition { Key = "qwk", Index = 3, HigherIsBetter = true, Read = s => s.QuadraticWeightedKappa },
            new StatisticDefinition { Key = "mae", Index = 4, HigherIsBetter = false, Read = s => s.MeanAbsoluteError },
            new StatisticDefinition { Key = "exactMatchRate", Index = 5, HigherIsBetter = true, Read = s => s.ExactMatchRate },
            new StatisticDefinition { Key = "auc", Index = 6, HigherIsBetter = true, Read = s => s.Auc }
        ];
    }
}
