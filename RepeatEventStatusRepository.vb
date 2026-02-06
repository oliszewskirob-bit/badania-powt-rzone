Imports Microsoft.Data.Sqlite

Public Module RepeatEventStatusRepository

    Public Sub SetStatus(eventId As Integer, status As String, username As String)

        Dim oldStatus As String = ""

        Using con = Db.Open()

            ' 1) pobierz stary status
            Using getCmd = con.CreateCommand()
                getCmd.CommandText = "SELECT COALESCE(Status,'new') FROM RepeatEvents WHERE Id=$id;"
                getCmd.Parameters.AddWithValue("$id", eventId)

                Dim obj = getCmd.ExecuteScalar()
                If obj IsNot Nothing Then
                    oldStatus = obj.ToString()
                End If
            End Using

            ' jeśli status bez zmian → nie rób nic
            If String.Equals(oldStatus, status, StringComparison.OrdinalIgnoreCase) Then
                Return
            End If

            ' 2) update
            Using cmd = con.CreateCommand()

                If status = "closed" Then
                    cmd.CommandText =
"UPDATE RepeatEvents
 SET Status = $st,
     ClosedAt = $dt,
     ClosedBy = $by
 WHERE Id = $id;"
                    cmd.Parameters.AddWithValue("$dt", DateTime.UtcNow.ToString("o"))
                    cmd.Parameters.AddWithValue("$by", username)
                Else
                    cmd.CommandText =
"UPDATE RepeatEvents
 SET Status = $st
 WHERE Id = $id;"
                End If

                cmd.Parameters.AddWithValue("$st", status)
                cmd.Parameters.AddWithValue("$id", eventId)

                cmd.ExecuteNonQuery()
            End Using

        End Using

        ' 3) audyt (POZA Using, ale oldStatus już istnieje)
        AuditRepository.LogChange(
            eventId,
            "status_change",
            "Status",
            oldStatus,
            status,
            username
        )

    End Sub

End Module
