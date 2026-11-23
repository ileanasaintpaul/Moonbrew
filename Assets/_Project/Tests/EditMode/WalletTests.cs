using Moonbrew.Economy;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class WalletTests
    {
        private static Wallet WalletWith(int balance)
        {
            var wallet = new Wallet();
            wallet.Restore(balance);
            return wallet;
        }

        [Test]
        public void Earn_PositiveAmount_AddsToBalance()
        {
            var wallet = WalletWith(10);

            Assert.IsTrue(wallet.Earn(5));

            Assert.AreEqual(15, wallet.Balance);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Earn_ZeroOrNegative_IsRefused(int amount)
        {
            var wallet = WalletWith(10);

            Assert.IsFalse(wallet.Earn(amount));

            Assert.AreEqual(10, wallet.Balance);
        }

        [Test]
        public void Earn_PastTheMaximum_StopsAtTheMaximum()
        {
            var wallet = WalletWith(int.MaxValue - 1);

            Assert.IsTrue(wallet.Earn(5));

            Assert.AreEqual(int.MaxValue, wallet.Balance);
        }

        [Test]
        public void Spend_AffordableAmount_RemovesFromBalance()
        {
            var wallet = WalletWith(10);

            Assert.IsTrue(wallet.Spend(4));

            Assert.AreEqual(6, wallet.Balance);
        }

        [Test]
        public void Spend_WholeBalance_LeavesZero()
        {
            var wallet = WalletWith(10);

            Assert.IsTrue(wallet.Spend(10));

            Assert.AreEqual(0, wallet.Balance);
        }

        [TestCase(0)]
        [TestCase(-5)]
        [TestCase(11)]
        public void Spend_InvalidOrTooMuch_IsRefused(int amount)
        {
            var wallet = WalletWith(10);

            Assert.IsFalse(wallet.Spend(amount));

            Assert.AreEqual(10, wallet.Balance);
        }

        [Test]
        public void Spend_EmptyWallet_IsRefused()
        {
            Assert.IsFalse(new Wallet().Spend(1));
        }

        [Test]
        public void Restore_PositiveBalance_ReplacesBalance()
        {
            var wallet = WalletWith(10);

            wallet.Restore(3);

            Assert.AreEqual(3, wallet.Balance);
        }

        [TestCase(0)]
        [TestCase(-20)]
        public void Restore_ZeroOrNegative_LeavesEmpty(int balance)
        {
            Assert.AreEqual(0, WalletWith(balance).Balance);
        }
    }
}
