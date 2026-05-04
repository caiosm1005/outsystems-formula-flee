namespace Flee.Tests.ExtensionMethodTests
{
    internal class TestData
    {
        public required string Id { get; set; }

        public TestData Sub => new() { Id = "Sub" + Id };

        public string SayHello(int times)
        {
            string result = string.Empty;
            for (int i = 0; i < times; i++)
            {
                result += "hello ";
            }

            return result + Id;
        }

        /// <summary>
        /// A bug previous meant a small difference in
        /// parameters was not detected and treated
        /// as ambiguous. The parameter values are not inspected; only their
        /// types matter, since these overloads exist to test that the binder
        /// picks the correct one based on argument types.
        /// </summary>
        public string MatchParams(uint x, int y, int z)
        {
            return Tag("UII", x, y, z);
        }

        public string MatchParams(int x, int y, int z)
        {
            return Tag("III", x, y, z);
        }

        public string MatchParams(float x, float y, double z)
        {
            return Tag("FFD", x, y, z);
        }

        public string MatchParams(double x, double y, double z)
        {
            return Tag("DDD", x, y, z);
        }

        private static string Tag(string label, object _1, object _2, object _3)
        {
            return label;
        }
    }
}
