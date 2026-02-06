Imports System.Collections.ObjectModel
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Threading


Public Class MainWindow

    Private ReadOnly _session As UserSession
    Private ReadOnly _items As New ObservableCollection(Of RepeatEventListItem)
    Private ReadOnly _searchTimer As New DispatcherTimer() With {.Interval = TimeSpan.FromMilliseconds(300)}

    Public Sub New(session As UserSession)
        InitializeComponent()
        _session = session
        DeleteBtn.Visibility = If(_session.IsAdmin, Visibility.Visible, Visibility.Collapsed)

        Me.Title = $"QT/MR Tracker – {_session.DisplayName} ({_session.Role})"

        EventsGrid.ItemsSource = _items
        AddHandler _searchTimer.Tick, AddressOf SearchTimer_Tick

        RefreshList()
    End Sub

    ' Zwraca aktualnie wybrane filtry: Modalność (CT/MR/ALL) + Status (new/in_progress/closed/ALL)
    Private Function SelectedFilters() As (Modality As String, Status As String)

        Dim modality As String = "ALL"
        Dim status As String = "ALL"

        ' FilterBox (CT/MR/ALL)
        If FilterBox IsNot Nothing AndAlso FilterBox.SelectedItem IsNot Nothing Then
            Dim modItem = TryCast(FilterBox.SelectedItem, ComboBoxItem)
            If modItem IsNot Nothing AndAlso modItem.Content IsNot Nothing Then
                modality = modItem.Content.ToString()
            ElseIf TypeOf FilterBox.SelectedItem Is String Then
                modality = FilterBox.SelectedItem.ToString()
            End If
        End If

        ' StatusFilterBox (new/in_progress/closed/ALL)
        If StatusFilterBox IsNot Nothing AndAlso StatusFilterBox.SelectedItem IsNot Nothing Then
            Dim stItem = TryCast(StatusFilterBox.SelectedItem, ComboBoxItem)
            If stItem IsNot Nothing AndAlso stItem.Content IsNot Nothing Then
                status = stItem.Content.ToString()
            ElseIf TypeOf StatusFilterBox.SelectedItem Is String Then
                status = StatusFilterBox.SelectedItem.ToString()
            End If
        End If

        Return (modality, status)
    End Function


    Private Sub RefreshList()
        Try
            ' Bezpieczne pobranie search (SearchBox może być Nothing, jeśli XAML nie ma kontrolki albo jeszcze się nie zainicjalizowała)
            Dim q As String = ""
            If SearchBox IsNot Nothing AndAlso SearchBox.Text IsNot Nothing Then
                q = SearchBox.Text.Trim()
            End If

            Dim f = SelectedFilters()

            ' 1) Lista
            Dim rows = RepeatEventListRepository.List(f.Modality, f.Status, q)

            _items.Clear()
            For Each it In rows
                _items.Add(it)
            Next

            If InfoText IsNot Nothing Then
                InfoText.Text = $"Wpisów: {_items.Count}"
            End If

            ' 2) Liczniki (jeśli masz je w XAML) – pełna odporność na brak kontrolek i brak kluczy
            ' Jeśli jeszcze nie dodałeś liczników w XAML, ta część nic nie zrobi.
            Dim counts = RepeatEventListRepository.CountByStatus(f.Modality, q)

            Dim allCnt = If(counts.ContainsKey("all"), counts("all"), 0)
            Dim newCnt = If(counts.ContainsKey("new"), counts("new"), 0)
            Dim ipCnt = If(counts.ContainsKey("in_progress"), counts("in_progress"), 0)
            Dim closedCnt = If(counts.ContainsKey("closed"), counts("closed"), 0)

            If CountAllText IsNot Nothing Then CountAllText.Text = $"Wszystkie: {allCnt}"
            If CountNewText IsNot Nothing Then CountNewText.Text = $"NEW: {newCnt}"
            If CountInProgressText IsNot Nothing Then CountInProgressText.Text = $"W trakcie: {ipCnt}"
            If CountClosedText IsNot Nothing Then CountClosedText.Text = $"Zamknięte: {closedCnt}"

        Catch ex As Exception
            MessageBox.Show("Błąd wczytania listy:" & vbCrLf & ex.ToString())
        End Try
    End Sub


    Private Sub Refresh_Click(sender As Object, e As RoutedEventArgs)
        RefreshList()
    End Sub

    Private Sub Filter_Changed(sender As Object, e As SelectionChangedEventArgs)
        RefreshList()
    End Sub

    Private Sub Search_KeyUp(sender As Object, e As KeyEventArgs)
        _searchTimer.Stop()
        _searchTimer.Start()
    End Sub


    Private Sub Add_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim w As New AddEventWindow(_session)
            w.Owner = Me
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner

            If w.ShowDialog() = True Then
                InfoText.Text = "Zapisano nowe zgłoszenie."
                RefreshList()
            Else
                InfoText.Text = "Anulowano."
            End If

        Catch ex As Exception
            MessageBox.Show("Błąd otwierania okna: " & ex.ToString())
        End Try
    End Sub

    Private Sub Grid_DoubleClick(sender As Object, e As MouseButtonEventArgs)
        Dim selected = TryCast(EventsGrid.SelectedItem, RepeatEventListItem)
        If selected Is Nothing Then Return

        Dim w As New EventDetailsWindow(selected.Id, _session)
        w.Owner = Me
        w.WindowStartupLocation = WindowStartupLocation.CenterOwner

        ' Po zamknięciu podglądu odśwież listę (np. status się zmienił)
        If w.ShowDialog() Then
            RefreshList()
        End If
    End Sub



    Private Sub Clear_Click(sender As Object, e As RoutedEventArgs)
        Try
            ' reset filtrów
            If FilterBox IsNot Nothing Then FilterBox.SelectedIndex = 0
            If StatusFilterBox IsNot Nothing Then StatusFilterBox.SelectedIndex = 0
            If SearchBox IsNot Nothing Then SearchBox.Text = ""
            RefreshList()
        Catch ex As Exception
            MessageBox.Show("Błąd resetu filtrów:" & vbCrLf & ex.ToString())
        End Try
    End Sub

    Private Sub SearchTimer_Tick(sender As Object, e As EventArgs)
        _searchTimer.Stop()
        RefreshList()
    End Sub

    Private Sub Search_TextChanged(sender As Object, e As TextChangedEventArgs)
        _searchTimer.Stop()
        _searchTimer.Start()
    End Sub

    Private Sub Users_Click(sender As Object, e As RoutedEventArgs)
        If _session.Role <> "admin" Then
            MessageBox.Show("Brak uprawnień. Tylko admin może zarządzać użytkownikami.")
            Return
        End If

        Dim w As New UsersWindow(_session)
        w.Owner = Me
        w.WindowStartupLocation = WindowStartupLocation.CenterOwner
        w.ShowDialog()
    End Sub
    Private Sub Export_Click(sender As Object, e As RoutedEventArgs)
        Dim w As New ExportCsvWindow(_session)
        w.Owner = Me
        w.WindowStartupLocation = WindowStartupLocation.CenterOwner
        w.ShowDialog()
    End Sub
    Private Sub SetInProgress_Row_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim btn = TryCast(sender, Button)
            If btn Is Nothing OrElse btn.Tag Is Nothing Then
                MessageBox.Show("Brak ID w przycisku (Tag). Kliknij wiersz i spróbuj ponownie.")
                Return
            End If

            Dim id As Integer
            If Not Integer.TryParse(btn.Tag.ToString(), id) OrElse id <= 0 Then
                MessageBox.Show("Nieprawidłowe ID w Tag: " & btn.Tag.ToString())
                Return
            End If

            RepeatEventsRepository.SetStatus(id, "in_progress", _session.Username)
            InfoText.Text = $"Ustawiono W TRAKCIE (ID={id})."
            RefreshList()

        Catch ex As Exception
            MessageBox.Show("Błąd zmiany statusu:" & vbCrLf & ex.ToString())
        End Try
    End Sub




    Private Sub SetClosed_Row_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim btn = TryCast(sender, Button)
            If btn Is Nothing OrElse btn.Tag Is Nothing Then
                MessageBox.Show("Brak ID w przycisku (Tag). Kliknij wiersz i spróbuj ponownie.")
                Return
            End If

            Dim id As Integer
            If Not Integer.TryParse(btn.Tag.ToString(), id) OrElse id <= 0 Then
                MessageBox.Show("Nieprawidłowe ID w Tag: " & btn.Tag.ToString())
                Return
            End If

            RepeatEventsRepository.SetStatus(id, "closed", _session.Username)
            InfoText.Text = $"Zamknięto (ID={id})."
            RefreshList()

        Catch ex As Exception
            MessageBox.Show("Błąd zmiany statusu:" & vbCrLf & ex.ToString())
        End Try
    End Sub


    Private Sub QuickNote_Row_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim btn = TryCast(sender, Button)
            If btn Is Nothing Then Return
            Dim id = CInt(btn.Tag)

            Dim w As New QuickNoteWindow(id, _session)
            w.Owner = Me
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner
            If w.ShowDialog() = True Then
                InfoText.Text = $"Zapisano notatkę (ID={id})."
                RefreshList()
            End If
        Catch ex As Exception
            MessageBox.Show("Błąd notatki:" & vbCrLf & ex.ToString())
        End Try
    End Sub
    Private Sub Delete_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim selected = TryCast(EventsGrid.SelectedItem, RepeatEventListItem)
            If selected Is Nothing Then
                MessageBox.Show("Wybierz wpis do usunięcia.")
                Return
            End If

            If Not _session.IsAdmin Then
                MessageBox.Show("Usuwanie tylko dla administratora.")
                Return
            End If

            If MessageBox.Show($"Usunąć wpis ID={selected.Id}?",
                               "Potwierdź",
                               MessageBoxButton.YesNo,
                               MessageBoxImage.Warning) <> MessageBoxResult.Yes Then
                Return
            End If

            ' USUWANIE W BAZIE (soft delete)
            RepeatEventsRepository.SoftDelete(selected.Id, _session)

            ' USUWANIE Z LISTY UI (żeby od razu zniknął)
            _items.Remove(selected)
            InfoText.Text = $"Wpisów: {_items.Count}"

            ' (opcjonalnie, ale polecam) dociągnij listę jeszcze raz z bazy:
            RefreshList()

        Catch ex As Exception
            MessageBox.Show("Błąd usuwania:" & vbCrLf & ex.ToString())
        End Try
    End Sub

    Private Function GetIdFromSenderOrSelected(sender As Object) As Integer
        ' 1) spróbuj z Tag przycisku
        Dim btn = TryCast(sender, Button)
        If btn IsNot Nothing AndAlso btn.Tag IsNot Nothing Then
            Dim idFromTag As Integer
            If Integer.TryParse(btn.Tag.ToString(), idFromTag) AndAlso idFromTag > 0 Then
                Return idFromTag
            End If
        End If

        ' 2) fallback: weź zaznaczony wiersz z DataGrid
        Dim selected = TryCast(EventsGrid.SelectedItem, RepeatEventListItem)
        If selected IsNot Nothing AndAlso selected.Id > 0 Then
            Return selected.Id
        End If

        MessageBox.Show("Nie udało się ustalić ID wpisu. Kliknij najpierw w wiersz.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning)
        Return -1
    End Function




End Class
