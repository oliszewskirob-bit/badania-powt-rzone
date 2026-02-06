Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports Microsoft.Data.Sqlite
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.IO
Imports Microsoft.Win32

Public Class EventDetailsWindow

    Private ReadOnly _eventId As Integer
    Private ReadOnly _session As UserSession
    Private ReadOnly _attachments As New ObservableCollection(Of AttachmentRow)

    Private Class AttachmentRow
        Public Property FileName As String
        Public Property FullPath As String
        Public Property SizeText As String
        Public Property ModifiedText As String
    End Class


    Private _isLoading As Boolean = False
    Private _isSavingNotes As Boolean = False

    Public Sub New(eventId As Integer, session As UserSession)
        InitializeComponent()
        _eventId = eventId
        _session = session
        LoadData()
    End Sub

    Private Sub LoadData()
        _isLoading = True
        Try
            Using con = Db.Open()
                Using cmd = con.CreateCommand()

                    cmd.CommandText =
"SELECT
    e.Id,
    e.Modality,
    e.Device,
    e.EventType,
    e.PatientName,
    e.PatientId,
    COALESCE(e.Accession,'') AS Accession,
    e.FirstPartDateTime,
    e.FixRequestedByDoctor,
    e.TechFirstPart,
    e.Nurse,
    COALESCE(r.Name,'') AS ReasonName,
    COALESCE(e.ReasonOtherText,'') AS ReasonOtherText,
    e.Status,
    COALESCE(e.Description,'') AS Description,
    e.CreatedAt,
    e.CreatedBy,
    COALESCE(e.Outcome,'') AS Outcome,
    COALESCE(e.CorrectiveAction,'') AS CorrectiveAction,
    COALESCE(e.Notes,'') AS Notes,
    COALESCE(e.IsContrastExtravasation,0) AS IsContrastExtravasation,
    COALESCE(e.ContrastCannula,'') AS ContrastCannula,
    COALESCE(e.ContrastType,'') AS ContrastType,
    COALESCE(e.ContrastFlow,'') AS ContrastFlow,
    COALESCE(e.ContrastVolume,'') AS ContrastVolume,
    COALESCE(e.ContrastVisible,0) AS ContrastVisible,
    COALESCE(e.WardNotified,0) AS WardNotified,
    COALESCE(e.PatientInstructions,0) AS PatientInstructions,
    COALESCE(e.ContrastAdditionalInfo,'') AS ContrastAdditionalInfo
