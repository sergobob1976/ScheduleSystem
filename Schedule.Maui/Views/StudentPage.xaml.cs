using Schedule.Core.Models;
using Schedule.Maui.Services;
using Schedule.Maui.ViewModels;

namespace Schedule.Maui.Views
{
    public partial class StudentPage : ContentPage
    {
        public StudentPage(StudentViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is StudentViewModel vm)
            {
                await vm.OnAppearingAsync();
            }
        }
    }
}
