using Sunlighter.FrayedKnot;
using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System.Collections.Immutable;
using System.Text;

namespace FrayedKnotTests
{
    [TestClass]
    public sealed class RopeTests
    {
        private static Rope CreateTestRope(Random rand, int numberOfLines)
        {
            string[] theNewlines = ["\n", "\r", "\r\n"];

            Rope r1 = Rope.Empty;

            foreach (int i in Enumerable.Range(0, numberOfLines))
            {
                int len = rand.Next(80) + 1;
                string s0 = new string('-', len) + theNewlines[rand.Next(theNewlines.Length)];
                string s1 = i.ToString();
                if (s1.Length + 4 < s0.Length)
                {
                    s0 = string.Concat("--", s1, s0.AsSpan(2 + s1.Length));
                }
                r1 += s0;
            }

            return r1;
        }

        [TestMethod]
        public void TestLineIndexing()
        {
            Random rand = new Random(0x377E124F);
            int count = 1000;
            Rope r = CreateTestRope(rand, count);

            bool hasNewlineCharsOnlyAtEnd(string str)
            {
                int i = 0;
                int iEnd = str.Length;
                int state = 0;

                // state 0: haven't seen \r or \n yet
                // state 10: saw \r
                // state 20: saw \n or \r\n
                // state 30: saw any regular character after \r or \n

                while (i < iEnd)
                {
                    char c = str[i];
                    int cType;

                    if (c == '\r') cType = 1;
                    else if (c == '\n') cType = 2;
                    else cType = 0;

                    switch(state + cType)
                    {
                        case 0: // state 0 and regular char
                            break;
                        case 1: // state 0 and \r
                            state = 10;
                            break;
                        case 2: // state 0 and \n
                            state = 20;
                            break;
                        case 10: // state 10 and regular char
                        case 11: // state 10 and \r
                            state = 30;
                            break;
                        case 12: // state 10 and \n
                            state = 20;
                            break;
                        case 20: // state 20 and regular char
                        case 21: // state 20 and \r
                        case 22: // state 20 and \n
                            state = 30;
                            break;
                        case 30: // state 30 and regular char
                        case 31: // state 30 and \r
                        case 32: // state 30 and \n
                            break;
                    }
                    ++i;
                }

                bool endsWithNewline = state == 10 || state == 20;
                return endsWithNewline;
            }

            ImmutableList<(int, string)> errors = Enumerable.Range(0, count)
                .Select(i => (i, (string)(r.SkipLines(i).TakeLines(1))))
                .Where(rec => !hasNewlineCharsOnlyAtEnd(rec.Item2))
                .ToImmutableList();

            Assert.AreEqual(0, errors.Count, $"Rope has newlines in the middle of lines: {string.Join(", ", errors.Take(5).Select(rec => $"({rec.Item1}: {rec.Item2.Quoted()})"))}");
        }

        private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private static Rope CreateRandomCharRope(Random rand, int numberOfChars)
        {
            Rope result = Rope.Empty;
            while(numberOfChars > 0)
            {
                int pass = Math.Min(numberOfChars, 192 + rand.Next(64));
                StringBuilder sb = new StringBuilder(pass);
                for (int i = 0; i < pass; ++i)
                {
                    sb.Append(alphabet[rand.Next(alphabet.Length)]);
                }
                result += sb.ToString();
                numberOfChars -= pass;
            }
            return result;
        }

