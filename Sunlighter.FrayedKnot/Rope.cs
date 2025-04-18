using Sunlighter.TypeTraitsLib;
using System.Collections.Immutable;
using System.Text;

namespace Sunlighter.FrayedKnot
{
    /// <summary>
    /// A Rope is a data structure that represents a string as a balanced tree of smaller strings. It is used to efficiently manipulate large strings.
    /// </summary>
    public sealed class Rope : IEquatable<Rope>, IComparable<Rope>
    {
        private const int MinLeafSize = 32;
        private const int MaxLeafSize = 64;

        private abstract class Node
        {
        }

        private class EmptyNode : Node
        {
            private static readonly EmptyNode value = new EmptyNode();

            private EmptyNode() { }

            public static EmptyNode Value => value;
        }

        private readonly struct NodeInfo
        {
            public NodeInfo(int length, int newlineCount, bool startsWithNewline, bool endsWithReturn)
            {
                Length = length;
                NewlineCount = newlineCount;
                StartsWithNewline = startsWithNewline;
                EndsWithReturn = endsWithReturn;
            }

            public int Length { get; }
            public int NewlineCount { get; }
            public bool StartsWithNewline { get; }
            public bool EndsWithReturn { get; }

            public static NodeInfo operator +(NodeInfo a, NodeInfo b)
            {
                int newlineCount = a.NewlineCount + b.NewlineCount;
                if (a.EndsWithReturn && b.StartsWithNewline)
                {
                    --newlineCount;
                }
                return new NodeInfo(a.Length + b.Length, newlineCount, a.StartsWithNewline, b.EndsWithReturn);
            }

            private static readonly NodeInfo zero = new NodeInfo(0, 0, false, false);

            public static NodeInfo Zero => zero;
        }

        private static NodeInfo GetNodeInfo(string str)
        {
            int newlineCount = 0;
            int pos = 0;
            int end = str.Length;
            while (pos < end)
            {
                if (str[pos] == '\n')
                {
                    ++newlineCount;
                    ++pos;
                }
                else if (str[pos] == '\r')
                {
                    ++newlineCount;
                    ++pos;
                    if (pos < end && str[pos] == '\n')
                    {
                        ++pos;
                    }
                }
                else
                {
                    ++pos;
                }
            }

            bool startsWithNewline = str.Length > 0 && (str[0] == '\n');
            bool endsWithReturn = str.Length > 0 && (str[^1] == '\r');

            return new NodeInfo(str.Length, newlineCount, startsWithNewline, endsWithReturn);
        }

        private static int LineOffset(string str, int startPos, int desiredLineNumber)
        {
            System.Diagnostics.Debug.Assert(str is not null);
            System.Diagnostics.Debug.Assert(startPos <= str.Length);
            System.Diagnostics.Debug.Assert(desiredLineNumber > 0);

            int newlineCount = 0;
            int pos = startPos;
            int end = str.Length;
            while (pos < end)
            {
                if (str[pos] == '\n')
                {
                    ++newlineCount;
                    ++pos;
                    if (newlineCount == desiredLineNumber)
                    {
                        return pos;
                    }
                }
                else if (str[pos] == '\r')
                {
                    ++newlineCount;
                    ++pos;
                    if (pos < end && str[pos] == '\n')
                    {
                        ++pos;
                    }
                    if (newlineCount == desiredLineNumber)
                    {
                        return pos;
                    }
                }
                else
                {
                    ++pos;
                }
            }
            return -1;
        }

        private abstract class NonEmptyNode : Node
        {
            protected readonly NodeInfo nodeInfo;

            protected NonEmptyNode(NodeInfo nodeInfo)
            {
                this.nodeInfo = nodeInfo;
            }

            public NodeInfo NodeInfo => nodeInfo;

            public virtual bool IsUnderfull => false;

            public abstract int Height { get; }
        }

        private class LeafNode : NonEmptyNode
        {
            private readonly string value;

            public LeafNode(string value)
                : base(GetNodeInfo(value))
            {
                System.Diagnostics.Debug.Assert(value != null, "String value cannot be null");
                System.Diagnostics.Debug.Assert(value.Length > 0, "Cannot make a leaf from an empty string");
                System.Diagnostics.Debug.Assert(value.Length <= MaxLeafSize, $"String (length {value.Length}) is too large to fit in a single leaf (length {MaxLeafSize}");
                this.value = value;
            }

