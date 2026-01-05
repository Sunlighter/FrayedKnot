using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Sunlighter.FrayedKnot
{
    /// <summary>
    /// A RopeAnnotationList is an immutable data structure that keeps annotations at positions and tracks them across insertions and deletions of space.
    /// </summary>
    [ProvidesOwnTypeTraits]
    public class RopeAnnotationList<T>
    {
        private readonly Node root;
        private readonly int trailingSpace;

        private RopeAnnotationList(Node root, int trailingSpace)
        {
            this.root = root;
            this.trailingSpace = trailingSpace;
        }

        private static readonly RopeAnnotationList<T> empty = new RopeAnnotationList<T>(EmptyNode.Value, 0);

        /// <summary>
        /// An empty RopeAnnotationList.
        /// </summary>
        public static RopeAnnotationList<T> Empty => empty;

        /// <summary>
        /// Returns a new rope annotation list consisting only of the given amount of empty space.
        /// </summary>
        /// <param name="length">Amount of empty space</param>
        /// <returns>The new rope annotation list</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if length is negative.</exception>
        public static RopeAnnotationList<T> Space(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
            }
            return new RopeAnnotationList<T>(EmptyNode.Value, length);
        }

        /// <summary>
        /// Returns a new rope annotation list consisting only of the given item, at position 0, with no following space.
        /// </summary>
        /// <param name="value">The item to include.</param>
        /// <returns>The new rope annotation list.</returns>
        public static RopeAnnotationList<T> Item(T value)
        {
            return new RopeAnnotationList<T>(new Leaf(0, value), 0);
        }

        private abstract class Node
        {

        }

        private sealed class EmptyNode : Node
        {
            public static readonly EmptyNode Value = new EmptyNode();

            private EmptyNode()
            {
            }
        }

        private readonly struct NodeInfo
        {
            private readonly int length;
            private readonly int count;

            public NodeInfo(int length, int count)
            {
                this.length = length;
                this.count = count;
            }

            public int Length => length;
            public int Count => count;

            public static NodeInfo operator + (NodeInfo a, NodeInfo b)
            {
                return new NodeInfo(a.Length + b.Length, a.Count + b.Count);
            }
        }

        private abstract class NonEmptyNode : Node
        {
            public abstract NodeInfo Info { get; }
            public abstract int Height { get; }
        }

        private sealed class Leaf : NonEmptyNode
        {
            private readonly int spaceBefore;
            private readonly T item;

            public Leaf(int spaceBefore, T item)
            {
                this.spaceBefore = spaceBefore;
                this.item = item;
            }

            public T Item => item;

            public int SpaceBefore => spaceBefore;

            public override NodeInfo Info => new NodeInfo(spaceBefore, 1);

            public override int Height => 0;
        }

        private sealed class TwoNode : NonEmptyNode
        {
            private readonly NonEmptyNode left;
            private readonly NonEmptyNode right;
            private readonly NodeInfo info;
            private int height;

            public TwoNode(NonEmptyNode left, NonEmptyNode right)
            {
                System.Diagnostics.Debug.Assert(left.Height == right.Height, "Left and right nodes of a TwoNode must have the same height.");
                this.left = left;
                this.right = right;
                this.info = left.Info + right.Info;
                this.height = left.Height + 1;
            }

            public NonEmptyNode Left => left;
            public NonEmptyNode Right => right;

            public override NodeInfo Info => info;

            public override int Height => height;

        }

        private sealed class ThreeNode : NonEmptyNode
        {
            private readonly NonEmptyNode left;
            private readonly NonEmptyNode middle;
            private readonly NonEmptyNode right;
            private readonly NodeInfo info;
            private readonly int height;

            public ThreeNode(NonEmptyNode left, NonEmptyNode middle, NonEmptyNode right)
            {
                System.Diagnostics.Debug.Assert(left.Height == right.Height, "Left and right nodes of a ThreeNode must have the same height.");
                System.Diagnostics.Debug.Assert(left.Height == middle.Height, "Left and middle nodes of a ThreeNode must have the same height.");

                this.left = left;
                this.middle = middle;
                this.right = right;
                this.info = left.Info + middle.Info + right.Info;
                this.height = left.Height + 1;
            }

            public NonEmptyNode Left => left;
            public NonEmptyNode Middle => middle;
            public NonEmptyNode Right => right;

            public override NodeInfo Info => info;

            public override int Height => height;
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

        private static NonEmptyNode Concat(int leftSpace, NonEmptyNode right)
        {
            System.Diagnostics.Debug.Assert(leftSpace >= 0);

            if (leftSpace == 0)
            {
                return right;
            }

            if (right is Leaf l)
            {
                return new Leaf(leftSpace + l.SpaceBefore, l.Item);
            }
            else if (right is TwoNode twoNode)
            {
                return new TwoNode(Concat(leftSpace, twoNode.Left), twoNode.Right);
            }
            else if (right is ThreeNode threeNode)
            {
                return new ThreeNode(Concat(leftSpace, threeNode.Left), threeNode.Middle, threeNode.Right);
            }
            else
            {
                throw new InvalidOperationException($"Internal error: Unknown NonEmptyNode type");
            }
        }

        private static ConcatResult Concat(NonEmptyNode left, int middleSpace, NonEmptyNode right)
        {
            if (left.Height == right.Height)
            {
                return new ConcatResultTwo(left, Concat(middleSpace, right));
            }
            else if (left.Height < right.Height)
            {
                if (right is TwoNode rightTwo)
                {
                    var concat = Concat(left, middleSpace, rightTwo.Left);
                    if (concat is ConcatResultOne concatOne)
                    {
                        return new ConcatResultOne(new TwoNode(concatOne.One, rightTwo.Right));
                    }
                    else if (concat is ConcatResultTwo concatTwo)
                    {
                        return new ConcatResultOne(new ThreeNode(concatTwo.One, concatTwo.Two, rightTwo.Right));
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Unknown ConcatResult type");
                    }
                }
                else if (right is ThreeNode rightThree)
                {
                    var concat = Concat(left, middleSpace, rightThree.Left);
                    if (concat is ConcatResultOne concatOne)
                    {
                        return new ConcatResultOne(new ThreeNode(concatOne.One, rightThree.Middle, rightThree.Right));
                    }
                    else if (concat is ConcatResultTwo concatTwo)
                    {
                        return new ConcatResultTwo(new TwoNode(concatTwo.One, concatTwo.Two), new TwoNode(rightThree.Middle, rightThree.Right));
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Unknown ConcatResult type");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Internal error: Unknown NonEmptyNode type");
                }
            }
            else // left.Height > right.Height
            {
                if (left is TwoNode leftTwo)
                {
                    var concat = Concat(leftTwo.Right, middleSpace, right);
                    if (concat is ConcatResultOne concatOne)
                    {
                        return new ConcatResultOne(new TwoNode(leftTwo.Left, concatOne.One));
                    }
                    else if (concat is ConcatResultTwo concatTwo)
                    {
                        return new ConcatResultOne(new ThreeNode(leftTwo.Left, concatTwo.One, concatTwo.Two));
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Unknown ConcatResult type");
                    }
                }
                else if (left is ThreeNode leftThree)
                {
                    var concat = Concat(leftThree.Right, middleSpace, right);
                    if (concat is ConcatResultOne concatOne)
                    {
                        return new ConcatResultOne(new ThreeNode(leftThree.Left, leftThree.Middle, concatOne.One));
                    }
                    else if (concat is ConcatResultTwo concatTwo)
                    {
                        return new ConcatResultTwo(new TwoNode(leftThree.Left, leftThree.Middle), new TwoNode(concatTwo.One, concatTwo.Two));
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Unknown ConcatResult type");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Internal error: Unknown NonEmptyNode type");
                }
            }
        }

        /// <summary>
        /// Returns the length of this Rope Annotation List (which is the amount of space in it).
        /// </summary>
        public int Length => (root is NonEmptyNode nonEmpty) ? (nonEmpty.Info.Length + trailingSpace) : trailingSpace;

        /// <summary>
        /// Returns the number of annotations in this Rope Annotation List.
        /// </summary>
        public int Count => (root is NonEmptyNode nonEmpty) ? nonEmpty.Info.Count : 0;

        /// <summary>
        /// Concatenates two Rope Annotation Lists.
        /// </summary>

        public static RopeAnnotationList<T> operator + (RopeAnnotationList<T> left, RopeAnnotationList<T> right)
        {
            if (left.root is EmptyNode)
            {
                if (right.root is EmptyNode)
                {
                    return new RopeAnnotationList<T>(EmptyNode.Value, left.trailingSpace + right.trailingSpace);
                }
                else if (right.root is NonEmptyNode rightRoot)
                {
                    return new RopeAnnotationList<T>(Concat(left.trailingSpace, rightRoot), right.trailingSpace);
                }
                else
                {
                    throw new InvalidOperationException("Internal error: Unknown Node type");
                }

            }
            else if (right.root is EmptyNode)
            {
                return new RopeAnnotationList<T>(left.root, left.trailingSpace + right.trailingSpace);
            }
            else
            {
                var leftNonEmpty = (NonEmptyNode)left.root;
                var rightNonEmpty = (NonEmptyNode)right.root;
                var concat = Concat(leftNonEmpty, left.trailingSpace, rightNonEmpty);
                if (concat is ConcatResultOne concatOne)
                {
                    return new RopeAnnotationList<T>(concatOne.One, right.trailingSpace);
                }
                else if (concat is ConcatResultTwo concatTwo)
                {
                    return new RopeAnnotationList<T>(new TwoNode(concatTwo.One, concatTwo.Two), right.trailingSpace);
                }
                else
                {
                    throw new InvalidOperationException("Internal error: Unknown ConcatResult type");
                }
            }
        }
    }
}
