Imports Microsoft.Data.Sqlite

Public Module AuditRepository

    Public Sub LogChange(eventId As Integer,
                         actionName As String,
                         fieldName As String,
                         oldValue As String,
                         newValue As String,
                         changedBy As String)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"INSERT INTO AuditLog
(EventId, Action, FieldName, OldValue, NewValue, ChangedAt, ChangedBy, Machine, AppVersion)
VALUES
($eid, $act, $field, $old, $new, $at, $by, $mach, $ver);"

                cmd.Parameters.AddWithValue("$eid", eventId)
                cmd.Parameters.AddWithValue("$act", actionName)
                cmd.Parameters.AddWithValue("$field", fieldName)
                cmd.Parameters.AddWithValue("$old", If(oldValue, ""))
                cmd.Parameters.AddWithValue("$new", If(newValue, ""))
                cmd.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("o"))
                cmd.Parameters.AddWithValue("$by", changedBy)
                cmd.Parameters.AddWithValue("$mach", Environment.MachineName)
                cmd.Parameters.AddWithValue("$ver", AppDomain.CurrentDomain.FriendlyName)

                cmd.ExecuteNonQuery()
            End Using
        End Using

    End Sub
    Public Function GetForEvent(eventId As Integer) As List(Of AuditLogItem)

        Dim items As New List(Of AuditLogItem)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()

                cmd.CommandText =
    "SELECT
    Id,
    EventId,
    ChangedAt,
    ChangedBy,
    FieldName,
    OldValue,
    NewValue,
    Machine
FROM AuditLog
WHERE EventId = $id
ORDER BY ChangedAt DESC;"

                cmd.Parameters.AddWithValue("$id", eventId)

                Using r = cmd.ExecuteReader()
                    While r.Read()
                        items.Add(New AuditLogItem With {
                            .Id = r.GetInt32(0),
                            .EventId = r.GetInt32(1),
                            .ChangedAt = ParseDateTimeOrMin(r.GetString(2)),
                            .ChangedBy = r.GetString(3),
                            .FieldName = r.GetString(4),
                            .OldValue = r.GetString(5),
                            .NewValue = r.GetString(6),
                            .Machine = r.GetString(7)
                        })
                    End While
                End Using

            End Using
        End Using

        Return items
    End Function


End Module
