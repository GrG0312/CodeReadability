namespace CodeReadabilityLib.Datasets
{
    /// <summary>
    /// Defines a contract for loading datasets of code snippets from a specified path.
    /// </summary>
    public interface IDatasetLoader
    {
        /// <summary>
        /// Loads a dataset of code snippets from the specified <paramref name="path"/>.
        /// </summary>
        /// 
        /// <returns>
        /// A list of <see cref="CodeSnippet"/> objects representing the loaded dataset.
        /// </returns>
        public List<CodeSnippet> Load(string path);
    }
}
