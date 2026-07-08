using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIDESA.Net.Core;

namespace SIDESA.Net.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    public event Action<Database.User>? LoginSuccess;

    [RelayCommand]
    private void Login()
    {
        ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username dan Password harus diisi.";
            return;
        }

        var user = Database.AuthenticateUser(Username, Password);
        if (user != null)
        {
            // Successful login
            LoginSuccess?.Invoke(user);
        }
        else
        {
            ErrorMessage = "Username atau Password salah.";
        }
    }
}
