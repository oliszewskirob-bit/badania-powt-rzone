Imports Microsoft.Data.Sqlite
Imports System.Globalization

Public Module RepeatEventListRepository

    Public Function List(modality As String,
                         status As String,
                         Optional search As String = "") As List(Of RepeatEventListItem)

        Dim items As New List(Of RepeatEventListItem)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()

                Dim whereParts As New List(Of String)

                ' --- KLUCZOWE: ukryj soft-delete ---
                whereParts.Add("IFNULL(e.IsDeleted,0)=0")

                If Not String.IsNullOrWhiteSpace(modality) AndAlso modality <> "ALL" Then
                    whereParts.Add("e.Modality = $m")
                    cmd.Parameters.AddWithValue("$m", modality)
                End If

                If Not String.IsNullOrWhiteSpace(status) AndAlso status <> "ALL" Then
                    whereParts.Add("COALESCE(e.Status,'new') = $s")
                    cmd.Parameters.AddWithValue("$s", status)
                End If

                If Not String.IsNullOrWhiteSpace(search) Then
                    whereParts.Add("(e.PatientName LIKE $q OR e.PatientId LIKE $q)")
                    cmd.Parameters.AddWithValue("$q", "%" & search & "%")
                End If

                Dim whereClause As String = ""
                If whereParts.Count > 0 Then
                    whereClause = "WHERE " & String.Join(" AND ", whereParts)
                End If

                cmd.CommandText =
$"SELECT
    e.Id,
    e.FirstPartDateTime,
    e.Modality,
    e.EventType,
    e.Device,
    e.PatientName,
    e.PatientId,
    COALESCE(r.Name,'') AS ReasonName,
    COALESCE(e.ReasonOtherText,'') AS ReasonOtherText,
    COALESCE(e.Status,'new') AS Status,
    e.CreatedAt,
    COALESCE(e.CreatedBy,'') AS CreatedBy
FROM RepeatEvents e
LEFT JOIN Reasons r ON r.Id = e.ReasonId
{whereClause}
ORDER BY e.FirstPartDateTime DESC;"

                Using r = cmd.ExecuteReader()
                    While r.Read()

                        Dim dt1 = DateTime.Parse(r.GetString(1), Nothing, DateTimeStyles.RoundtripKind).ToLocalTime()
                        Dim created = DateTime.Parse(r.GetString(10), Nothing, DateTimeStyles.RoundtripKind).ToLocalTime()

                        items.Add(New RepeatEventListItem With {
                            .Id = r.GetInt32(0),
                            .FirstPartDateTime = dt1,
                            .Modality = r.GetString(2),
                            .EventType = r.GetString(3),
                            .Device = r.GetString(4),
                            .PatientName = r.GetString(5),
                            .PatientId = r.GetString(6),
                            .ReasonName = r.GetString(7),
                            .ReasonOtherText = r.GetString(8),
                            .Status = r.GetString(9),
                            .CreatedAt = created,
                            .CreatedBy = r.GetString(11)
                        })
                    End While
                End Using

            End Using
        End Using

        Return items
    End Function


    Public Function CountByStatus(modality As String,
                                  Optional search As String = "") As Dictionary(Of String, Integer)

        Dim result As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) From {
            {"all", 0},
            {"new", 0},
            {"in_progress", 0},
            {"closed", 0}
        }

        Using con = Db.Open()
            Using cmd = con.CreateCommand()

                Dim whereParts As New List(Of String)

                ' --- KLUCZOWE: ukryj soft-delete ---
                whereParts.Add("IFNULL(e.IsDeleted,0)=0")

                If Not String.IsNullOrWhiteSpace(modality) AndAlso modality <> "ALL" Then
                    whereParts.Add("e.Modality = $m")
                    cmd.Parameters.AddWithValue("$m", modality)
                End If

                If Not String.IsNullOrWhiteSpace(search) Then
                    whereParts.Add("(e.PatientName LIKE $q OR e.PatientId LIKE $q)")
                    cmd.Parameters.AddWithValue("$q", "%" & search & "%")
                End If

                Dim whereClause As String = ""
                If whereParts.Count > 0 Then
                    whereClause = "WHERE " & String.Join(" AND ", whereParts)
                End If

                cmd.CommandText =
$"SELECT COALESCE(e.Status,'new') AS Status, COUNT(*) AS Cnt
FROM RepeatEvents e
{whereClause}
GROUP BY COALESCE(e.Status,'new');"

                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim st = r.GetString(0)
                        Dim cnt = r.GetInt32(1)

                        result("all") += cnt

                        If result.ContainsKey(st) Then
                            result(st) = cnt
                        End If
                    End While
                End Using

            End Using
        End Using

        Return result
    End Function

End Module
