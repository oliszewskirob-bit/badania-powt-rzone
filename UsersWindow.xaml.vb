Imports System.Collections.ObjectModel
Imports System.Windows

Public Class UsersWindow

    Private ReadOnly _session As UserSession
    Private ReadOnly _items As New ObservableCollection(Of UserRow)

    Public Sub New(session As UserSession)
        InitializeComponent()
        _session = session
        UsersGrid.ItemsSource = _items
        Refresh()
    End Sub

    Private Sub Refresh()
        _items.Clear()
        For Each u In UserAdminRepository.ListUsers()
            _items.Add(u)
        Next
        InfoText.Text = $"Użytkowników: {_items.Count}"
    End Sub

    Private Function SelectedUser() As UserRow
        Return TryCast(UsersGrid.SelectedItem, UserRow)
    End Function

    Private Sub Refresh_Click(sender As Object, e As RoutedEventArgs)
        Refresh()
    End Sub

    Private Sub Add_Click(sender As Object, e As RoutedEventArgs)
        Dim w As New UserCreateWindow(_session)
        w.Owner = Me
        If w.ShowDialog() = True Then
            Refresh()
        End If
    End Sub

    Private Sub ResetPwd_Click(sender As Object, e As RoutedEventArgs)
        Dim u = SelectedUser()
        If u Is Nothing Then
            MessageBox.Show("Zaznacz użytkownika.")
            Return
        End If

        Dim w As New UserResetPasswordWindow(_session, u)
        w.Owner = Me
        If w.ShowDialog() = True Then
            Refresh()
        End If
    End Sub

    Private Sub ToggleActive_Click(sender As Object, e As RoutedEventArgs)
        Dim u = SelectedUser()
        If u Is Nothing Then
            MessageBox.Show("Zaznacz użytkownika.")
            Return
        End If

        Try
            UserAdminRepository.SetActive(u.Id, Not u.IsActive, _session.Username)
            Refresh()
        Catch ex As Exception
            MessageBox.Show("Błąd:" & vbCrLf & ex.ToString())
        End Try
    End Sub

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

End Class
