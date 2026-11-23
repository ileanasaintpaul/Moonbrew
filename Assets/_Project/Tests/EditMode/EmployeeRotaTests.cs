using Moonbrew.Employee;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class EmployeeRotaTests
    {
        [Test]
        public void IsFree_NothingDoneYet_IsTrue()
        {
            Assert.IsTrue(new EmployeeRota(20).IsFree(0));
        }

        [Test]
        public void IsFree_DuringAService_IsFalse()
        {
            var rota = new EmployeeRota(20);

            rota.Worked(100);

            Assert.IsFalse(rota.IsFree(119));
        }

        [Test]
        public void IsFree_ServiceOver_IsTrue()
        {
            var rota = new EmployeeRota(20);

            rota.Worked(100);

            Assert.IsTrue(rota.IsFree(120));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Worked_InvalidServiceTime_StillTakesAMinute(int serviceMinutes)
        {
            var rota = new EmployeeRota(serviceMinutes);

            rota.Worked(100);

            Assert.IsFalse(rota.IsFree(100));
            Assert.IsTrue(rota.IsFree(101));
        }
    }
}
