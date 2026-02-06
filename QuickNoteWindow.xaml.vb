Imports System.Windows

Public Class QuickNoteWindow

    Private ReadOnly _eventId As Integer
    Private ReadOnly _session As UserSession

    Public Sub New(eventId As Integer, session As UserSession)
        InitializeComponent()
        _eventId = eventId
        _session = session

        HeaderText.Text = "Notatka (ID=" & _eventId.ToString() & ")"
        NotesBox.Text = RepeatEventsRepository.GetNotes(_eventId)
        NotesBox.Focus()
        NotesBox.CaretIndex = NotesBox.Text.Length
    End Sub

    Private Sub Save_Click(sender As Object, e As RoutedEventArgs)
        Try
            RepeatEventsRepository.SaveNotes(_eventId, NotesBox.Text, _session.Username)
            DialogResult = True
            Close()
        Catch ex As Exception
            MessageBox.Show("Błąd zapisu notatki:" & vbCrLf & ex.ToString())
        End Try
    End Sub

    Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
        Close()
    End Sub

End Class
