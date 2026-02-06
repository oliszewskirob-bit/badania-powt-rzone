Imports Microsoft.Data.Sqlite

Public Module RepeatEventNotesRepository

    Public Function GetNotes(eventId As Integer) As String
        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText = "SELECT COALESCE(Notes,'') FROM RepeatEvents WHERE Id=$id;"
                cmd.Parameters.AddWithValue("$id", eventId)
                Dim obj = cmd.ExecuteScalar()
                If obj Is Nothing Then Return ""
                Return obj.ToString()
            End Using
        End Using
    End Function

    Public Sub UpdateNotes(eventId As Integer, newNotes As String, username As String)

        Dim oldNotes As String = ""

        Using con = Db.Open()

            ' 1) pobierz stare
            Using getCmd = con.CreateCommand()
                getCmd.CommandText = "SELECT COALESCE(Notes,'') FROM RepeatEvents WHERE Id=$id;"
                getCmd.Parameters.AddWithValue("$id", eventId)
                Dim obj = getCmd.ExecuteScalar()
                If obj IsNot Nothing Then oldNotes = obj.ToString()
            End Using

            If oldNotes = newNotes Then
                Return
            End If

            ' 2) update
            Using cmd = con.CreateCommand()
                cmd.CommandText = "UPDATE RepeatEvents SET Notes = $n WHERE Id=$id;"
                cmd.Parameters.AddWithValue("$n", If(newNotes, ""))
                cmd.Parameters.AddWithValue("$id", eventId)
                cmd.ExecuteNonQuery()
            End Using

        End Using

        ' 3) audyt
        AuditRepository.LogChange(eventId, "edit", "Notes", oldNotes, newNotes, username)

    End Sub

End Module
