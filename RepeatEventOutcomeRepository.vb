Imports Microsoft.Data.Sqlite

Public Module RepeatEventOutcomeRepository

    Public Sub UpdateOutcomeAndCorrective(eventId As Integer,
                                         newOutcome As String,
                                         newCorrective As String,
                                         username As String)

        Dim oldOutcome As String = ""
        Dim oldCorrective As String = ""

        Using con = Db.Open()

            ' 1) pobierz stare wartości
            Using getCmd = con.CreateCommand()
                getCmd.CommandText =
"SELECT COALESCE(Outcome,''), COALESCE(CorrectiveAction,'')
 FROM RepeatEvents
 WHERE Id=$id;"
                getCmd.Parameters.AddWithValue("$id", eventId)

                Using r = getCmd.ExecuteReader()
                    If r.Read() Then
                        oldOutcome = r.GetString(0)
                        oldCorrective = r.GetString(1)
                    End If
                End Using
            End Using

            Dim outVal = If(newOutcome, "")
            Dim corrVal = If(newCorrective, "")

            ' nic do zrobienia
            If oldOutcome = outVal AndAlso oldCorrective = corrVal Then Return

            ' 2) update w bazie
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"UPDATE RepeatEvents
 SET Outcome = $o,
     CorrectiveAction = $c,
     UpdatedAt = $ua,
     UpdatedBy = $ub,
     RowVersion = RowVersion + 1
 WHERE Id = $id;"
                cmd.Parameters.AddWithValue("$o", outVal)
                cmd.Parameters.AddWithValue("$c", corrVal)
                cmd.Parameters.AddWithValue("$ua", DateTime.Now.ToString("s"))
                cmd.Parameters.AddWithValue("$ub", username)
                cmd.Parameters.AddWithValue("$id", eventId)
                cmd.ExecuteNonQuery()
            End Using

        End Using

        ' 3) audyt osobno dla pól
        If oldOutcome <> If(newOutcome, "") Then
            AuditRepository.LogChange(eventId, "edit", "Outcome", oldOutcome, If(newOutcome, ""), username)
        End If

        If oldCorrective <> If(newCorrective, "") Then
            AuditRepository.LogChange(eventId, "edit", "CorrectiveAction", oldCorrective, If(newCorrective, ""), username)
        End If

    End Sub

End Module
