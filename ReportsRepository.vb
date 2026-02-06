Imports Microsoft.Data.Sqlite

Public Module ReportsRepository

    ' Zwraca listę słowników: klucz=kolumna, wartość=tekst do CSV
    Public Function GetRowsForExport(modality As String,
                                     status As String,
                                     dateField As String,
                                     fromDate As DateTime?,
                                     toDate As DateTime?) As List(Of Dictionary(Of String, String))

        Dim rows As New List(Of Dictionary(Of String, String))

        If String.IsNullOrWhiteSpace(dateField) Then dateField = "FirstPartDateTime"
        If dateField <> "FirstPartDateTime" AndAlso dateField <> "CreatedAt" Then
            dateField = "FirstPartDateTime"
        End If

        Using con = Db.Open()
            Using cmd = con.CreateCommand()

                Dim whereParts As New List(Of String)
                whereParts.Add("1=1")

                If modality IsNot Nothing AndAlso modality <> "" AndAlso modality <> "ALL" Then
                    whereParts.Add("e.Modality = $m")
                    cmd.Parameters.AddWithValue("$m", modality)
                End If

                If status IsNot Nothing AndAlso status <> "" AndAlso status <> "ALL" Then
                    whereParts.Add("e.Status = $st")
                    cmd.Parameters.AddWithValue("$st", status)
                End If

                If fromDate.HasValue Then
                    whereParts.Add($"e.{dateField} >= $from")
                    cmd.Parameters.AddWithValue("$from", fromDate.Value.ToString("s"))
                End If

                If toDate.HasValue Then
                    whereParts.Add($"e.{dateField} <= $to")
                    cmd.Parameters.AddWithValue("$to", toDate.Value.ToString("s"))
                End If

                Dim whereSql = String.Join(" AND ", whereParts)

                cmd.CommandText =
$"SELECT
    e.Id, e.Modality, e.Device, e.EventType,
    e.PatientName, e.PatientId, COALESCE(e.Accession,'') AS Accession,
    e.FirstPartDateTime,
    e.FixRequestedByDoctor, e.TechFirstPart, e.Nurse,
    e.ReasonId, COALESCE(r.Name,'') AS ReasonName, COALESCE(e.ReasonOtherText,'') AS ReasonOtherText,
    COALESCE(e.FixDateTime,'') AS FixDateTime, COALESCE(e.ExtraMinutes,'') AS ExtraMinutes,
    e.Status,
    COALESCE(e.Outcome,'') AS Outcome,
    COALESCE(e.Description,'') AS Description,
    COALESCE(e.CorrectiveAction,'') AS CorrectiveAction,
    e.CreatedAt, e.CreatedBy,
    COALESCE(e.UpdatedAt,'') AS UpdatedAt, COALESCE(e.UpdatedBy,'') AS UpdatedBy,
    COALESCE(e.ClosedAt,'') AS ClosedAt, COALESCE(e.ClosedBy,'') AS ClosedBy,
    COALESCE(e.Notes,'') AS Notes
FROM RepeatEvents e
LEFT JOIN Reasons r ON r.Id = e.ReasonId
WHERE {whereSql}
ORDER BY e.{dateField} DESC;"

                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim d As New Dictionary(Of String, String) From {
                            {"Id", r.GetInt32(0).ToString()},
                            {"Modality", r.GetString(1)},
                            {"Device", r.GetString(2)},
                            {"EventType", r.GetString(3)},
                            {"PatientName", r.GetString(4)},
                            {"PatientId", r.GetString(5)},
                            {"Accession", r.GetString(6)},
                            {"FirstPartDateTime", r.GetString(7)},
                            {"FixRequestedByDoctor", r.GetString(8)},
                            {"TechFirstPart", r.GetString(9)},
                            {"Nurse", r.GetString(10)},
                            {"ReasonId", r.GetInt32(11).ToString()},
                            {"ReasonName", r.GetString(12)},
                            {"ReasonOtherText", r.GetString(13)},
                            {"FixDateTime", r.GetString(14)},
                            {"ExtraMinutes", r.GetString(15)},
                            {"Status", r.GetString(16)},
                            {"Outcome", r.GetString(17)},
                            {"Description", r.GetString(18)},
                            {"CorrectiveAction", r.GetString(19)},
                            {"CreatedAt", r.GetString(20)},
                            {"CreatedBy", r.GetString(21)},
                            {"UpdatedAt", r.GetString(22)},
                            {"UpdatedBy", r.GetString(23)},
                            {"ClosedAt", r.GetString(24)},
                            {"ClosedBy", r.GetString(25)},
                            {"Notes", r.GetString(26)}
                        }
                        rows.Add(d)
                    End While
                End Using

            End Using
        End Using

        Return rows
    End Function

End Module
