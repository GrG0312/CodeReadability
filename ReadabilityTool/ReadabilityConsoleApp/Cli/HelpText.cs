using CodeReadabilityLib;
using CodeReadabilityLib.Languages;
using CodeReadabilityLib.Llms.Judging.Prompting;

namespace ReadabilityConsoleApp.Cli
{
    /// <summary>
    /// Renders the tool's usage text.
    /// </summary>
    public static class HelpText
    {
        /// <summary>
        /// Prints usage for every command to standard output.
        /// </summary>
        public static void Print()
        {
            Console.WriteLine($"Code Readability Tool v{LibraryInfo.Version}");
            Console.WriteLine();
            Console.WriteLine("Usage: ReadabilityConsoleApp <command> [options]");
            Console.WriteLine();

            Console.WriteLine("COMMANDS");
            Console.WriteLine("  mass-judge          Every model x prompting strategy scores a labelled dataset (answers only).");
            Console.WriteLine("  compute-metrics     Algorithmic and/or LLM-graded readability metrics of every snippet.");
            Console.WriteLine("  compute-statistics  Agreement, model axes, prompting strategies and metric impact of a");
            Console.WriteLine("                      judgement file; writes a copy with the statistics added.");
            Console.WriteLine("  run-pipeline        mass-judge and compute-metrics, then compute-statistics, in one run folder.");
            Console.WriteLine("  check-models        Quick test of every model, with an estimate of the judging time.");
            Console.WriteLine("  resume              Continue an execution that stopped early: resume --checkpoint <folder>.");
            Console.WriteLine("  submit              On the cluster: plan SLURM jobs for a command (used by scripts/submit.sh).");
            Console.WriteLine("  help                Show this text.");
            Console.WriteLine();
            Console.WriteLine("Every command checks all its requirements before starting and stops immediately if one");
            Console.WriteLine("is not met. Outputs go to <output>/run_<date>_<time>/<step>/. Datasets are copied into");
            Console.WriteLine("a 'datasets' folder next to <output> with a __visi-<id> stamp; originals are never changed.");
            Console.WriteLine();

            Console.WriteLine("mass-judge");
            Console.WriteLine("  --dataset <path>          Labelled dataset (.csv, .json, .parquet).     [required]");
            Console.WriteLine("  --model-dir <path>        Directory of .gguf models.                    [required]");
            Console.WriteLine($"  --language <name>         One of: {LanguageRegistry.SupportedNames}. Default: python.");
            Console.WriteLine($"  --strategies <ids>        Comma-separated: {PromptingStrategies.SupportedIds}. Default: all.");
            Console.WriteLine("  --model-descriptions <p>  Model description file. Default: descriptions.json in --model-dir.");
            Console.WriteLine("                            Models missing from it get placeholders, to fill in before");
            Console.WriteLine("                            compute-statistics.");
            Console.WriteLine();

            Console.WriteLine("compute-metrics");
            Console.WriteLine("  --dataset <path>          Dataset, scored or not.                       [required]");
            Console.WriteLine($"  --language <name>         One of: {LanguageRegistry.SupportedNames}.          [required]");
            Console.WriteLine("  --mode <kinds>            algo-only, llm-only or both. Default: both.");
            Console.WriteLine("  --model-dir <path>        Models grading the LLM-based metrics.  [required unless algo-only]");
            Console.WriteLine("  --model-descriptions <p>  As for mass-judge. Hybrid models grade once, with thinking off.");
            Console.WriteLine();

            Console.WriteLine("compute-statistics");
            Console.WriteLine("  --judgements <path>       A judgement file written by mass-judge.       [required]");
            Console.WriteLine("  --metrics <path>          A metrics file of the same dataset.           [required]");
            Console.WriteLine("  --bootstrap <n>           Bootstrap resamples. Default: 2000.");
            Console.WriteLine("  --alpha <p>               Family-wise significance level. Default: 0.05.");
            Console.WriteLine("  --seed <n>                Bootstrap seed, for reproducibility. Default: 12345.");
            Console.WriteLine("  Stops if the files come from different datasets or a model description holds placeholders.");
            Console.WriteLine();

            Console.WriteLine("run-pipeline");
            Console.WriteLine("  --dataset, --model-dir, --language [required], plus every option of the three steps.");
            Console.WriteLine("  Needs a complete model description file (--model-descriptions, or descriptions.json in");
            Console.WriteLine("  --model-dir) describing every model.");
            Console.WriteLine();

            Console.WriteLine("check-models");
            Console.WriteLine("  --model-dir <path>        Directory of .gguf models.                    [required]");
            Console.WriteLine("  --dataset <path>          Optional: test with its snippets and estimate judging time for it.");
            Console.WriteLine("  --model-descriptions <p>  As for mass-judge; hybrid models' thinking switch is verified.");
            Console.WriteLine();

            Console.WriteLine("submit <command> [its options]");
            Console.WriteLine("  --job-mode <mode>         one-job (default): everything in one job; multi-job: one job per");
            Console.WriteLine("                            model in parallel, then a final job for the output files.");
            Console.WriteLine("  --script-out <path>       Where to write the sbatch script.              [required]");
            Console.WriteLine("  GPUs per job: model file size x 4/3, smallest fitting option; a model's 'gpu' description");
            Console.WriteLine("  entry (vram8, vram11, vram16, vram24, vram24x2, a100) overrides it. Jobs requeue themselves");
            Console.WriteLine("  at the time limit and continue where they stopped.");
            Console.WriteLine();

            Console.WriteLine("SHARED OPTIONS");
            Console.WriteLine("  --output <path>           Output root. Default: ./output");
            Console.WriteLine("  --concurrency <n>         Snippets scored at once (also server slots). Default: 1.");
            Console.WriteLine($"  --thinking-budget <n>     Extra tokens for thinking (not for flash runs). Default: {SharedOptions.DefaultThinkingBudget}.");
            Console.WriteLine("  --checkpoint-dir <path>   Where resume data is kept. Default: 'checkpoints' next to <output>.");
            Console.WriteLine("                            Deleted after success; kept after a stop, for 'resume'.");
            Console.WriteLine("  --ctx-size <n>            Model context size, shared by the parallel slots. Default: 4096.");
            Console.WriteLine("  --gpu-layers <n>          Layers offloaded to the GPU. Default: 99.");
            Console.WriteLine("  --port <n>                llama-server port. Default: 0 (auto-select a free port).");
            Console.WriteLine("  --llama-server-path <p>   llama-server executable. Default: llama-server (on PATH).");
            Console.WriteLine("  --server-args=\"<args>\"    Extra llama-server arguments.");
            Console.WriteLine("  --log-dir <path>          Where llama-server output is captured. Default: ./logs/llama");
            Console.WriteLine("  --verbose                 Enable debug logging.");
            Console.WriteLine();

            Console.WriteLine("EXAMPLES");
            Console.WriteLine("  ReadabilityConsoleApp check-models --model-dir ./models --dataset data.csv");
            Console.WriteLine("  ReadabilityConsoleApp run-pipeline --dataset data.csv --model-dir ./models --language python");
            Console.WriteLine("  ReadabilityConsoleApp compute-statistics --judgements <judged.json> --metrics <metrics.json>");
        }
    }
}
