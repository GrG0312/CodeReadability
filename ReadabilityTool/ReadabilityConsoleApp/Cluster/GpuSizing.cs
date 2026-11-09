using CodeReadabilityLib.Llms.Models.Descriptions;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReadabilityConsoleApp.Cluster
{
    /// <summary>
    /// Decides which GPUs a job needs for a model.
    /// </summary>
    ///
    /// <remarks>
    /// Needed GPU memory ≈ GGUF file size × 4/3, matching the reference points given by the
    /// cluster side (4-bit 30B → 20 GB, 8-bit 30B → 40 GB, 4-bit 15B → 10 GB, 8-bit 15B
    /// → 20 GB): the extra third covers the working context and server overhead. The job asks
    /// for the smallest option that fits. A model's description can override the estimate
    /// with a <c>gpu</c> entry such as <c>vram24</c>, <c>vram24x2</c> or <c>a100</c>.
    /// </remarks>
    public static partial class GpuSizing
    {
        [GeneratedRegex(@"^vram(?<memory>\d+)(?:x(?<count>\d+))?$", RegexOptions.IgnoreCase)]
        private static partial Regex OverridePattern();

        /// <summary>
        /// The estimated GPU memory a model needs, in GB.
        /// </summary>
        public static double NeededMemoryGb(string modelPath)
        {
            return new FileInfo(modelPath).Length / (1024.0 * 1024 * 1024) * 4 / 3;
        }

        /// <summary>
        /// The resources of a job that runs one model.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when no GPU option fits, or a <c>gpu</c> override cannot be read.
        /// </exception>
        public static JobResources ForModel(string modelPath, ModelDescription? description, int concurrency)
        {
            double fileGb = new FileInfo(modelPath).Length / (1024.0 * 1024 * 1024);
            (GpuTier tier, int gpus) = description?.Gpu is { Length: > 0 } requested
                ? Override(requested, Path.GetFileNameWithoutExtension(modelPath))
                : Smallest(NeededMemoryGb(modelPath), Path.GetFileNameWithoutExtension(modelPath));

            return new JobResources
            {
                Tier = tier,
                Gpus = gpus,
                Partition = tier.Partition,
                TimeLimit = tier.TimeLimit,
                Cpus = Math.Clamp(concurrency + 3, 4, 16),

                // The model is read into memory while it loads, plus room for the tool itself.
                MemoryGb = Math.Max(16, (int)Math.Ceiling(fileGb * 1.2) + 6)
            };
        }

        /// <summary>
        /// The resources that fit every one of <paramref name="jobs"/>: the largest GPU need,
        /// the most memory and CPUs.
        /// </summary>
        public static JobResources Largest(IReadOnlyList<JobResources> jobs)
        {
            if (jobs.Count == 0)
            {
                return JobResources.CpuOnly;
            }

            JobResources largestGpu = jobs
                .OrderByDescending(job => (job.Tier?.MemoryGb ?? 0) * Math.Max(1, job.Gpus))
                .ThenByDescending(job => job.Tier?.MemoryGb ?? 0)
                .First();

            return largestGpu with
            {
                Cpus = jobs.Max(job => job.Cpus),
                MemoryGb = jobs.Max(job => job.MemoryGb)
            };
        }

        private static (GpuTier Tier, int Gpus) Smallest(double neededGb, string modelFile)
        {
            foreach (GpuTier tier in GpuTier.All.Where(tier => tier.Name != "vram40"))
            {
                if (neededGb <= tier.MemoryGb)
                {
                    return (tier, 1);
                }
            }

            // Too big for one card: split across two 24 GB cards before using the A100 node,
            // which is a single machine with a slow network link.
            GpuTier large = GpuTier.All.Single(tier => tier.Name == "vram24");
            if (neededGb <= large.MemoryGb * large.MaxGpus)
            {
                return (large, large.MaxGpus);
            }

            throw new ArgumentException(
                $"Model '{modelFile}' needs about {neededGb.ToString("0", CultureInfo.InvariantCulture)} GB of GPU memory, " +
                "more than any job on the cluster can have. Add a 'gpu' entry to its description to choose GPUs by hand.");
        }

        private static (GpuTier Tier, int Gpus) Override(string requested, string modelFile)
        {
            string normalized = requested.Trim().ToLowerInvariant() == "a100" ? "vram40" : requested.Trim();
            Match match = OverridePattern().Match(normalized);
            GpuTier? tier = match.Success
                ? GpuTier.All.FirstOrDefault(t => t.MemoryGb == int.Parse(match.Groups["memory"].Value, CultureInfo.InvariantCulture))
                : null;

            if (tier is null)
            {
                throw new ArgumentException(
                    $"Model '{modelFile}' has gpu '{requested}', which is not a GPU option. Use vram8, vram11, vram16, vram24, " +
                    "vram40 or a100, optionally with a count such as vram24x2.");
            }

            int gpus = match.Groups["count"].Success ? int.Parse(match.Groups["count"].Value, CultureInfo.InvariantCulture) : 1;
            if (gpus < 1 || gpus > tier.MaxGpus)
            {
                throw new ArgumentException($"Model '{modelFile}' asks for {gpus} x {tier.Name}; at most {tier.MaxGpus} are allowed.");
            }

            return (tier, gpus);
        }
    }
}
