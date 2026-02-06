Imports System.Windows

Public Class UserResetPasswordWindow

    Private ReadOnly _session As UserSession
    Private ReadOnly _user As UserRow

    Public Sub New(session As UserSession, user As UserRow)
        InitializeComponent()
        _session = session
        _user = user

        UserInfoBox.Text = $"{_user.Username}  ({_user.DisplayName}, {_user.Role})"
        PasswordBox.Focus()
    End Sub

    Private Sub Save_Click(sender As Object, e As RoutedEventArgs)
        Try
            ErrorText.Text = ""

            Dim pass1 = PasswordBox.Password
            Dim pass2 = Password2Box.Password

            If pass1 <> pass2 Then
                ErrorText.Text = "Hasła nie są takie same."
                Return
            End If

            UserAdminRepository.ResetPassword(_user.Id, pass1, _session.Username)

            DialogResult = True
            Close()

        Catch ex As Exception
            ErrorText.Text = ex.Message
        End Try
    End Sub

    Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
        Close()
    End Sub

End Class