            public LeafNode(string value, NodeInfo nodeInfo)
                : base(nodeInfo)
            {
                System.Diagnostics.Debug.Assert(value != null, "String value cannot be null");
                System.Diagnostics.Debug.Assert(value.Length > 0, "Cannot make a leaf from an empty string");
                System.Diagnostics.Debug.Assert(value.Length <= MaxLeafSize, $"String (length {value.Length}) is too large to fit in a single leaf (length {MaxLeafSize}");
                this.value = value;
            }

            public string Value => value;

            public override bool IsUnderfull => value.Length < MinLeafSize;

            public override int Height => 0;
        }

        private class TwoNode : NonEmptyNode
        {
            private readonly int height;
            private readonly NonEmptyNode left;
            private readonly NonEmptyNode right;

            public TwoNode(NonEmptyNode left, NonEmptyNode right)
                : base(left.NodeInfo + right.NodeInfo)
            {
                System.Diagnostics.Debug.Assert(left.Height == right.Height, "Left and right node heights must be equal");
                System.Diagnostics.Debug.Assert(!left.IsUnderfull, "Left node is underfull");
                System.Diagnostics.Debug.Assert(!right.IsUnderfull, "Right node is underfull");
                this.left = left;
                this.right = right;
                this.height = left.Height + 1;
            }

            public NonEmptyNode Left => left;
            public NonEmptyNode Right => right;

            public override int Height => height;
        }

        private class ThreeNode : NonEmptyNode
        {
            private readonly int height;
            private readonly NonEmptyNode left;
            private readonly NonEmptyNode middle;
            private readonly NonEmptyNode right;

            public ThreeNode(NonEmptyNode left, NonEmptyNode middle, NonEmptyNode right)
                : base(left.NodeInfo + middle.NodeInfo + right.NodeInfo)
            {
                System.Diagnostics.Debug.Assert(left.Height == middle.Height, "Left and middle node heights must be equal");
                System.Diagnostics.Debug.Assert(left.Height == right.Height, "Left and right node heights must be equal");
                System.Diagnostics.Debug.Assert(!left.IsUnderfull, "Left node is underfull");
                System.Diagnostics.Debug.Assert(!middle.IsUnderfull, "Middle node is underfull");
                System.Diagnostics.Debug.Assert(!right.IsUnderfull, "Right node is underfull");
                this.left = left;
                this.middle = middle;
                this.right = right;
                this.height = left.Height + 1;
            }

            public NonEmptyNode Left => left;
            public NonEmptyNode Middle => middle;
            public NonEmptyNode Right => right;

            public override int Height => height;
        }

        private readonly Node root;

        private Rope(Node root)
        {
            this.root = root;
        }

        private static readonly Rope empty = new Rope(EmptyNode.Value);

        /// <summary>
        /// Returns an empty Rope.
        /// </summary>
        public static Rope Empty => empty;

