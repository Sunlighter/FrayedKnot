using Sunlighter.FrayedKnot;
using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System.ComponentModel;

namespace FrayedKnotTests;

[TestClass]
public class RopeAnnotationListTests
{
    private static RopeAnnotationList<int> CreateTestRope(ITypeTraits<RopeAnnotationList<int>> traits, Random rand, int size, int numberOfItems)
    {
        RopeAnnotationList<int> ra = RopeAnnotationList<int>.Space(size);
        for (int i = 0; i < numberOfItems; i++)
        {
            int index = rand.Next(ra.Length + 1);
            RopeAnnotationList<int> ra2 = ra.InsertItemAt(index, InsertionMode.AfterExisting, i);

#if false
            RopeAnnotationList<int> raPrefix = ra.TakePositions(index, BoundType.Exclusive);
            RopeAnnotationList<int> ra2Prefix = ra2.TakePositions(index, BoundType.Exclusive);

            RopeAnnotationList<int> raSuffix = ra.SkipPositions(index, BoundType.Inclusive);
            RopeAnnotationList<int> ra2Suffix = ra2.SkipPositions(index, BoundType.Inclusive);

            if (traits.Compare(raPrefix, ra2Prefix) != 0)
            {
                Console.WriteLine("Prefix:");
                Console.WriteLine(traits.ToDebugString(raPrefix));
                Console.WriteLine(traits.ToDebugString(ra2Prefix));
            }

            if (traits.Compare(raSuffix, ra2Suffix) != 0)
            {
                Console.WriteLine("Suffix:");
                Console.WriteLine(traits.ToDebugString(raSuffix));
                Console.WriteLine(traits.ToDebugString(ra2Suffix));
            }

            if (ra.Count + 1 != ra2.Count)
            {
                Console.WriteLine("Number of items:");
                Console.WriteLine(traits.ToDebugString(ra));
                Console.WriteLine(traits.ToDebugString(ra2));
            }

            Assert.AreEqual(0, traits.Compare(ra.TakePositions(index, BoundType.Exclusive), ra2.TakePositions(index, BoundType.Exclusive)), "Prefix should be unchanged after insertion");
            Assert.AreEqual(0, traits.Compare(ra.SkipPositions(index, BoundType.Inclusive), ra2.SkipPositions(index, BoundType.Inclusive)), "Suffix should be unchanged after insertion");

            Assert.AreEqual(ra.Count + 1, ra2.Count, "One more item should have been added");
#endif
            ra = ra2;
        }
        return ra;
    }

    [TestMethod]
    public void TestSerialization()
    {
        ITypeTraits<RopeAnnotationList<int>> traits = Builder.Instance.GetTypeTraits<RopeAnnotationList<int>>();

        Random rand = new Random(0x60079F31);
        const int space = 1000000;
        const int itemCount = 10000;
        RopeAnnotationList<int> ra = CreateTestRope(traits, rand, space, itemCount);

        Assert.AreEqual(space, ra.Length);
        Assert.AreEqual(itemCount, ra.Count);

        byte[] serialized = traits.SerializeToBytes(ra);
        Assert.AreEqual(serialized.Length, traits.MeasureAllBytes(ra), "Serialized length should be equal to measured bytes");
        RopeAnnotationList<int> deserialized = traits.DeserializeFromBytes(serialized);

        Assert.AreEqual(0, traits.Compare(ra, deserialized), "Deserialized item should be equal to serialized item");

        Assert.IsTrue(traits.IsAnalogous(ra, deserialized), "Deserialized item should be analogous to serialized item");
    }
}
