using CodeEndeavors.Extensions;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeEndeavors.Extensions.UnitTest
{
    [TestClass]
    public class Reflection
    {
        [TestMethod]
        public void GetAllTypes()
        {
            var types = typeof(IAnimal).GetAllTypes();
            Assert.AreEqual(2, types.Count);
        }

        [TestMethod]
        public void GetAllInstances()
        {
            var instances = ReflectionExtensions.GetAllInstances<IAnimal>();
            Assert.AreEqual(2, instances.Count);

            foreach (var animal in instances)
            {
                Assert.IsFalse(string.IsNullOrEmpty(animal.Name));
            }
        }
    }
}