        /// <summary>
        /// Converts a string to a Rope.
        /// </summary>
        public static implicit operator Rope(string str)
        {
            ArgumentNullException.ThrowIfNull(str, nameof(str));

            if (str.Length == 0)
            {
                return empty;
            }

            NonEmptyNode makeNode(int begin, int end)
            {
                if ((end - begin) < MaxLeafSize)
                {
                    return new LeafNode(str.Substring(begin, end - begin));
                }
                else
                {
                    int mid = (begin + end) / 2;
                    NonEmptyNode left = makeNode(begin, mid);
                    NonEmptyNode right = makeNode(mid, end);

                    if (left.Height == right.Height)
                    {
                        return new TwoNode(left, right);
                    }
                    else if (left.Height == right.Height + 1)
                    {
                        if (left is TwoNode leftTwo)
                        {
                            return new ThreeNode(leftTwo.Left, leftTwo.Right, right);
                        }
                        else if (left is ThreeNode leftThree)
                        {
                            return new TwoNode(new TwoNode(leftThree.Left, leftThree.Middle), new TwoNode(leftThree.Right, right));
                        }
                        else
                        {
                            throw new InvalidOperationException("Internal error: Invalid node type");
                        }
                    }
                    else if (left.Height + 1 == right.Height)
                    {
                        if (right is TwoNode rightTwo)
                        {
                            return new ThreeNode(left, rightTwo.Left, rightTwo.Right);
                        }
                        else if (right is ThreeNode rightThree)
                        {
                            return new TwoNode(new TwoNode(left, rightThree.Left), new TwoNode(rightThree.Middle, rightThree.Right));
                        }
                        else
                        {
                            throw new InvalidOperationException("Internal error: Invalid node type");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Node heights differ by more than one");
                    }
                }
            }

            NonEmptyNode node = makeNode(0, str.Length);

            return new Rope(node);
        }

        /// <summary>
        /// Converts a Rope to a string.
        /// </summary>
        public static implicit operator string(Rope r)
        {
            ArgumentNullException.ThrowIfNull(r, nameof(r));
            if (r.root is EmptyNode)
            {
                return string.Empty;
            }
            else if (r.root is LeafNode leafNode)
            {
                return leafNode.Value;
            }
            else if (r.root is NonEmptyNode nonEmptyNode)
            {
                StringBuilder sb = new StringBuilder(nonEmptyNode.NodeInfo.Length);

                void visit(NonEmptyNode node)
                {
                    if (node is LeafNode leaf)
                    {
                        sb.Append(leaf.Value);
                    }
                    else if (node is TwoNode two)
                    {
                        visit(two.Left);
                        visit(two.Right);
                    }
                    else if (node is ThreeNode three)
                    {
                        visit(three.Left);
                        visit(three.Middle);
                        visit(three.Right);
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Invalid node type");
                    }
                }

                visit(nonEmptyNode);

                return sb.ToString();
            }
            else
            {
                throw new InvalidOperationException("Internal error: Invalid node type");
            }
        }

        /// <summary>
        /// Converts a Rope to a string.
        /// </summary>
        public override string ToString()
        {
            return (string)this;
        }

        private abstract class ConcatResult
        {

        }

        private sealed class ConcatResultOne : ConcatResult
        {
            private readonly NonEmptyNode one;

            public ConcatResultOne(NonEmptyNode one)
            {
                this.one = one;
            }

            public NonEmptyNode One => one;
        }

        private sealed class ConcatResultTwo : ConcatResult
        {
            private readonly NonEmptyNode one;
            private readonly NonEmptyNode two;

            public ConcatResultTwo(NonEmptyNode one, NonEmptyNode two)
            {
                this.one = one;
                this.two = two;
            }

            public NonEmptyNode One => one;
            public NonEmptyNode Two => two;
        }

        private static ConcatResult Concat(NonEmptyNode n1, NonEmptyNode n2)
        {
            if (n1.Height == n2.Height)
            {
                if (n1 is LeafNode n1Leaf && n2 is LeafNode n2Leaf)
                {
                    int totalLen = n1Leaf.NodeInfo.Length + n2Leaf.NodeInfo.Length;
                    if (totalLen > MaxLeafSize)
                    {
                        int mid = totalLen / 2;
                        string concat = n1Leaf.Value + n2Leaf.Value;
                        NonEmptyNode n1New = new LeafNode(concat.Substring(0, mid));
                        NonEmptyNode n2New = new LeafNode(concat.Substring(mid, totalLen - mid));

                        System.Diagnostics.Debug.Assert(!n1New.IsUnderfull, "New left node is underfull");
                        System.Diagnostics.Debug.Assert(!n2New.IsUnderfull, "New right node is underfull");

                        return new ConcatResultTwo(n1New, n2New);
                    }
                    else
                    {
                        return new ConcatResultOne(new LeafNode(n1Leaf.Value + n2Leaf.Value, n1Leaf.NodeInfo + n2Leaf.NodeInfo));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(n1 is not LeafNode && n2 is not LeafNode, "A non-leaf node cannot be the same height as a leaf node");
                    return new ConcatResultTwo(n1, n2);
                }
            }
            else if (n1.Height < n2.Height)
            {
                System.Diagnostics.Debug.Assert(n2 is not LeafNode, "A leaf node cannot have a greater height than any other node");

                if (n2 is TwoNode n2Two)
                {
                    ConcatResult cr = Concat(n1, n2Two.Left);
                    if (cr is ConcatResultOne cr1)
                    {
                        return new ConcatResultOne(new TwoNode(cr1.One, n2Two.Right));
                    }
                    else if (cr is ConcatResultTwo cr2)
                    {
                        return new ConcatResultOne(new ThreeNode(cr2.One, cr2.Two, n2Two.Right));
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Invalid concat result");
                    }
                }
                else if (n2 is ThreeNode n2Three)
                {
                    ConcatResult cr = Concat(n1, n2Three.Left);
                    if (cr is ConcatResultOne cr1)
                    {
                        return new ConcatResultOne(new ThreeNode(cr1.One, n2Three.Middle, n2Three.Right));
                    }
                    else if (cr is ConcatResultTwo cr2)
                    {
                        return new ConcatResultTwo(new TwoNode(cr2.One, cr2.Two), new TwoNode(n2Three.Middle, n2Three.Right));
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Invalid concat result");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Internal error: Invalid node type");
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(n1.Height > n2.Height);
                System.Diagnostics.Debug.Assert(n1 is not LeafNode, "A leaf node cannot have a greater height than any other node");

                if (n1 is TwoNode n1Two)
                {
                    ConcatResult cr = Concat(n1Two.Right, n2);
                    if (cr is ConcatResultOne cr1)
                    {
                        return new ConcatResultOne(new TwoNode(n1Two.Left, cr1.One));
                    }
                    else if (cr is ConcatResultTwo cr2)
                    {
                        return new ConcatResultOne(new ThreeNode(n1Two.Left, cr2.One, cr2.Two));
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Invalid concat result");
                    }
                }
                else if (n1 is ThreeNode n1Three)
                {
                    ConcatResult cr = Concat(n1Three.Right, n2);
                    if (cr is ConcatResultOne c1)
                    {
                        return new ConcatResultOne(new ThreeNode(n1Three.Left, n1Three.Middle, c1.One));
                    }
                    else if (cr is ConcatResultTwo c2)
                    {
                        return new ConcatResultTwo(new TwoNode(n1Three.Left, n1Three.Middle), new TwoNode(c2.One, c2.Two));
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Invalid concat result");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Internal error: Invalid node type");
                }
            }
        }

        /// <summary>
        /// Concatenates two ropes.
        /// </summary>
        public static Rope operator +(Rope a, Rope b)
        {
            if (a.IsEmpty && b.IsEmpty) return empty;
            if (a.IsEmpty) return b;
            if (b.IsEmpty) return a;

            NonEmptyNode ar = (NonEmptyNode)a.root;
            NonEmptyNode br = (NonEmptyNode)b.root;
            ConcatResult cr = Concat(ar, br);
            if (cr is ConcatResultOne cr1)
            {
                return new Rope(cr1.One);
            }
            else if (cr is ConcatResultTwo cr2)
            {
                return new Rope(new TwoNode(cr2.One, cr2.Two));
            }
            else
            {
                throw new InvalidOperationException("Internal error: Invalid concat result");
            }
        }

        /// <summary>
        /// Returns true if and only if this Rope is empty.
        /// </summary>
        public bool IsEmpty => root is EmptyNode;

        /// <summary>
        /// Returns the number of characters in this Rope.
        /// </summary>
        public int Length
        {
            get
            {
                if (root is EmptyNode)
                {
                    return 0;
                }
                else if (root is NonEmptyNode nonEmptyNode)
                {
                    return nonEmptyNode.NodeInfo.Length;
                }
                else
                {
                    throw new InvalidOperationException("Internal error: Invalid node type");
                }
            }
        }

        /// <summary>
        /// Returns the number of lines in this Rope. There is always at least one. The last line doesn't end in a newline character.
        /// </summary>
        public int LineCount
        {
            get
            {
                if (root is EmptyNode)
                {
                    return 1;
                }
                else if (root is NonEmptyNode nonEmptyNode)
                {
                    return 1 + nonEmptyNode.NodeInfo.NewlineCount;
                }
                else
                {
                    throw new InvalidOperationException("Internal error: Invalid node type");
                }
            }
        }

        private static (ImmutableStack<NonEmptyNode>, ImmutableStack<NonEmptyNode>) Split(NonEmptyNode root, int charCount)
        {
            ImmutableStack<NonEmptyNode> taken = ImmutableStack<NonEmptyNode>.Empty;
            ImmutableStack<NonEmptyNode> untaken = ImmutableStack<NonEmptyNode>.Empty.Push(root);

            while (!untaken.IsEmpty && charCount > 0)
            {
                if (untaken.Peek().NodeInfo.Length <= charCount)
                {
                    NonEmptyNode n = untaken.Peek();
                    taken = taken.Push(n);
                    untaken = untaken.Pop();
                    charCount -= n.NodeInfo.Length;
                }
                else
                {
                    NonEmptyNode node = untaken.Peek();
                    untaken = untaken.Pop();
                    if (node is LeafNode leaf)
                    {
                        string leftStr = leaf.Value[..charCount];
                        string rightStr = leaf.Value[charCount..];

                        System.Diagnostics.Debug.Assert(leftStr.Length > 0);
                        System.Diagnostics.Debug.Assert(rightStr.Length > 0);

                        taken = taken.Push(new LeafNode(leftStr));
                        untaken = untaken.Push(new LeafNode(rightStr));
                        charCount = 0;
                    }
                    else if (node is TwoNode two)
                    {
                        untaken = untaken.Push(two.Right);
                        untaken = untaken.Push(two.Left);
                    }
                    else if (node is ThreeNode three)
                    {
                        untaken = untaken.Push(three.Right);
                        untaken = untaken.Push(three.Middle);
                        untaken = untaken.Push(three.Left);
                    }
                }
            }

            return (taken, untaken);
        }

        /// <summary>
        /// Returns a new Rope created by skipping the first <paramref name="charCount"/> characters of this Rope.
        /// </summary>
        public Rope Skip(int charCount)
        {
            if (charCount < 0) throw new ArgumentException($"{nameof(charCount)} cannot be negative", nameof(charCount));

            if (root is EmptyNode)
            {
                return this;
            }
            else if (charCount == 0) return this;
            else if (charCount >= Length)
            {
                return empty;
            }
            else
            {
                (ImmutableStack<NonEmptyNode> _, ImmutableStack<NonEmptyNode> untaken) = Split((NonEmptyNode)root, charCount);

                System.Diagnostics.Debug.Assert(!untaken.IsEmpty, "Untaken part is unexpectedly empty");

                NonEmptyNode result = untaken.Peek();
                untaken = untaken.Pop();
                while (!untaken.IsEmpty)
                {
                    NonEmptyNode n2 = untaken.Peek();
                    untaken = untaken.Pop();
                    ConcatResult cr = Concat(result, n2);
                    if (cr is ConcatResultOne cr1)
                    {
                        result = cr1.One;
                    }
                    else if (cr is ConcatResultTwo cr2)
                    {
                        result = new TwoNode(cr2.One, cr2.Two);
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Invalid concat result");
                    }
                }
                return new Rope(result);
            }
        }

        /// <summary>
        /// Returns a new Rope created by taking only the first <paramref name="charCount"/> characters of this Rope.
        /// </summary>
        public Rope Take(int charCount)
        {
            if (charCount < 0) throw new ArgumentException($"{nameof(charCount)} cannot be negative", nameof(charCount));

            if (root is EmptyNode)
            {
                return this;
            }
            else if (charCount == 0) return empty;
            else if (charCount >= Length)
            {
                return this;
            }
            else
            {
                (ImmutableStack<NonEmptyNode> taken, ImmutableStack<NonEmptyNode> _) = Split((NonEmptyNode)root, charCount);

                System.Diagnostics.Debug.Assert(!taken.IsEmpty, "Taken part is unexpectedly empty");

                NonEmptyNode result = taken.Peek();
                taken = taken.Pop();
                while (!taken.IsEmpty)
                {
                    NonEmptyNode n2 = taken.Peek();
                    taken = taken.Pop();
                    ConcatResult cr = Concat(n2, result);
                    if (cr is ConcatResultOne cr1)
                    {
                        result = cr1.One;
                    }
                    else if (cr is ConcatResultTwo cr2)
                    {
                        result = new TwoNode(cr2.One, cr2.Two);
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Invalid concat result");
                    }
                }
                return new Rope(result);
            }
        }

        private static int LineOffset(NonEmptyNode root, int desiredLineNumber)
        {
            NodeInfo taken = NodeInfo.Zero;
            ImmutableStack<NonEmptyNode> untaken = ImmutableStack<NonEmptyNode>.Empty.Push(root);

            while (!untaken.IsEmpty && desiredLineNumber > taken.NewlineCount)
            {
                NonEmptyNode n = untaken.Peek();
                NodeInfo pastN = taken + n.NodeInfo;

                if (desiredLineNumber > pastN.NewlineCount)
                {
                    taken = pastN;
                    untaken = untaken.Pop();
                }
                else if (n is LeafNode leaf)
                {
                    untaken = untaken.Pop();
                    string str = leaf.Value;

                    System.Diagnostics.Debug.Assert(str.Length > 0);
                    int startPos = (taken.EndsWithReturn && leaf.NodeInfo.StartsWithNewline) ? 1 : 0;
                    int endPos = LineOffset(str, startPos, desiredLineNumber - taken.NewlineCount);
                    if (endPos > 0)
                    {
                        if (endPos == str.Length && str[^1] == '\r' && !untaken.IsEmpty && untaken.Peek().NodeInfo.StartsWithNewline)
                        {
                            return taken.Length + endPos + 1;
                        }
                        else
                        {
                            return taken.Length + endPos;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Leaf eaten! (desiredLineNumber = {desiredLineNumber}");
                        taken = pastN;
                    }
                }
                else if (n is TwoNode two)
                {
                    untaken = untaken.Pop().Push(two.Right).Push(two.Left);
                }
                else if (n is ThreeNode three)
                {
                    untaken = untaken.Pop().Push(three.Right).Push(three.Middle).Push(three.Left);
                }
                else
                {
                    throw new InvalidOperationException("Internal error: Invalid node type");
                }
            }

            //System.Diagnostics.Debug.Assert(false);
            return -1;
        }

        /// <summary>
        /// Returns the character offset of the start of the specified line, or -1 if the line does not exist.
        /// </summary>

        public int LineOffset(int line)
        {
            if (line < 0) throw new ArgumentException($"{nameof(line)} cannot be negative", nameof(line));
            
            if (line == 0)
            {
                return 0;
            }
            else if (root is EmptyNode)
            {
                return -1;
            }
            else
            {
                return LineOffset((NonEmptyNode)root, line);
            }
        }

        /// <summary>
        /// Returns a new Rope created by skipping the first <paramref name="lineCount"/> lines of this Rope.
        /// </summary>

        public Rope SkipLines(int lineCount)
        {
            int offset = LineOffset(lineCount);
            if (offset > 0)
            {
                return Skip(offset);
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Returns a new Rope created by taking only the first <paramref name="lineCount"/> lines of this Rope. An empty line is added to the end!
        /// </summary>
        public Rope TakeLines(int lineCount)
        {
            int offset = LineOffset(lineCount);
            if (offset > 0)
            {
                return Take(offset);
            }
            else
            {
                return empty;
            }
        }

        /// <summary>
        /// Returns true only if this Rope equals the specified object.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is Rope r)
            {
                return ropeTraits.Compare(this, r) == 0;
            }
            else return false;
        }

        /// <summary>
        /// Returns a hash code for this rope.
        /// </summary>
        public override int GetHashCode()
        {
            return ropeTraits.GetBasicHashCode(this);
        }

        /// <summary>
        /// Returns true only if this Rope equals the specified Rope.
        /// </summary>
        public bool Equals(Rope? other)
        {
            if (other is null) return false;
            return ropeTraits.Compare(this, other) == 0;
        }

        /// <summary>
        /// Compares the content of this rope to the given Rope.
        /// </summary>
        public int CompareTo(Rope? other)
        {
            if (other is null) return 1;
            return ropeTraits.Compare(this, other);
        }

        /// <summary>
        /// Gets the character at the specified index.
        /// </summary>
        public char this[int index]
        {
            get
            {
                if (index < 0) throw new IndexOutOfRangeException(nameof(index));
                if (root is EmptyNode) throw new IndexOutOfRangeException(nameof(index));

                NonEmptyNode nRoot = (NonEmptyNode)root;
                if (index >= nRoot.NodeInfo.Length) throw new IndexOutOfRangeException(nameof(index));

                static char GetChar(NonEmptyNode node, int index)
                {
                    if (node is LeafNode leaf)
                    {
                        return leaf.Value[index];
                    }
                    else if (node is TwoNode two)
                    {
                        int leftLen = two.Left.NodeInfo.Length;
                        if (index < leftLen)
                        {
                            return GetChar(two.Left, index);
                        }
                        else
                        {
                            return GetChar(two.Right, index - leftLen);
                        }
                    }
                    else if (node is ThreeNode three)
                    {
                        int leftLen = three.Left.NodeInfo.Length;
                        if (index < leftLen)
                        {
                            return GetChar(three.Left, index);
                        }
                        else
                        {
                            index -= leftLen;
                            int midLen = three.Middle.NodeInfo.Length;
                            if (index < midLen)
                            {
                                return GetChar(three.Middle, index);
                            }
                            else
                            {
                                return GetChar(three.Right, index - midLen);
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Invalid node type");
                    }
                }

                return GetChar(nRoot, index);
            }
        }

        private class LeafReader
        {
            private ImmutableStack<NonEmptyNode> nodeStack;
            private ImmutableStack<string> leafStack;

            public LeafReader(NonEmptyNode n)
            {
                nodeStack = ImmutableStack<NonEmptyNode>.Empty;
                leafStack = ImmutableStack<string>.Empty;

                Push(n);
            }

            private void Push(NonEmptyNode n)
            {
                if (n is LeafNode leaf)
                {
                    leafStack = leafStack.Push(leaf.Value);
                }
                else
                {
                    nodeStack = nodeStack.Push(n);
                }
            }

            private void Normalize()
            {
                if (nodeStack.IsEmpty) return;

                if (!leafStack.IsEmpty) return;

                while (leafStack.IsEmpty)
                {
                    NonEmptyNode n = nodeStack.Peek();
                    nodeStack = nodeStack.Pop();
                    if (n is TwoNode two)
                    {
                        Push(two.Right);
                        Push(two.Left);
                    }
                    else if (n is ThreeNode three)
                    {
                        Push(three.Right);
                        Push(three.Middle);
                        Push(three.Left);
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Invalid node type");
                    }
                }
            }

            public bool IsEmpty => nodeStack.IsEmpty;

            public int LeafSize()
            {
                if (nodeStack.IsEmpty) return 0;
                Normalize();
                return leafStack.Peek().Length;
            }

            public string Consume(int i)
            {
                if (nodeStack.IsEmpty) throw new InvalidOperationException();
                Normalize();
                int avail = leafStack.Peek().Length;

                if (i > avail) throw new InvalidOperationException("Attempt to consume too many characters");
                else if (i == avail)
                {
                    string result = leafStack.Peek();
                    leafStack = leafStack.Pop();
                    return result;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(i < avail);

                    string leafStackTop = leafStack.Peek();
                    leafStack = leafStack.Pop();
                    string result = leafStackTop.Substring(0, i);
                    leafStack = leafStack.Push(leafStackTop.Substring(i));
                    return result;
                }
            }
        }

        private class RopeTraits : ITypeTraits<Rope>
        {
            public RopeTraits() { }

            public int Compare(Rope a, Rope b)
            {
                if (a.IsEmpty && b.IsEmpty) return 0;
                if (a.IsEmpty) return 1;
                if (b.IsEmpty) return -1;

                LeafReader ar = new LeafReader((NonEmptyNode)a.root);
                LeafReader br = new LeafReader((NonEmptyNode)b.root);

                while(true)
                {
                    int nextLen = Math.Min(ar.LeafSize(), br.LeafSize());
                    string aStr = ar.Consume(nextLen);
                    string bStr = br.Consume(nextLen);
                    int cmp = string.Compare(aStr, bStr, StringComparison.Ordinal);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                    else
                    {
                        if (ar.IsEmpty && br.IsEmpty) return 0;
                        if (ar.IsEmpty) return -1;
                        if (br.IsEmpty) return 1;
                    }
                }
            }

            private const int blockSize = 16384;

            public void AddToHash(HashBuilder b, Rope a)
            {
                int blockCount = a.Length / blockSize;
                if (a.Length % blockSize != 0)
                {
                    ++blockCount;
                }
                for (int i = 0; i < blockCount; ++i)
                {
                    string aPrefix = a.Take(blockSize).ToString();
                    a = a.Skip(blockSize);
                    StringTypeTraits.Value.AddToHash(b, aPrefix);
                }
            }

            public bool CanSerialize(Rope a) => true;


            public void Serialize(Serializer dest, Rope a)
            {
                int blockCount = a.Length / blockSize;
                if (a.Length % blockSize != 0)
                {
                    ++blockCount;
                }
                Int32TypeTraits.Value.Serialize(dest, blockCount);
                for(int i = 0; i < blockCount; ++i)
                {
                    string aPrefix = a.Take(blockSize).ToString();
                    a = a.Skip(blockSize);
                    StringTypeTraits.Value.Serialize(dest, aPrefix);
                }
            }

            public Rope Deserialize(Deserializer src)
            {
                int blockCount = Int32TypeTraits.Value.Deserialize(src);
                Rope result = empty;
                for(int i = 0; i < blockCount; ++i)
                {
                    string aPrefix = StringTypeTraits.Value.Deserialize(src);
                    result += aPrefix;
                }
                return result;
            }

            public void MeasureBytes(ByteMeasurer measurer, Rope a)
            {
                int blockCount = a.Length / blockSize;
                if (a.Length % blockSize != 0)
                {
                    ++blockCount;
                }
                Int32TypeTraits.Value.MeasureBytes(measurer, blockCount);
                for (int i = 0; i < blockCount; ++i)
                {
                    string aPrefix = a.Take(blockSize).ToString();
                    a = a.Skip(blockSize);
                    StringTypeTraits.Value.MeasureBytes(measurer, aPrefix);
                }
            }

            public void AppendDebugString(DebugStringBuilder sb, Rope a)
            {
                if (a.IsEmpty)
                {
                    sb.Builder.Append("(empty-rope)");
                }
                else
                {
                    LeafReader ar = new LeafReader((NonEmptyNode)a.root);
                    sb.Builder.Append("(rope ");
                    while (!ar.IsEmpty)
                    {
                        int nextLen = ar.LeafSize();
                        string aStr = ar.Consume(nextLen);
                        sb.Builder.AppendQuoted(aStr);
                        if (!ar.IsEmpty)
                        {
                            sb.Builder.Append(", ");
                        }
                    }
                    sb.Builder.Append(')');
                }
            }
        }

        private static readonly RopeTraits ropeTraits = new RopeTraits();

        /// <summary>
        /// Gets the Type Traits for the Rope class.
        /// </summary>
        public static ITypeTraits<Rope> TypeTraits => ropeTraits;

        /// <summary>
        /// Return true if the two Ropes are equal, and false otherwise.
        /// </summary>
        public static bool operator ==(Rope a, Rope b) => ropeTraits.Compare(a, b) == 0;

        /// <summary>
        /// Return true if the two Ropes are not equal, and false otherwise.
        /// </summary>
        public static bool operator !=(Rope a, Rope b) => ropeTraits.Compare(a, b) != 0;

        /// <summary>
        /// Return true if the first Rope is less than the second Rope, and false otherwise.
        /// </summary>
        public static bool operator <(Rope a, Rope b) => ropeTraits.Compare(a, b) < 0;

        /// <summary>
        /// Return true if the first Rope is greater than the second Rope, and false otherwise.
        /// </summary>
        public static bool operator >(Rope a, Rope b) => ropeTraits.Compare(a, b) > 0;

        /// <summary>
        /// Return true if the first Rope is less than or equal to the second Rope, and false otherwise.
        /// </summary>
        public static bool operator <=(Rope a, Rope b) => ropeTraits.Compare(a, b) <= 0;

        /// <summary>
        /// Return true if the first Rope is greater than or equal to the second Rope, and false otherwise.
        /// </summary>
        public static bool operator >=(Rope a, Rope b) => ropeTraits.Compare(a, b) >= 0;
    }

    /// <summary>
    /// Utility methods for working with Ropes.
    /// </summary>
    public static class RopeUtility
    {
        private const int bufSize = 16384;

        /// <summary>
        /// Reads a Rope from the specified TextReader until the end of the stream.
        /// </summary>
        public static Rope ReadRopeToEnd(this TextReader textReader)
        {
            ArgumentNullException.ThrowIfNull(textReader, nameof(textReader));
            Rope r = Rope.Empty;

            while(true)
            {
                char[] buffer = new char[bufSize];
                int actuallyRead = textReader.Read(buffer, 0, buffer.Length);

                if (actuallyRead < buffer.Length)
                {
                    if (actuallyRead > 0)
                    {
                        string str = new string(buffer, 0, actuallyRead);
                        r += str;
                    }
                    return r;
                }
                else
                {
                    string str = new string(buffer);
                    r += str;
                }
            }
        }

        /// <summary>
        /// Writes the specified Rope to the given TextWriter.
        /// </summary>
        public static void WriteRope(this TextWriter textWriter, Rope r)
        {
            ArgumentNullException.ThrowIfNull(textWriter, nameof(textWriter));
            while (true)
            {
                string prefix = r.Take(bufSize);
                r = r.Skip(bufSize);
                textWriter.Write(prefix);
                if (r.IsEmpty) break;
            }
        }

        /// <summary>
        /// Reads the contents of the given file into a Rope.
        /// </summary>
        public static Rope ReadRopeFromFile(string fileName, Encoding? encoding = null)
        {
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
            using StreamReader sr = new StreamReader(fileName, encoding ?? Encoding.UTF8);
            return sr.ReadRopeToEnd();
        }

        /// <summary>
        /// Creates or overwrites the given file with the given Rope.
        /// </summary>
        public static void WriteRopeToFile(string fileName, Rope r, Encoding? encoding = null)
        {
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
            using StreamWriter sw = new StreamWriter(fileName, false, encoding ?? Encoding.UTF8);
            sw.WriteRope(r);
        }
    }
}
