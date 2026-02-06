Imports System.Windows

Public Class LoginWindow

    Public Sub New()
        InitializeComponent()
        UsernameBox.Focus()
    End Sub

    Private Sub Login_Click(sender As Object, e As RoutedEventArgs)
        Try
            ErrorText.Text = ""

            Dim username = (If(UsernameBox.Text, "")).Trim()
            Dim pwd = PasswordBox.Password

            Dim session = UserRepository.Login(username, pwd)

            If session Is Nothing Then
                ErrorText.Text = "Nieprawidłowy login lub hasło."
                Return
            End If

            ' Wymuś zmianę hasła przy pierwszym logowaniu
            If session.MustChangePassword Then
                Dim cp As New ChangePasswordWindow(session)
                cp.Owner = Me
                cp.WindowStartupLocation = WindowStartupLocation.CenterOwner

                Dim ok = cp.ShowDialog()

                If ok <> True Then
                    ErrorText.Text = "Musisz zmienić hasło przy pierwszym logowaniu."
                    Return
                End If

                ' Ważne: NIE logujemy się drugi raz.
                ' Hasło zostało już zmienione w bazie i MustChangePassword ustawione na 0.
                session.MustChangePassword = False
            End If

            Dim main As New MainWindow(session)
            main.Show()
            Me.Close()

        Catch ex As Exception
            MessageBox.Show("Błąd logowania:" & vbCrLf & ex.ToString(),
                            "Błąd", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

End Class
