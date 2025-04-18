﻿using Sunlighter.FrayedKnot;
using Sunlighter.TypeTraitsLib;
using System.Collections.Immutable;

namespace FrayedKnotTests
{
    [TestClass]
    public sealed class Test1
    {
        private static Rope CreateTestRope(Random rand, int numberOfLines)
        {
            string[] theNewlines = ["\n", "\r", "\r\n"];

            Rope r1 = Rope.Empty;

            foreach (int i in Enumerable.Range(0, 1000))
            {
                int len = rand.Next(80) + 1;
                string s0 = new string('-', len) + theNewlines[rand.Next(theNewlines.Length)];
                string s1 = i.ToString();
                if (s1.Length + 4 < s0.Length)
                {
                    s0 = string.Concat("--", s1, s0.AsSpan(2 + s1.Length));
                }
                r1 = r1 + s0;
            }

            return r1;
        }

        [TestMethod]
        public void TestMethod1()
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
    }
}
