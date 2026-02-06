Imports System.Windows

Public Class ChangePasswordWindow

    Private ReadOnly _session As UserSession

    Public Sub New(session As UserSession)
        InitializeComponent()
        _session = session
    End Sub

    Private Sub Save_Click(sender As Object, e As RoutedEventArgs)
        Try
            ErrorText.Text = ""

            Dim p1 = NewPassBox.Password
            Dim p2 = NewPass2Box.Password

            If String.IsNullOrWhiteSpace(p1) OrElse p1.Length < 6 Then
                ErrorText.Text = "Hasło musi mieć min. 6 znaków."
                Return
            End If
            If p1 <> p2 Then
                ErrorText.Text = "Hasła nie są takie same."
                Return
            End If

            UserPasswordRepository.ChangeOwnPassword(_session.Id, p1, _session.Username)

            Me.DialogResult = True
            Me.Close()

        Catch ex As Exception
            ErrorText.Text = ex.Message
        End Try
    End Sub

    Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
        Me.DialogResult = False
        Me.Close()
    End Sub

End Class
