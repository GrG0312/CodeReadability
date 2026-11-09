using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Preflight;
using CodeReadabilityLib.Reporting.Judgements;
using CodeReadabilityLib.Reporting.MetricResults;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeReadabilityLib.Analysis.Inputs
{
    /// <summary>
    /// The two input files of <c>compute-statistics</c>, loaded and checked.
    /// </summary>
    public sealed class StatisticsInputs
    {
        /// <summary>
        /// The judgement file exactly as read, so that its copy stays identical.
        /// </summary>
        public required JsonObject JudgementJson { get; init; }

        public required JudgementReport Judgements { get; init; }

        public required MetricsReport Metrics { get; init; }

        public required string JudgementPath { get; init; }

        public required string MetricsPath { get; init; }

        /// <summary>
        /// Loads both files and checks every requirement before anything is computed.
        /// </summary>
        ///
        /// <exception cref="RequirementsNotMetException">
        /// Thrown, listing every problem, when a requirement is not met: the files come from
        /// different datasets or carry no dataset ID, or a model description is missing,
        /// still holds placeholders, or has a type other than reasoning or flash.
        /// </exception>
        public static StatisticsInputs Load(string judgementPath, string metricsPath)
        {
            string judgementText = ReadFile(judgementPath, "judgement");
            string metricsText = ReadFile(metricsPath, "metrics");

            JsonObject judgementJson = JsonNode.Parse(judgementText) as JsonObject
                ?? throw new InvalidDataException($"'{judgementPath}' is not a JSON object.");

            if (judgementJson.ContainsKey("statistics"))
            {
                throw new RequirementsNotMetException("compute-statistics",
                [
                    $"'{judgementPath}' already holds statistics. compute-statistics only accepts mass-judge output; " +
                    "run it again on the original judgement file instead."
                ]);
            }

            JudgementReport judgements = Deserialize<JudgementReport>(judgementText, judgementPath);
            MetricsReport metrics = Deserialize<MetricsReport>(metricsText, metricsPath);

            List<string> problems = new List<string>();
            CheckDataset(judgements, metrics, problems);
            CheckDescriptions(judgements, problems);

            if (problems.Count > 0)
            {
                throw new RequirementsNotMetException("compute-statistics", problems);
            }

            return new StatisticsInputs
            {
                JudgementJson = judgementJson,
                Judgements = judgements,
                Metrics = metrics,
                JudgementPath = Path.GetFullPath(judgementPath),
                MetricsPath = Path.GetFullPath(metricsPath)
            };
        }

        private static void CheckDataset(JudgementReport judgements, MetricsReport metrics, List<string> problems)
        {
            string? judgedId = judgements.Provenance?.DatasetId;
            string? metricsId = metrics.Provenance?.DatasetId;

            if (string.IsNullOrWhiteSpace(judgedId))
            {
                problems.Add("The judgement file records no dataset ID.");
            }

            if (string.IsNullOrWhiteSpace(metricsId))
            {
                problems.Add("The metrics file records no dataset ID.");
            }

            if (!string.IsNullOrWhiteSpace(judgedId) && !string.IsNullOrWhiteSpace(metricsId) && judgedId != metricsId)
            {
                problems.Add($"The files come from different datasets: judgements from dataset ID {judgedId}, metrics from {metricsId}.");
            }
        }

        private static void CheckDescriptions(JudgementReport judgements, List<string> problems)
        {
            Dictionary<string, ModelDescription> descriptions =
                new Dictionary<string, ModelDescription>(judgements.ModelDescriptions, StringComparer.OrdinalIgnoreCase);

            foreach (string model in judgements.Runs.Select(run => run.ModelName).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!descriptions.TryGetValue(model, out ModelDescription? description))
                {
                    problems.Add($"Model '{model}' has no entry in model-descriptions.");
                    continue;
                }

                if (description.HasPlaceholders)
                {
                    problems.Add($"Model '{model}' still has '{ModelTypes.Placeholder}' values in model-descriptions; fill them in first.");
                }

                string type = ModelTypes.Normalize(description.Type);
                if (type is not (ModelTypes.Reasoning or ModelTypes.Flash) && type != ModelTypes.Placeholder)
                {
                    problems.Add($"Model '{model}' has type '{description.Type}'; in a judgement file it must be {ModelTypes.Reasoning} or {ModelTypes.Flash}.");
                }
            }
        }

        private static string ReadFile(string path, string role)
        {
            return File.Exists(path)
                ? File.ReadAllText(path)
                : throw new FileNotFoundException($"The {role} file was not found: {path}", path);
        }

        private static T Deserialize<T>(string text, string path)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(text)
                    ?? throw new InvalidDataException($"'{path}' is empty.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"'{path}' is not a valid file of the expected kind: {ex.Message}", ex);
            }
        }
    }
}
