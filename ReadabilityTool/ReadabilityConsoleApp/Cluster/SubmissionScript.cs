using System.Text;

namespace ReadabilityConsoleApp.Cluster
{
    /// <summary>
    /// Writes the bash script that submits a set of jobs with <c>sbatch</c>.
    /// </summary>
    ///
    /// <remarks>
    /// The tool plans the jobs inside the container on the login node, where <c>sbatch</c>
    /// is not available; the submit script runs the written script outside the container.
    /// Every job asks for a warning 10 minutes before its time limit and may be requeued,
    /// which the job script uses to stop cleanly and continue in a new allocation under the
    /// same job ID, so that the final job's dependency keeps waiting for it.
    /// </remarks>
    public static class SubmissionScript
    {
        /// <summary>
        /// The job script every job runs, relative to the project folder.
        /// </summary>
        public const string JobScript = "scripts/helpers/job.sh";

        /// <summary>
        /// Where SLURM writes each job's output, relative to the project folder.
        /// </summary>
        public const string LogFolder = "logs";

        public static string Build(string projectFolder, IReadOnlyList<ClusterJob> jobs)
        {
            StringBuilder script = new StringBuilder();
            script.Append("#!/bin/bash\n");
            script.Append("# Written by ReadabilityConsoleApp submit. Run by scripts/submit.sh outside the container.\n");
            script.Append("set -euo pipefail\n");
            script.Append($"cd {Quote(projectFolder)}\n");
            script.Append($"mkdir -p {LogFolder}\n\n");

            List<string> earlier = new List<string>();
            for (int i = 0; i < jobs.Count; i++)
            {
                ClusterJob job = jobs[i];
                string variable = $"job{i + 1}";

                List<string> options =
                [
                    "--parsable",
                    $"--job-name={Quote(job.Name)}",
                    $"--partition={job.Resources.Partition}",
                    $"--cpus-per-task={job.Resources.Cpus}",
                    $"--mem={job.Resources.MemoryGb}G",
                    $"--time={job.Resources.TimeLimit}",
                    $"--output={LogFolder}/slurm-%j.out",
                    "--open-mode=append",
                    "--requeue",
                    "--signal=B:USR1@600"
                ];

                if (job.Resources.Tier is GpuTier tier)
                {
                    options.Add($"--gres=gpu:{job.Resources.Gpus}");
                    if (tier.Constraint is string constraint)
                    {
                        options.Add($"--constraint={Quote(constraint)}");
                    }
                }

                if (job.AfterAllOthers && earlier.Count > 0)
                {
                    options.Add("--dependency=afterok:" + string.Join(':', earlier.Select(v => "${" + v + "}")));
                }

                string arguments = string.Join(' ', job.ToolArguments.Select(Quote));
                script.Append($"{variable}=$(sbatch {string.Join(' ', options)} {JobScript} {arguments})\n");
                script.Append($"echo \"Submitted job ${{{variable}}}: {Escape(job.Description)} [{job.Resources.Label}]\"\n\n");

                earlier.Add(variable);
            }

            return script.ToString();
        }

        /// <summary>
        /// Quotes a value for bash, so that spaces and special characters pass unchanged.
        /// </summary>
        private static string Quote(string value)
        {
            return "'" + value.Replace("'", "'\\''") + "'";
        }

        private static string Escape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("$", "\\$").Replace("`", "\\`");
        }
    }
}
