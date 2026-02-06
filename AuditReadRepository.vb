Imports Microsoft.Data.Sqlite
Imports System.Globalization

Public Module AuditReadRepository

    Public Function ListForEvent(eventId As Integer) As List(Of AuditItem)
        Dim items As New List(Of AuditItem)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"SELECT
    ChangedAt,
    COALESCE(ChangedBy,'') AS ChangedBy,
    COALESCE(Action,'') AS Action,
    COALESCE(FieldName,'') AS FieldName,
    COALESCE(OldValue,'') AS OldValue,
    COALESCE(NewValue,'') AS NewValue,
    COALESCE(Machine,'') AS Machine
FROM AuditLog
WHERE EventId = $id
ORDER BY ChangedAt DESC;"
                cmd.Parameters.AddWithValue("$id", eventId)

                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim atStr = r.GetString(0)
                        Dim atDt = ParseDateTimeOrMin(atStr, False)

                        items.Add(New AuditItem With {
                            .ChangedAt = atDt.ToLocalTime(),
                            .ChangedBy = r.GetString(1),
                            .Action = r.GetString(2),
                            .FieldName = r.GetString(3),
                            .OldValue = r.GetString(4),
                            .NewValue = r.GetString(5),
                            .Machine = r.GetString(6)
                        })
                    End While
                End Using
            End Using
        End Using

        Return items
    End Function

End Module
