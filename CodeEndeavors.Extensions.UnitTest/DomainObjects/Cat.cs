using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Extensions.UnitTest.DomainObjects
{
    public class Cat : IAnimal
    {
        public Cat()
        {
            Name = "Crookshanks";
        }

        public string Name { get; set; }
    }
}
