using CodeReadabilityLib.Preflight;
using System.Security.Cryptography;
using System.Text.Json;

namespace CodeReadabilityLib.Checkpointing
{
    /// <summary>
    /// The checkpoint folder of one execution: its plan, one answer file per unit of work,
    /// and a "done" mark per finished unit.
    /// </summary>
    ///
    /// <remarks>
    /// A unit is one model and strategy in judging, or one model in metric grading. Answers
    /// are appended as they arrive, so a crash loses at most the answers in flight.
    /// </remarks>
    public sealed class CheckpointStore
    {
        private const string PlanFileName = "plan.json";
        private const string DoneExtension = ".done";
        private const string AnswersExtension = ".jsonl";

        private static readonly JsonSerializerOptions PlanOptions = new JsonSerializerOptions { WriteIndented = true };

        private readonly object _planLock = new object();

        /// <summary>
        /// The checkpoint folder.
        /// </summary>
        public string Folder { get; }

        public ExecutionPlan Plan { get; private set; }

        private CheckpointStore(string folder, ExecutionPlan plan)
        {
            Folder = folder;
            Plan = plan;
        }

        /// <summary>
        /// Creates the checkpoint folder of a new execution and writes its plan.
        /// </summary>
        ///
        /// <param name="checkpointsRoot">
        /// The folder holding every execution's checkpoint folder.
        /// </param>
        ///
        /// <param name="runName">
        /// The run folder's name, which the checkpoint folder takes too.
        /// </param>
        public static CheckpointStore Create(string checkpointsRoot, string runName, ExecutionPlan plan)
        {
            string folder = Path.Combine(checkpointsRoot, runName);
            Directory.CreateDirectory(folder);

            CheckpointStore store = new CheckpointStore(folder, plan);
            store.SavePlan();
            return store;
        }

        /// <summary>
        /// Opens an existing checkpoint folder.
        /// </summary>
        ///
        /// <exception cref="RequirementsNotMetException">
        /// Thrown when the folder holds no readable plan.
        /// </exception>
        public static CheckpointStore Open(string folder)
        {
            string planPath = Path.Combine(folder, PlanFileName);
            if (!File.Exists(planPath))
            {
                throw new RequirementsNotMetException("resume", [$"'{folder}' is not a checkpoint folder: it has no {PlanFileName}."]);
            }

            ExecutionPlan? plan;
            try
            {
                plan = JsonSerializer.Deserialize<ExecutionPlan>(File.ReadAllText(planPath));
            }
            catch (JsonException ex)
            {
                throw new RequirementsNotMetException("resume", [$"'{planPath}' could not be read: {ex.Message}"]);
            }

            return new CheckpointStore(Path.GetFullPath(folder), plan
                ?? throw new RequirementsNotMetException("resume", [$"'{planPath}' is empty."]));
        }

        /// <summary>
        /// Updates the plan and writes it to disk.
        /// </summary>
        public void UpdatePlan(Func<ExecutionPlan, ExecutionPlan> update)
        {
            lock (_planLock)
            {
                Plan = update(Plan);
                SavePlan();
            }
        }

        /// <summary>
        /// Records the llama.cpp build the first time it is seen, and refuses to continue
        /// with a different one.
        /// </summary>
        ///
        /// <exception cref="RequirementsNotMetException">
        /// Thrown when the build differs from the one the execution started with.
        /// </exception>
        public void EnsureLlamaCppVersion(string? version)
        {
            if (version is null)
            {
                return;
            }

            lock (_planLock)
            {
                if (Plan.LlamaCppVersion is null)
                {
                    Plan = Plan with { LlamaCppVersion = version };
                    SavePlan();
                }
                else if (Plan.LlamaCppVersion != version)
                {
                    throw new RequirementsNotMetException("resume",
                    [
                        $"The llama.cpp version changed since this execution started ({Plan.LlamaCppVersion} → {version}), " +
                        "for example because the container image was rebuilt. One execution must not mix versions; start a new execution."
                    ]);
                }
            }
        }

        public bool IsDone(string unitId)
        {
            return File.Exists(UnitPath(unitId) + DoneExtension);
        }

        public void MarkDone(string unitId)
        {
            File.WriteAllText(UnitPath(unitId) + DoneExtension, string.Empty);
        }

        /// <summary>
        /// Every entry saved for a unit so far. A damaged line (written at the moment of a
        /// crash) is skipped, so that its snippet is asked again.
        /// </summary>
        public List<T> ReadEntries<T>(string unitId)
        {
            string path = UnitPath(unitId) + AnswersExtension;
            List<T> entries = new List<T>();
            if (!File.Exists(path))
            {
                return entries;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    if (JsonSerializer.Deserialize<T>(line) is T entry)
                    {
                        entries.Add(entry);
                    }
                }
                catch (JsonException)
                {
                    // A line cut short by a crash; its snippet is simply asked again.
                }
            }

            return entries;
        }

        /// <summary>
        /// Opens the unit's answer file for appending.
        /// </summary>
        public JsonlCheckpointWriter OpenWriter(string unitId)
        {
            return new JsonlCheckpointWriter(UnitPath(unitId) + AnswersExtension);
        }

        /// <summary>
        /// A hash of a file's contents.
        /// </summary>
        public static string Sha256Of(string path)
        {
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }

        /// <summary>
        /// Removes the checkpoint folder, after the execution finished successfully.
        /// </summary>
        public void Delete()
        {
            if (Directory.Exists(Folder))
            {
                Directory.Delete(Folder, recursive: true);
            }
        }

        private void SavePlan()
        {
            File.WriteAllText(Path.Combine(Folder, PlanFileName), JsonSerializer.Serialize(Plan, PlanOptions));
        }

        /// <summary>
        /// A unit's files, named after its id with characters that are unsafe in file names
        /// replaced.
        /// </summary>
        private string UnitPath(string unitId)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string safe = new string(unitId.Select(c => invalid.Contains(c) || c is ' ' or '[' or ']' ? '_' : c).ToArray());
            return Path.Combine(Folder, safe);
        }
    }
}
