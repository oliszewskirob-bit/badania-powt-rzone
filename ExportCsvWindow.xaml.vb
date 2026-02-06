Imports System
Imports System.Collections.Generic
Imports System.Windows
Imports System.Windows.Controls

Public Class ExportCsvWindow

    Private ReadOnly _session As UserSession

    Public Sub New(session As UserSession)
        InitializeComponent()
        _session = session

        If InfoText IsNot Nothing Then
            InfoText.Text = "Wybierz zakres dat i filtry, potem kliknij Generuj CSV."
        End If
    End Sub

    Private Function SelectedModality() As String
        Dim item = TryCast(ModalityBox.SelectedItem, ComboBoxItem)
        If item Is Nothing Then Return "ALL"
        Dim tagValue = TryCast(item.Tag, String)
        If Not String.IsNullOrWhiteSpace(tagValue) Then
            Return tagValue
        End If
        Return item.Content.ToString()
    End Function

    Private Function SelectedStatus() As String
        Dim item = TryCast(StatusBox.SelectedItem, ComboBoxItem)
        If item Is Nothing Then Return "ALL"
        Dim tagValue = TryCast(item.Tag, String)
        If Not String.IsNullOrWhiteSpace(tagValue) Then
            Return tagValue
        End If
        Return item.Content.ToString()
    End Function

    Private Function SelectedDateField() As String
        If DateFieldCreated IsNot Nothing AndAlso DateFieldCreated.IsChecked = True Then
            Return "CreatedAt"
        End If
        Return "FirstPartDateTime"
    End Function

    Private Function DateRange() As (FromDate As DateTime?, ToDate As DateTime?)
        Dim f As DateTime? = Nothing
        Dim t As DateTime? = Nothing

        If FromDatePicker IsNot Nothing AndAlso FromDatePicker.SelectedDate.HasValue Then
            f = FromDatePicker.SelectedDate.Value.Date
        End If

        If ToDatePicker IsNot Nothing AndAlso ToDatePicker.SelectedDate.HasValue Then
            t = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1)
        End If

        Return (f, t)
    End Function

    Private Sub Generate_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim dr = DateRange()
            Dim modality = SelectedModality()
            Dim status = SelectedStatus()
            Dim dateField = SelectedDateField()

            Dim rows = ReportsRepository.GetRowsForExport(modality, status, dateField, dr.FromDate, dr.ToDate)

            Dim headers As New List(Of String) From {
                "Id", "Modality", "Device", "EventType",
                "PatientName", "PatientId", "Accession",
                "FirstPartDateTime",
                "FixRequestedByDoctor", "TechFirstPart", "Nurse",
                "ReasonId", "ReasonName", "ReasonOtherText",
                "FixDateTime", "ExtraMinutes",
                "Status", "Outcome", "Description", "CorrectiveAction",
                "IsContrastExtravasation", "ContrastCannula", "ContrastType", "ContrastFlow", "ContrastVolume",
                "ContrastVisible", "WardNotified", "PatientInstructions", "ContrastAdditionalInfo",
                "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy",
                "ClosedAt", "ClosedBy",
                "Notes"
            }

            Dim folder = System.AppDomain.CurrentDomain.BaseDirectory
            Dim fileName = "QTMR_EXPORT_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".csv"
            Dim path = System.IO.Path.Combine(folder, fileName)

            CsvExport.WriteCsv(path, headers, rows)

            If InfoText IsNot Nothing Then
                InfoText.Text = "Zapisano: " & path & " (wierszy: " & rows.Count.ToString() & ")"
            End If

            MessageBox.Show("Zapisano plik CSV:" & vbCrLf & path,
                            "Eksport CSV",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information)

        Catch ex As Exception
            MessageBox.Show("Błąd eksportu:" & vbCrLf & ex.ToString())
        End Try
    End Sub

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

End Class
