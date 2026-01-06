using Sunlighter.FrayedKnot;
using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System.ComponentModel;

namespace FrayedKnotTests;

[TestClass]
public class RopeAnnotationListTests
{
    private static RopeAnnotationList<int> CreateTestRope(Random rand, int size, int numberOfItems)
    {
        RopeAnnotationList<int> ra = RopeAnnotationList<int>.Space(size);
        for (int i = 0; i < numberOfItems; i++)
        {
            int index = rand.Next(0, ra.Count);
            ra = ra.InsertItemAt(index, InsertionMode.AfterExisting, i);
        }
        return ra;
    }

    [TestMethod]
    public void TestSerialization()
    {
        Random rand = new Random(0x60079F31);
        RopeAnnotationList<int> ra = CreateTestRope(rand, 1000000, 10000);

        ITypeTraits<RopeAnnotationList<int>> traits = Builder.Instance.GetTypeTraits<RopeAnnotationList<int>>();

        byte[] serialized = traits.SerializeToBytes(ra);
        Assert.AreEqual(serialized.Length, traits.MeasureAllBytes(ra));
        RopeAnnotationList<int> deserialized = traits.DeserializeFromBytes(serialized);

        Assert.AreEqual(0, traits.Compare(ra, deserialized), "Deserialized item should be equal to serialized item");

        Assert.IsTrue(traits.IsAnalogous(ra, deserialized), "Deserialized item should be analogous to serialized item");
    }
}
