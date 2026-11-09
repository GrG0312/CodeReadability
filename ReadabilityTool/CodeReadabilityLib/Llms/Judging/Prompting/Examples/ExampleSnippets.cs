namespace CodeReadabilityLib.Llms.Judging.Prompting.Examples
{
    /// <summary>
    /// The scored examples shown by the one-shot and few-shot strategies.
    /// </summary>
    ///
    /// <remarks>
    /// The examples are written for this purpose and are not drawn from any dataset, so they
    /// cannot leak reference scores into the evaluation. Each performs the same task at a
    /// different readability level, so that the score difference is attributable to
    /// readability rather than to what the code does. Languages without their own examples
    /// fall back to the Python ones.
    /// </remarks>
    public static class ExampleSnippets
    {
        private static readonly ScoredExample[] Python =
        [
            new ScoredExample
            {
                Code = """
                    def f(l,t):
                      r=[]
                      for i in range(len(l)):
                          if l[i][1]>t:
                            if l[i][2]==True: r.append(l[i][0])
                      return r
                    """,
                Score = 1
            },
            new ScoredExample
            {
                Code = """
                    def get_names(items, limit):
                        result = []
                        for item in items:
                            if item[1] > limit and item[2]:
                                result.append(item[0])
                        return result
                    """,
                Score = 3
            },
            new ScoredExample
            {
                Code = """"
                    def active_users_above_score(users, min_score):
                        """Return the names of active users whose score exceeds min_score."""
                        return [
                            user.name
                            for user in users
                            if user.is_active and user.score > min_score
                        ]
                    """",
                Score = 5
            }
        ];

        private static readonly ScoredExample[] CSharp =
        [
            new ScoredExample
            {
                Code = """
                    public List<string> F(List<object[]> l,int t){var r=new List<string>();
                    for(int i=0;i<l.Count;i++){if((int)l[i][1]>t){if((bool)l[i][2]==true){r.Add((string)l[i][0]);}}}
                    return r;}
                    """,
                Score = 1
            },
            new ScoredExample
            {
                Code = """
                    public List<string> GetNames(List<User> items, int limit)
                    {
                        var result = new List<string>();
                        foreach (var item in items)
                        {
                            if (item.Score > limit && item.Active)
                                result.Add(item.Name);
                        }
                        return result;
                    }
                    """,
                Score = 3
            },
            new ScoredExample
            {
                Code = """
                    /// <summary>Names of the active users whose score exceeds <paramref name="minScore"/>.</summary>
                    public IEnumerable<string> ActiveUsersAboveScore(IEnumerable<User> users, int minScore)
                    {
                        return users
                            .Where(user => user.IsActive && user.Score > minScore)
                            .Select(user => user.Name);
                    }
                    """,
                Score = 5
            }
        ];

        private static readonly ScoredExample[] Java =
        [
            new ScoredExample
            {
                Code = """
                    public List<String> f(List<Object[]> l,int t){List<String> r=new ArrayList<>();
                    for(int i=0;i<l.size();i++){if((int)l.get(i)[1]>t){if((boolean)l.get(i)[2]==true){r.add((String)l.get(i)[0]);}}}
                    return r;}
                    """,
                Score = 1
            },
            new ScoredExample
            {
                Code = """
                    public List<String> getNames(List<User> items, int limit) {
                        List<String> result = new ArrayList<>();
                        for (User item : items) {
                            if (item.score > limit && item.active)
                                result.add(item.name);
                        }
                        return result;
                    }
                    """,
                Score = 3
            },
            new ScoredExample
            {
                Code = """
                    /** Returns the names of the active users whose score exceeds {@code minScore}. */
                    public List<String> activeUsersAboveScore(List<User> users, int minScore) {
                        return users.stream()
                                .filter(user -> user.isActive() && user.getScore() > minScore)
                                .map(User::getName)
                                .toList();
                    }
                    """,
                Score = 5
            }
        ];

        /// <summary>
        /// The three examples for <paramref name="language"/>, worst first.
        /// </summary>
        ///
        /// <param name="language">
        /// The language's <see cref="Languages.ProgLang"/> name, such as <c>CSharp</c>.
        /// </param>
        public static IReadOnlyList<ScoredExample> For(string? language)
        {
            return language?.ToLowerInvariant() switch
            {
                "csharp" => CSharp,
                "java" => Java,
                _ => Python
            };
        }

        /// <summary>
        /// The excellent example for <paramref name="language"/>.
        /// </summary>
        public static ScoredExample ExcellentFor(string? language)
        {
            return For(language).MaxBy(example => example.Score)!;
        }
    }
}
