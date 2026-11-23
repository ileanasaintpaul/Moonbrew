using System;

namespace Moonbrew.Employee
{
    // When the employee can take the next table.
    public class EmployeeRota
    {
        private readonly int _serviceMinutes;
        private int _freeAt;

        public EmployeeRota(int serviceMinutes)
        {
            _serviceMinutes = Math.Max(1, serviceMinutes);
        }

        public bool IsFree(int minute) => minute >= _freeAt;

        public void Worked(int minute)
        {
            _freeAt = minute + _serviceMinutes;
        }
    }
}
