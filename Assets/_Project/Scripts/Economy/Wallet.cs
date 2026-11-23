using UnityEngine;

namespace Moonbrew.Economy
{
    public class Wallet
    {
        public int Balance { get; private set; }

        // A negative balance can only come from a damaged save.
        public void Restore(int balance)
        {
            Balance = Mathf.Max(0, balance);
        }

        public bool Earn(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            Balance = amount > int.MaxValue - Balance ? int.MaxValue : Balance + amount;
            return true;
        }

        public bool Spend(int amount)
        {
            if (amount <= 0 || amount > Balance)
            {
                return false;
            }

            Balance -= amount;
            return true;
        }
    }
}
