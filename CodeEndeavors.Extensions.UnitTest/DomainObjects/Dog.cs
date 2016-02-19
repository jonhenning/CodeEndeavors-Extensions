using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Extensions.UnitTest.DomainObjects
{
    public class Dog : IAnimal
    {
        public Dog()
        {
            Name = "Fido";
        }
        public string Name { get; set; }
    }
}
