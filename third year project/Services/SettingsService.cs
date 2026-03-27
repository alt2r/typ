using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace third_year_project.Services
{
    public class SettingsService
    {
        private static readonly SettingsService instance = new SettingsService();
        public static SettingsService Instance => instance;

        public Key LeftInputKey { get; set; } = Key.A; //default value
        public Key RightInputKey { get; set; } = Key.L; //default value
    }
}