        [TestMethod]
        public void TestComparison()
        {
            Random rand = new Random(0x377E124F);
            int count = 1000;

            ImmutableSortedSet<Rope> theSet = ImmutableSortedSet<Rope>.Empty;

            for (int i = 0; i < count; ++i)
            {
                Rope r = CreateRandomCharRope(rand, 501 + rand.Next(500));
                theSet = theSet.Add(r);
            }

            ImmutableList<(int, string, string)> failures = Enumerable.Range(0, theSet.Count - 1)
                .Select(i => (i, theSet[i].ToString(), theSet[i + 1].ToString()))
                .Where(rec => string.Compare(rec.Item2, rec.Item3, StringComparison.Ordinal) >= 0)
                .ToImmutableList();

            Assert.AreEqual(0, failures.Count, $"Ropes are not sorted: {string.Join(", ", failures.Take(5).Select(rec => $"({rec.Item1}: {rec.Item2.Quoted()} < {rec.Item3.Quoted()})"))}");
        }

        [TestMethod]
        public void TestLineIndexing2()
        {
            Random rand = new Random(0x377E124F);
            int count = 5000;
            Rope r = CreateTestRope(rand, count);

            ImmutableSortedDictionary<int, (int, int, int)> results = ImmutableSortedDictionary<int, (int, int, int)>.Empty;

            for (int i = 0; i < 1000; ++i)
            {
                int randomCharOffset = rand.Next(r.Length);
                int lineNumberForOffset = r.LineNumberForOffset(randomCharOffset);
                int charOffsetForLine = r.LineOffset(lineNumberForOffset);
                int charOffsetForNextLine = r.LineOffset(lineNumberForOffset + 1);

                Assert.IsTrue(charOffsetForLine <= randomCharOffset, $"Line offset {charOffsetForLine} for lineNumber {lineNumberForOffset} is greater than random char offset {randomCharOffset}.");
                Assert.IsTrue(randomCharOffset < charOffsetForNextLine, $"Random char offset {randomCharOffset} is greater than lineNumber offset {charOffsetForNextLine} for lineNumber {lineNumberForOffset + 1}.");

                results = results.Add(randomCharOffset, (lineNumberForOffset, charOffsetForLine, charOffsetForNextLine));
            }
        }

        [TestMethod]
        public void TestLineIndexing3()
        {
            Random rand = new Random(0x2B1F3987);
            int count = 5000;
            Rope r = CreateTestRope(rand, count);

            for (int i = 0; i < 1000; ++i)
            {
                int lineNumber = rand.Next(r.LineCount);
                int offset = r.LineOffset(lineNumber);
                int lineNumberForOffset = r.LineNumberForOffset(offset);

                Assert.AreEqual(lineNumber, lineNumberForOffset, $"Fail: lineNumber = {lineNumber}, offset = {offset}, lineNumberForOffset = {lineNumberForOffset}");

                int offsetNext = r.LineOffset(lineNumber + 1);
                int lineNumberForOffsetNext = r.LineNumberForOffset(offsetNext - 1);

                Assert.AreEqual(lineNumber, lineNumberForOffsetNext, $"Fail: lineNumber = {lineNumber}, offset = {offset}, offsetNext = {offsetNext}, lineNumberForOffsetNext = {lineNumberForOffsetNext}");
            }
        }

        [TestMethod]
        public void TestComparison2()
        {
            Random rand = new Random(0x377E124F);
            int countPerSet = 10;
            int numberOfSets = 100;

            Adapter<ImmutableSortedSet<Rope>> a = Builder.Instance.GetAdapter<ImmutableSortedSet<Rope>>();

            ImmutableSortedSet<ImmutableSortedSet<Rope>> hyperSet = ImmutableSortedSet<ImmutableSortedSet<Rope>>.Empty.WithComparer(a);

            for (int i = 0; i < numberOfSets; ++i)
            {
                ImmutableSortedSet<Rope> theSet = ImmutableSortedSet<Rope>.Empty;
                for (int j = 0; j < countPerSet; ++j)
                {
                    Rope r = CreateRandomCharRope(rand, 501 + rand.Next(500));
                    theSet = theSet.Add(r);
                }
                hyperSet = hyperSet.Add(theSet);
            }

            Assert.AreEqual(numberOfSets, hyperSet.Count, "Hyper set should contain the expected number of sets.");
        }
    }
}
