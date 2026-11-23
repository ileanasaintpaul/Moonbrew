using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Employee
{
    // The employee at work, in the scenes where the owner is not in the café.
    public class EmployeeShift : MonoBehaviour
    {
        [SerializeField] private EmployeeData employee;
        [SerializeField] private EventBusAsset eventBus;

        private EmployeeRota _rota;
        private int _minute;
        private bool _subscribed;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(EmployeeData employeeData, EventBusAsset bus)
        {
            employee = employeeData;
            eventBus = bus;
        }

        private void Awake()
        {
            if (employee == null || eventBus == null)
            {
                Debug.LogError($"{nameof(EmployeeShift)} on {name} needs the employee and the event bus.", this);
                enabled = false;
                return;
            }

            _rota = new EmployeeRota(employee.ServiceMinutes);
        }

        private void OnEnable()
        {
            if (_subscribed || _rota == null)
            {
                return;
            }

            eventBus.Subscribe<TimeChanged>(OnTimeChanged);
            eventBus.Subscribe<OrderServed>(OnOrderServed);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<TimeChanged>(OnTimeChanged);
            eventBus.Unsubscribe<OrderServed>(OnOrderServed);
            _subscribed = false;
        }

        private void OnTimeChanged(TimeChanged message)
        {
            _minute = message.Minutes;
            if (_rota.IsFree(_minute))
            {
                eventBus.Publish(new EmployeeFreed(employee.MaxStars));
            }
        }

        private void OnOrderServed(OrderServed message)
        {
            _rota.Worked(_minute);
        }
    }
}
