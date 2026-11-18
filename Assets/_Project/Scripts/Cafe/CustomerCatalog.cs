using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Cafe
{
    // Every café customer of the game.
    [CreateAssetMenu(menuName = "Moonbrew/Cafe/Customer Catalog", fileName = "SO_CustomerCatalog")]
    public class CustomerCatalog : ScriptableObject
    {
        [SerializeField] private CustomerData[] customers = new CustomerData[0];

        public IReadOnlyList<CustomerData> Customers => customers;

        // For catalogs built from code.
        public void Init(CustomerData[] catalogCustomers)
        {
            customers = catalogCustomers ?? new CustomerData[0];
        }

        public CustomerData Find(string id)
        {
            foreach (var customer in customers)
            {
                if (customer != null && !string.IsNullOrEmpty(id) && customer.Id == id)
                {
                    return customer;
                }
            }

            return null;
        }
    }
}
