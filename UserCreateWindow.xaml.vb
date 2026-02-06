Imports System.Windows
Imports System.Windows.Controls

Public Class UserCreateWindow

    Private ReadOnly _session As UserSession

    Public Sub New(session As UserSession)
        InitializeComponent()
        _session = session
        UsernameBox.Focus()
    End Sub

    Private Sub Create_Click(sender As Object, e As RoutedEventArgs)
        Try
            ErrorText.Text = ""

            Dim username = If(UsernameBox.Text, "").Trim()
            Dim displayName = If(DisplayNameBox.Text, "").Trim()

            Dim roleItem = TryCast(RoleBox.SelectedItem, ComboBoxItem)
            Dim roleTag = If(roleItem Is Nothing, "user", TryCast(roleItem.Tag, String))
            Dim role = If(String.IsNullOrWhiteSpace(roleTag), "user", roleTag)

            Dim pass1 = PasswordBox.Password
            Dim pass2 = Password2Box.Password

            If pass1 <> pass2 Then
                ErrorText.Text = "Hasła nie są takie same."
                Return
            End If

            UserAdminRepository.CreateUser(username, displayName, role, pass1, _session.Username)

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
