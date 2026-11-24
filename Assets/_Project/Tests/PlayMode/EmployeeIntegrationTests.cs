using System.Collections;
using System.Collections.Generic;
using Moonbrew.Cafe;
using Moonbrew.Core;
using Moonbrew.Employee;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // With the owner away, the employee serves one-star dishes from the café's stock and the table pays.
    public class EmployeeIntegrationTests
    {
        private const int Open = 11 * 60;
        private const int Gap = 30;
        private const int Choose = 10;
        private const int Patience = 60;
        private const int Price = 10;

        private readonly List<Object> _created = new List<Object>();
        private readonly List<OrderServed> _served = new List<OrderServed>();
        private EventBusAsset _bus;
        private InventoryHolder _fridge;
        private ItemData _easy;
        private ItemData _hard;
        private ItemData _crop;
        private ItemCatalog _items;
        private CafeRun _run;
        private CafeConfig _config;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _bus.Subscribe<OrderServed>(message => _served.Add(message));
            _easy = Dish("dish_easy");
            _hard = Dish("dish_hard");
            _crop = Dish("crop_test");
            _items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            _items.Init(new[] { _easy, _hard, _crop });

            var customer = Create(ScriptableObject.CreateInstance<CustomerData>());
            customer.Init("customer_knight", CustomerKind.Passing, null, null, new[] { "Hello" }, null);
            var customers = Create(ScriptableObject.CreateInstance<CustomerCatalog>());
            customers.Init(new[] { customer });
            _config = Create(ScriptableObject.CreateInstance<CafeConfig>());
            _config.Init(customers, 11, 18, Gap, Gap, 1, 0f, Choose, Choose, Patience);
            _config.InitOrders(_items, 2.5f);
            _config.InitTables(new[] { 2 });
            var day = Create(ScriptableObject.CreateInstance<CafeDay>());
            _run = day.Run(_config, _config.TableSeats);

            var world = Create(new GameObject("World"));
            world.SetActive(false);
            _fridge = world.AddComponent<InventoryHolder>();
            _fridge.Init(Create(ScriptableObject.CreateInstance<InventoryConfig>()), _items, _bus);
            var display = world.AddComponent<InventoryHolder>();
            display.Init(Create(ScriptableObject.CreateInstance<InventoryConfig>()), _items, _bus);
            var easyRecipe = Recipe("recipe_easy", _easy, 1);
            var hardRecipe = Recipe("recipe_hard", _hard, 2);
            var recipes = Create(ScriptableObject.CreateInstance<RecipeCatalog>());
            recipes.Init(new[] { easyRecipe, hardRecipe });
            var book = world.AddComponent<RecipeBookHolder>();
            book.Init(recipes, _bus);
            world.AddComponent<CafePantry>().Init(_fridge, display, recipes, _items, _bus);
            world.AddComponent<CafeAway>().Init(_config, day, _bus);
            var employee = Create(ScriptableObject.CreateInstance<EmployeeData>());
            employee.Init("employee_test", 1, 20);
            world.AddComponent<EmployeeShift>().Init(employee, _bus);
            world.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
            _served.Clear();
        }

        [UnityTest]
        public IEnumerator ReadyTable_OneStarDishInStock_IsServedAndPays()
        {
            yield return null;
            _fridge.Inventory.Add(_easy, 1);

            ReachReadyTable();

            Assert.AreEqual(1, _served.Count);
            Assert.AreEqual(Price, _served[0].Payment);
            Assert.AreEqual(0, _fridge.Inventory.Count(_easy));
        }

        [UnityTest]
        public IEnumerator ServedTable_DoneEating_TheEmployeeCashesTheBill()
        {
            var paid = new List<BillPaid>();
            _bus.Subscribe<BillPaid>(message => paid.Add(message));
            yield return null;
            _fridge.Inventory.Add(_easy, 1);
            ReachReadyTable();
            Assert.IsEmpty(paid, "Nothing is cashed while the table eats.");

            _bus.Publish(new TimeChanged(Open + Gap + Choose + _config.EatMinutes));

            Assert.AreEqual(1, paid.Count);
            Assert.AreEqual(Price, paid[0].Payment);
            Assert.IsTrue(paid[0].ByEmployee);
        }

        [UnityTest]
        public IEnumerator ReadyTable_NoStock_IsNotServed()
        {
            yield return null;

            ReachReadyTable();

            Assert.IsEmpty(_served);
            Assert.AreEqual(GroupState.Ordered, _run.Service.Tables[0].State);
        }

        [UnityTest]
        public IEnumerator ReadyTable_OnlyTwoStarDishInStock_IsNotServed()
        {
            yield return null;
            _fridge.Inventory.Add(_hard, 1);

            ReachReadyTable();

            Assert.IsEmpty(_served);
            Assert.AreEqual(1, _fridge.Inventory.Count(_hard));
        }

        private void ReachReadyTable()
        {
            _bus.Publish(new TimeChanged(Open));
            _bus.Publish(new TimeChanged(Open + Gap));
            _bus.Publish(new TimeChanged(Open + Gap + Choose));
        }

        private RecipeData Recipe(string id, ItemData dish, int stars)
        {
            var recipe = Create(ScriptableObject.CreateInstance<RecipeData>());
            recipe.Init(id, new[] { new Ingredient(_crop, 1) }, dish, stars, 30, true);
            return recipe;
        }

        private ItemData Dish(string id)
        {
            var item = Create(ScriptableObject.CreateInstance<ItemData>());
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(item);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("basePrice").intValue = Price;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            return item;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
