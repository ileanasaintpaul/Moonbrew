using UnityEngine;

namespace Moonbrew.Employee
{
    // Who looks after the café while the owner is away, and what they can do.
    [CreateAssetMenu(menuName = "Moonbrew/Employee/Employee", fileName = "SO_Employee_")]
    public class EmployeeData : ScriptableObject
    {
        [Tooltip("Stable snake_case ID. Never change it once shipped.")]
        [SerializeField] private string id;
        [Tooltip("The hardest recipes they can serve.")]
        [SerializeField, Min(1)] private int maxStars = 1;
        [Tooltip("Game minutes between two tables served.")]
        [SerializeField, Min(1)] private int serviceMinutes = 20;

        public string Id => id;

        public int MaxStars => maxStars;

        public int ServiceMinutes => serviceMinutes;

        // For employees built from code.
        public void Init(string employeeId, int stars, int minutes)
        {
            id = employeeId;
            maxStars = stars;
            serviceMinutes = minutes;
        }
    }
}
