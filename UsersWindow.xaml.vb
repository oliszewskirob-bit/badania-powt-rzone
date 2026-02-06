Imports System.Collections.ObjectModel
Imports System.Windows

Public Class UsersWindow

    Private ReadOnly _session As UserSession
    Private ReadOnly _items As New ObservableCollection(Of UserRow)

    ' Constants for messages
    Private Const USER_NOT_SELECTED_MESSAGE As String = "Zaznacz użytkownika."
    Private Const LOAD_ERROR_MESSAGE As String = "Błąd przy ładowaniu użytkowników:"
    Private Const OPERATION_ERROR_MESSAGE As String = "Błąd:"
    Private Const USERS_INFO_FORMAT As String = "Użytkowników: {0}"

    Public Sub New(session As UserSession)
        If session Is Nothing Then
            Throw New ArgumentNullException(NameOf(session), "Sesja nie może być pusta.")
        End If
        
        InitializeComponent()
        _session = session
        UsersGrid.ItemsSource = _items
        Refresh()
    End Sub

    Private Sub Refresh()
        Try
            _items.Clear()
            
            Dim users = UserAdminRepository.ListUsers()
            If users IsNot Nothing Then
                For Each u In users
                    _items.Add(u)
                Next
            End If
            
            UpdateInfoText()
        Catch ex As Exception
            MessageBox.Show(LOAD_ERROR_MESSAGE & vbCrLf & ex.ToString(), 
                          "Błąd", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error)
            _items.Clear()
            UpdateInfoText()
        End Try
    End Sub

    Private Sub UpdateInfoText()
        If InfoText IsNot Nothing Then
            InfoText.Text = String.Format(USERS_INFO_FORMAT, _items.Count)
        End If
    End Sub

    Private Function SelectedUser() As UserRow
        If UsersGrid Is Nothing Then
            Return Nothing
        End If
        Return TryCast(UsersGrid.SelectedItem, UserRow)
    End Function

    Private Function ValidateUserSelected() As UserRow
        Dim user = SelectedUser()
        If user Is Nothing Then
            MessageBox.Show(USER_NOT_SELECTED_MESSAGE, 
                          "Informacja", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information)
        End If
        Return user
    End Function

    Private Sub Refresh_Click(sender As Object, e As RoutedEventArgs)
        Refresh()
    End Sub

    Private Sub Add_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim w As New UserCreateWindow(_session)
            w.Owner = Me
            If w.ShowDialog() = True Then
                Refresh()
            End If
        Catch ex As Exception
            MessageBox.Show(OPERATION_ERROR_MESSAGE & vbCrLf & ex.ToString(), 
                          "Błąd", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ResetPwd_Click(sender As Object, e As RoutedEventArgs)
        Dim u = ValidateUserSelected()
        If u Is Nothing Then
            Return
        End If

        Try
            Dim w As New UserResetPasswordWindow(_session, u)
            w.Owner = Me
            If w.ShowDialog() = True Then
                Refresh()
            End If
        Catch ex As Exception
            MessageBox.Show(OPERATION_ERROR_MESSAGE & vbCrLf & ex.ToString(), 
                          "Błąd", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ToggleActive_Click(sender As Object, e As RoutedEventArgs)
        Dim u = ValidateUserSelected()
        If u Is Nothing Then
            Return
        End If

        Try
            UserAdminRepository.SetActive(u.Id, Not u.IsActive, _session.Username)
            Refresh()
        Catch ex As Exception
            MessageBox.Show(OPERATION_ERROR_MESSAGE & vbCrLf & ex.ToString(), 
                          "Błąd", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

End Class