FROM RepeatEvents e
LEFT JOIN Reasons r ON r.Id = e.ReasonId
WHERE e.Id = $id;"

                    cmd.Parameters.AddWithValue("$id", _eventId)

                    Using r = cmd.ExecuteReader()
                        If Not r.Read() Then
                            MessageBox.Show("Nie znaleziono zgłoszenia w bazie.")
                            Close()
                            Return
                        End If

                        ' ====== WYPEŁNIJ POLA UI (Twoje nazwy kontrolek) ======
                        IdBox.Text = r.GetInt32(0).ToString()
                        ModalityBox.Text = r.GetString(1)
                        DeviceBox.Text = r.GetString(2)
                        EventTypeBox.Text = EventTypeLabel(r.GetString(3))

                        PatientNameBox.Text = r.GetString(4)
                        PatientIdBox.Text = r.GetString(5)
                        AccessionBox.Text = r.GetString(6)

                        FirstPartBox.Text = FormatMaybeDate(r.GetString(7))

                        DoctorBox.Text = r.GetString(8)
                        TechBox.Text = r.GetString(9)
                        NurseBox.Text = r.GetString(10)

                        ReasonBox.Text = r.GetString(11)
                        ReasonOtherBox.Text = r.GetString(12)

                        Dim st = r.GetString(13)
                        StatusBox.Text = StatusLabel(st)

                        DescBox.Text = r.GetString(14)

                        Dim createdAtRaw = r.GetString(15)
                        Dim createdBy = r.GetString(16)
                        CreatedBox.Text = $"{FormatMaybeDate(createdAtRaw)} • {createdBy}"

                        OutcomeBox.Text = r.GetString(17)
                        CorrectiveActionBox.Text = r.GetString(18)
                        OutcomeSavedInfo.Text = ""
                        OutcomeInfo.Text = ""


                        ' Notatka z tabeli (kolumna Notes)
                        NotesBox.Text = r.GetString(19)
                        NotesSavedInfo.Text = ""

                        Dim isExtravasation = (r.GetInt32(20) = 1)
                        If ContrastSection IsNot Nothing Then
                            ContrastSection.Visibility = If(isExtravasation, Visibility.Visible, Visibility.Collapsed)
                        End If

                        If isExtravasation Then
                            ContrastCannulaBox.Text = r.GetString(21)
                            ContrastTypeBox.Text = r.GetString(22)
                            ContrastFlowBox.Text = r.GetString(23)
                            ContrastVolumeBox.Text = r.GetString(24)
                            ContrastVisibleBox.Text = YesNoLabel(r.GetInt32(25) = 1)
                            WardNotifiedBox.Text = YesNoLabel(r.GetInt32(26) = 1)
                            PatientInstructionsBox.Text = YesNoLabel(r.GetInt32(27) = 1)
                            ContrastAdditionalInfoBox.Text = r.GetString(28)
                        Else
                            ContrastCannulaBox.Text = ""
                            ContrastTypeBox.Text = ""
                            ContrastFlowBox.Text = ""
                            ContrastVolumeBox.Text = ""
                            ContrastVisibleBox.Text = ""
                            WardNotifiedBox.Text = ""
                            PatientInstructionsBox.Text = ""
                            ContrastAdditionalInfoBox.Text = ""
                        End If

                        ' Nagłówek po prawej (opcjonalnie)
                        If HeaderRight IsNot Nothing Then
                            HeaderRight.Text = $"Status: {StatusLabel(st)}"
                        End If
                    End Using
                End Using
            End Using

            AttachmentsGrid.ItemsSource = _attachments
            RefreshAttachments()

            ' ====== AUDYT ======
            AuditGrid.ItemsSource = AuditRepository.GetForEvent(_eventId)

        Catch ex As Exception
            MessageBox.Show("Błąd wczytywania danych:" & vbCrLf & ex.ToString())
        Finally
            _isLoading = False
        End Try
    End Sub

    Private Function FormatMaybeDate(raw As String) As String
        If String.IsNullOrWhiteSpace(raw) Then Return ""
        Dim dt As DateTime
        If DateTime.TryParse(raw, dt) Then
            Return dt.ToString("yyyy-MM-dd HH:mm")
        End If
        Return raw
    End Function

    ' =========================
    ' NOTATKA
    ' =========================

    Private Sub NotesBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isLoading Then Return
        If NotesSavedInfo IsNot Nothing Then
            NotesSavedInfo.Text = "Niezapisane zmiany…"
        End If
    End Sub

    Private Sub NotesBox_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        e.Handled = True
    End Sub

    Private Sub SaveNotes_Click(sender As Object, e As RoutedEventArgs)
        If _isLoading Then Return
        If _isSavingNotes Then Return

        Try
            _isSavingNotes = True

            Dim notes As String = ""
            If NotesBox IsNot Nothing AndAlso NotesBox.Text IsNot Nothing Then
                notes = NotesBox.Text.Trim()
            End If

            RepeatEventNotesRepository.UpdateNotes(_eventId, notes, _session.Username)

            If NotesSavedInfo IsNot Nothing Then
                NotesSavedInfo.Text = "Zapisano ✔"
            End If

            ' odśwież audyt, żeby było widać wpis
            AuditGrid.ItemsSource = AuditRepository.GetForEvent(_eventId)

        Catch ex As Exception
            MessageBox.Show("Błąd zapisu notatki:" & vbCrLf & ex.ToString())
        Finally
            _isSavingNotes = False
        End Try
    End Sub

    ' =========================
    ' STATUS (Tag = in_progress / closed / new)
    ' =========================

    Private Sub SetStatus_Click(sender As Object, e As RoutedEventArgs)
        Dim btn = TryCast(sender, Button)
        If btn Is Nothing Then Return

        Dim newStatus = btn.Tag?.ToString()
        If String.IsNullOrWhiteSpace(newStatus) Then Return

        Try
            RepeatEventStatusRepository.SetStatus(_eventId, newStatus, _session.Username)
            LoadData()
        Catch ex As Exception
            MessageBox.Show("Błąd zmiany statusu:" & vbCrLf & ex.ToString())
        End Try
    End Sub

    ' =========================
    ' ZAMKNIJ OKNO
    ' =========================

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = True
        Close()
    End Sub
    Private Sub Outcome_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isLoading Then Return
        OutcomeInfo.Text = "Niezapisane zmiany…"
    End Sub

    Private Sub CorrectiveAction_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isLoading Then Return
        OutcomeInfo.Text = "Niezapisane zmiany…"
    End Sub

    Private Sub SaveOutcomeCorrective_Click(sender As Object, e As RoutedEventArgs)
        If _isLoading Then Return

        Try
            Dim outcome = If(OutcomeBox.Text, "").Trim()
            Dim corrective = If(CorrectiveActionBox.Text, "").Trim()

            RepeatEventOutcomeRepository.UpdateOutcomeAndCorrective(_eventId, outcome, corrective, _session.Username)

            OutcomeSavedInfo.Text = "Zapisano ✔"
            OutcomeInfo.Text = ""

            ' odśwież audyt
            AuditGrid.ItemsSource = AuditRepository.GetForEvent(_eventId)

        Catch ex As Exception
            MessageBox.Show("Błąd zapisu Outcome/Działań:" & vbCrLf & ex.ToString())
        End Try
    End Sub
    Private Function EventAttachmentsDir(eventId As Integer) As String
        Dim baseDir = AppDomain.CurrentDomain.BaseDirectory
        Dim dir = System.IO.Path.Combine(baseDir, "attachments", $"event_{eventId}")
        If Not System.IO.Directory.Exists(dir) Then
            System.IO.Directory.CreateDirectory(dir)
        End If
        Return dir
    End Function

    Private Function AttachmentsDir() As String
        Dim baseDir = AppDomain.CurrentDomain.BaseDirectory
        Dim dir = Path.Combine(baseDir, "attachments", $"event_{_eventId}")
        If Not Directory.Exists(dir) Then
            Directory.CreateDirectory(dir)
        End If
        Return dir
    End Function

    Private Sub RefreshAttachments()
        Try
            _attachments.Clear()

            Dim dir = AttachmentsDir()
            Dim files = Directory.GetFiles(dir)

            For Each p In files
                Dim fi As New FileInfo(p)
                _attachments.Add(New AttachmentRow With {
                .FileName = fi.Name,
                .FullPath = fi.FullName,
                .SizeText = FormatSize(fi.Length),
                .ModifiedText = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
            })
            Next

        Catch ex As Exception
            MessageBox.Show("Błąd wczytania załączników:" & vbCrLf & ex.ToString())
        End Try
    End Sub

    Private Function FormatSize(bytes As Long) As String
        If bytes < 1024 Then Return bytes & " B"
        Dim kb = bytes / 1024.0
        If kb < 1024 Then Return kb.ToString("0.0") & " KB"
        Dim mb = kb / 1024.0
        If mb < 1024 Then Return mb.ToString("0.0") & " MB"
        Dim gb = mb / 1024.0
        Return gb.ToString("0.00") & " GB"
    End Function

    Private Sub AddAttachment_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim dlg As New OpenFileDialog()
            dlg.Title = "Wybierz plik do dodania"
            dlg.Multiselect = True

            If dlg.ShowDialog() <> True Then Return

            Dim dir = AttachmentsDir()

            For Each src In dlg.FileNames
                Dim name = Path.GetFileName(src)
                Dim dest = Path.Combine(dir, name)

                ' Jeśli plik o tej nazwie istnieje — dopisz licznik
                If File.Exists(dest) Then
                    Dim baseName = Path.GetFileNameWithoutExtension(name)
                    Dim ext = Path.GetExtension(name)
                    Dim i = 1
                    Do
                        dest = Path.Combine(dir, $"{baseName} ({i}){ext}")
                        i += 1
                    Loop While File.Exists(dest)
                End If

                File.Copy(src, dest, False)
            Next

            RefreshAttachments()
            ' opcjonalnie audyt:
            AuditRepository.LogChange(_eventId, "attachment_add", "Attachments", "", String.Join(", ", dlg.SafeFileNames), _session.Username)

        Catch ex As Exception
            MessageBox.Show("Błąd dodawania pliku:" & vbCrLf & ex.ToString())
        End Try
    End Sub

    Private Sub Attachments_DoubleClick(sender As Object, e As MouseButtonEventArgs)
        Dim row = TryCast(AttachmentsGrid.SelectedItem, AttachmentRow)
        If row Is Nothing Then Return

        Try
            If Not File.Exists(row.FullPath) Then
                MessageBox.Show("Plik nie istnieje: " & row.FullPath)
                Return
            End If

            Process.Start(New ProcessStartInfo(row.FullPath) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show("Nie udało się otworzyć pliku:" & vbCrLf & ex.ToString())
        End Try
    End Sub

End Class
