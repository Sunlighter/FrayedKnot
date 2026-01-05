using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System.Collections.Immutable;
using System.Numerics;

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
            private readonly int height;

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

        /// <summary>
        /// Returns the length of this Rope Annotation List (which is the amount of space in it).
        /// </summary>
        public int Length => (root is NonEmptyNode nonEmpty) ? (nonEmpty.Info.Length + trailingSpace) : trailingSpace;

        /// <summary>
        /// Returns the number of annotations in this Rope Annotation List.
        /// </summary>
        public int Count => (root is NonEmptyNode nonEmpty) ? nonEmpty.Info.Count : 0;

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
            System.Diagnostics.Debug.Assert(leftSpace >= 0, "leftSpace has to be at least zero.");

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

        private static (ImmutableStack<NonEmptyNode>, int, ImmutableStack<NonEmptyNode>, int) SplitByPositions(NonEmptyNode root, int spaceAfterRoot, int charCount, BoundType boundType)
        {
            ImmutableStack<NonEmptyNode> taken = ImmutableStack<NonEmptyNode>.Empty;
            ImmutableStack<NonEmptyNode> untaken = ImmutableStack<NonEmptyNode>.Empty.Push(root);

            while (true)
            {
                if (untaken.IsEmpty)
                {
                    int takenSpace = Math.Max(charCount, spaceAfterRoot);
                    return (taken, takenSpace, untaken, spaceAfterRoot - takenSpace);
                }
                else if (untaken.Peek().Info.Length <= charCount)
                {
                    NonEmptyNode n = untaken.Peek();
                    taken = taken.Push(n);
                    untaken = untaken.Pop();
                    charCount -= n.Info.Length;

                    if (untaken.IsEmpty)
                    {
                        int takenSpace = Math.Max(charCount, spaceAfterRoot);

                        return (taken, takenSpace, untaken, spaceAfterRoot - takenSpace);
                    }
                }
                else
                {
                    NonEmptyNode node = untaken.Peek();
                    untaken = untaken.Pop();
                    if (node is Leaf leaf)
                    {
                        if (charCount < leaf.SpaceBefore)
                        {
                            untaken = untaken.Push(new Leaf(leaf.SpaceBefore - charCount, leaf.Item));
                            return (taken, charCount, untaken, spaceAfterRoot);
                        }
                        else if (charCount == leaf.SpaceBefore)
                        {
                            if (boundType == BoundType.Inclusive)
                            {
                                taken = taken.Push(leaf);
                                while(!untaken.IsEmpty && untaken.Peek() is Leaf nextLeaf && nextLeaf.SpaceBefore == 0)
                                {
                                    Leaf nl = (Leaf)untaken.Peek();
                                    taken = taken.Push(nl);
                                    untaken = untaken.Pop();
                                }
                                return (taken, 0, untaken, spaceAfterRoot);
                            }
                            else
                            {
                                untaken = untaken.Push(new Leaf(0, leaf.Item));
                                return (taken, charCount, untaken, spaceAfterRoot);
                            }
                        }
                        else
                        {
                            taken = taken.Push(leaf);
                            charCount -= leaf.SpaceBefore;
                        }
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
        }

        /// <summary>
        /// Returns a new Rope Annotation List created by skipping the first <paramref name="charCount"/> positions of this list.
        /// </summary>
        /// <param name="charCount">The number of positions to skip.</param>
        /// <param name="boundType">Specifies whether to also skip the position at <paramref name="charCount"/>.</param>
        public RopeAnnotationList<T> SkipPositions(int charCount, BoundType boundType)
        {
            if (charCount < 0) throw new ArgumentException($"{nameof(charCount)} cannot be negative", nameof(charCount));

            if (charCount == 0) return this;
            else if (charCount >= Length)
            {
                return empty;
            }
            else if (root is EmptyNode)
            {
                System.Diagnostics.Debug.Assert(trailingSpace >= charCount);
                return new RopeAnnotationList<T>(EmptyNode.Value, trailingSpace - charCount);
            }
            else
            {
                (ImmutableStack<NonEmptyNode> _, int _, ImmutableStack<NonEmptyNode> untaken, int untakenSpace) =
                    SplitByPositions((NonEmptyNode)root, trailingSpace, charCount, boundType);

                if (untaken.IsEmpty)
                {
                    return new RopeAnnotationList<T>(EmptyNode.Value, untakenSpace);
                }

                NonEmptyNode result = untaken.Peek();
                untaken = untaken.Pop();
                while (!untaken.IsEmpty)
                {
                    NonEmptyNode n2 = untaken.Peek();
                    untaken = untaken.Pop();
                    ConcatResult cr = Concat(result, 0, n2);
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
                return new RopeAnnotationList<T>(result, untakenSpace);
            }
        }

        /// <summary>
        /// Returns a new Rope Annotation List created by taking the first <paramref name="charCount"/> positions of this list.
        /// </summary>
        /// <param name="charCount">The number of positions to take.</param>
        /// <param name="boundType">Specifies whether to also take the position at <paramref name="charCount"/>.</param>
        public RopeAnnotationList<T> TakePositions(int charCount, BoundType boundType)
        {
            if (charCount < 0) throw new ArgumentException($"{nameof(charCount)} cannot be negative", nameof(charCount));

            if (charCount == 0) return empty;
            else if (charCount >= Length)
            {
                return this;
            }
            else if (root is EmptyNode)
            {
                System.Diagnostics.Debug.Assert(trailingSpace >= charCount);
                return new RopeAnnotationList<T>(EmptyNode.Value, charCount);
            }
            else
            {
                (ImmutableStack<NonEmptyNode> taken, int takenSpace, ImmutableStack<NonEmptyNode> _, int _) =
                    SplitByPositions((NonEmptyNode)root, trailingSpace, charCount, boundType);

                if (taken.IsEmpty)
                {
                    return new RopeAnnotationList<T>(EmptyNode.Value, takenSpace);
                }

                NonEmptyNode result = taken.Peek();
                taken = taken.Pop();
                while (!taken.IsEmpty)
                {
                    NonEmptyNode n2 = taken.Peek();
                    taken = taken.Pop();
                    ConcatResult cr = Concat(n2, 0, result);
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
                return new RopeAnnotationList<T>(result, takenSpace);
            }
        }

        private static (ImmutableStack<NonEmptyNode>, ImmutableStack<NonEmptyNode>) SplitByCount(NonEmptyNode root, int itemCount)
        {
            ImmutableStack<NonEmptyNode> taken = ImmutableStack<NonEmptyNode>.Empty;
            ImmutableStack<NonEmptyNode> untaken = ImmutableStack<NonEmptyNode>.Empty.Push(root);

            while (true)
            {
                if (untaken.IsEmpty || itemCount == 0)
                {
                    return (taken, untaken);
                }
                else if (untaken.Peek().Info.Count <= itemCount)
                {
                    NonEmptyNode n = untaken.Peek();
                    taken = taken.Push(n);
                    untaken = untaken.Pop();
                    itemCount -= n.Info.Count;

                    if (untaken.IsEmpty)
                    {
                        return (taken, untaken);
                    }
                }
                else
                {
                    NonEmptyNode node = untaken.Peek();
                    untaken = untaken.Pop();
                    System.Diagnostics.Debug.Assert(node is not Leaf); // should have already been dealt with above

                    if (node is TwoNode two)
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
                    else
                    {
                        throw new InvalidOperationException("Internal error: Unknown NonEmptyNode type");
                    }
                }
            }
        }

        /// <summary>
        /// Returns a new Rope Annotation List created by skipping the first <paramref name="itemCount"/> items in this list.
        /// </summary>
        /// <param name="itemCount">The number of items to skip.</param>
        public RopeAnnotationList<T> SkipItems(int itemCount)
        {
            if (itemCount < 0) throw new ArgumentException($"{nameof(itemCount)} cannot be negative", nameof(itemCount));

            if (itemCount == 0) return this;
            else if (itemCount >= Count)
            {
                return empty;
            }
            else if (root is EmptyNode)
            {
                return this;
            }
            else
            {
                (ImmutableStack<NonEmptyNode> _, ImmutableStack<NonEmptyNode> untaken) =
                    SplitByCount((NonEmptyNode)root, itemCount);

                if (untaken.IsEmpty) return this;

                NonEmptyNode result = untaken.Peek();
                untaken = untaken.Pop();
                while (!untaken.IsEmpty)
                {
                    NonEmptyNode n2 = untaken.Peek();
                    untaken = untaken.Pop();
                    ConcatResult cr = Concat(result, 0, n2);
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
                return new RopeAnnotationList<T>(result, trailingSpace);
            }
        }

        /// <summary>
        /// Returns a new Rope Annotation List created by taking the first <paramref name="itemCount"/> items in this list.
        /// </summary>
        /// <param name="itemCount">The number of items to take.</param>
        public RopeAnnotationList<T> TakeItems(int itemCount)
        {
            if (itemCount < 0) throw new ArgumentException($"{nameof(itemCount)} cannot be negative", nameof(itemCount));

            if (itemCount == 0) return empty;
            else if (itemCount > Count)
            {
                return this;
            }
            else if (itemCount == Count)
            {
                return new RopeAnnotationList<T>(root, 0);
            }
            else
            {
                (ImmutableStack<NonEmptyNode> taken, ImmutableStack<NonEmptyNode> _) =
                    SplitByCount((NonEmptyNode)root, itemCount);

                if (taken.IsEmpty) return empty;

                NonEmptyNode result = taken.Peek();
                taken = taken.Pop();
                while (!taken.IsEmpty)
                {
                    NonEmptyNode n2 = taken.Peek();
                    taken = taken.Pop();
                    ConcatResult cr = Concat(n2, 0, result);
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
                return new RopeAnnotationList<T>(result, 0);
            }
        }

        /// <summary>
        /// Represents an item with its offset.
        /// </summary>
        public sealed class ItemWithOffset
        {
            private readonly int offset;
            private readonly T item;

            /// <summary>
            /// Constructs a representation of an item with its offset.
            /// </summary>
            /// <param name="offset"></param>
            /// <param name="item"></param>
            public ItemWithOffset(int offset, T item)
            {
                this.offset = offset;
                this.item = item;
            }

            /// <summary>
            /// The offset of the item.
            /// </summary>
            public int Offset => offset;

            /// <summary>
            /// The item itself.
            /// </summary>
            public T Item => item;
        }

        /// <summary>
        /// Returns an enumerable of the items in this Rope Annotation List, along with their offsets.
        /// </summary>
        public IEnumerable<ItemWithOffset> EnumerateItemsWithOffsets()
        {
            if (root is EmptyNode)
            {
                yield break;
            }
            else
            {
                ImmutableStack<NonEmptyNode> toVisit = ImmutableStack<NonEmptyNode>.Empty.Push((NonEmptyNode)root);
                int offset = 0;
                while(true)
                {
                    NonEmptyNode n = toVisit.Peek();
                    toVisit = toVisit.Pop();
                    if (n is Leaf leaf)
                    {
                        offset += leaf.SpaceBefore;
                        yield return new ItemWithOffset(offset, leaf.Item);
                    }
                    else if (n is TwoNode twoNode)
                    {
                        toVisit = toVisit.Push(twoNode.Right);
                        toVisit = toVisit.Push(twoNode.Left);
                    }
                    else if (n is ThreeNode threeNode)
                    {
                        toVisit = toVisit.Push(threeNode.Right);
                        toVisit = toVisit.Push(threeNode.Middle);
                        toVisit = toVisit.Push(threeNode.Left);
                    }
                    else
                    {
                        throw new InvalidOperationException("Internal error: Unknown NonEmptyNode type");
                    }
                }
            }
        }

        /// <summary>
        /// Inserts an annotation at the given position.
        /// </summary>
        /// <param name="pos">The position at which to do the insertion.</param>
        /// <param name="insertionMode">Controls whether item is inserted before or after existing items at the same position.</param>
        /// <param name="item">The item to be inserted.</param>
        /// <returns>The new annotation list.</returns>
        public RopeAnnotationList<T> InsertItemAt(int pos, InsertionMode insertionMode, T item)
        {
            if (pos < 0) throw new ArgumentOutOfRangeException(nameof(pos), "Position cannot be negative.");
            if (pos > Length)
            {
                return this + Space(pos - Length) + Item(item);
            }
            else
            {
                BoundType boundType = (insertionMode == InsertionMode.BeforeExisting) ? BoundType.Exclusive : BoundType.Inclusive;
                var left = this.TakePositions(pos, boundType);
                var right = this.SkipPositions(pos, boundType);
                var middle = RopeAnnotationList<T>.Item(item);
                return left + middle + right;
            }
        }

        /// <summary>
        /// Deletes all annotations at the given position.
        /// </summary>
        /// <param name="pos">The position at which to do the deletion</param>
        /// <returns>The new annotation list.</returns>
        public RopeAnnotationList<T> DeleteItemsAt(int pos)
        {
            if (pos < 0) throw new ArgumentOutOfRangeException(nameof(pos), "Position cannot be negative.");
            if (pos > Length) return this;

            var left = this.TakePositions(pos, BoundType.Exclusive);
            var right = this.SkipPositions(pos, BoundType.Inclusive);
            return left + right;
        }

        /// <summary>
        /// Deletes all annotations within a given range, without removing any space.
        /// </summary>
        /// <param name="startPos">The start position of the range.</param>
        /// <param name="startBoundType">Whether to include (in deletion) or exclude (from deletion) items exactly at the start position.</param>
        /// <param name="length">The length of the range.</param>
        /// <param name="endBoundType">Whether to include (in deletion) or exclude (from deletion) items exactly at the end position.</param>
        /// <returns>The new annotation list.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public RopeAnnotationList<T> ClearRange(int startPos, BoundType startBoundType, int length, BoundType endBoundType)
        {
            if (startPos < 0) throw new ArgumentOutOfRangeException(nameof(startPos), "Start position cannot be negative.");
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");
            if ((long)startPos + (long)length > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "The sum of start position and length cannot exceed Int32.MaxValue.");
            }
            int endPos = startPos + length;
            if (endPos > Length)
            {
                int realLength = Length - startPos;

                return TakePositions(startPos, startBoundType == BoundType.Exclusive ? BoundType.Inclusive : BoundType.Exclusive)
                    + Space(realLength);
            }
            else
            {
                return TakePositions(startPos, startBoundType == BoundType.Exclusive ? BoundType.Inclusive : BoundType.Exclusive)
                    + Space(length)
                    + SkipPositions(endPos, endBoundType);
            }
        }

        private static RopeAnnotationList<U>.NonEmptyNode MapNode<U>(NonEmptyNode node, Func<T, U> mapFunc)
        {
            if (node is Leaf leaf)
            {
                return new RopeAnnotationList<U>.Leaf(leaf.SpaceBefore, mapFunc(leaf.Item));
            }
            else if (node is TwoNode two)
            {
                return new RopeAnnotationList<U>.TwoNode
                (
                    MapNode(two.Left, mapFunc),
                    MapNode(two.Right, mapFunc)
                );
            }
            else if (node is ThreeNode three)
            {
                return new RopeAnnotationList<U>.ThreeNode
                (
                    MapNode(three.Left, mapFunc),
                    MapNode(three.Middle, mapFunc),
                    MapNode(three.Right, mapFunc)
                );
            }
            else
            {
                throw new InvalidOperationException("Internal error: Unknown NonEmptyNode type");
            }
        }

        /// <summary>
        /// Apply a map function to every annotation in this list, returning a new list.
        /// </summary>
        /// <typeparam name="U">The type of the result of the map function.</typeparam>
        /// <param name="mapFunc">The map function.</param>
        /// <returns>The new list.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public RopeAnnotationList<U> Map<U>(Func<T, U> mapFunc)
        {
            if (root is EmptyNode)
            {
                if (trailingSpace == 0)
                {
                    return RopeAnnotationList<U>.Empty;
                }
                else
                {
                    return new RopeAnnotationList<U>(RopeAnnotationList<U>.EmptyNode.Value, trailingSpace);
                }
            }
            else if (root is NonEmptyNode nonEmptyRoot)
            {
                return new RopeAnnotationList<U>(MapNode(nonEmptyRoot, mapFunc), this.trailingSpace);
            }
            else
            {
                throw new InvalidOperationException("Internal error: Unknown Node type");
            }
        }
    }

    /// <summary>
    /// Specifies whether a range bound is inclusive or exclusive.
    /// </summary>
    public enum BoundType
    {
        /// <summary>
        /// Specifies that a range bound is exclusive. Inserts before other items at the same position.
        /// </summary>
        Exclusive,

        /// <summary>
        /// Specifies that a range bound is inclusive. Inserts after other items at the same position.
        /// </summary>
        Inclusive
    }

    /// <summary>
    /// Specifies whether to insert a new item before or after existing items at the same position.
    /// </summary>
    public enum InsertionMode
    {
        /// <summary>
        /// Specifies that a new item should be inserted before other items at the same position.
        /// </summary>
        BeforeExisting,

        /// <summary>
        /// Specifies that a new item should be inserted after other items at the same position.
        /// </summary>
        AfterExisting
    }
}
