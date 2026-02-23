using Avalonia.Controls;
using third_year_project.ViewModels;

namespace third_year_project.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //DataContext = new MainWindowViewModel();  //this is a repeated initialization do we need this???

            // Load a default page
            //PageHost.Content = new HomePage();
        }
    }
}